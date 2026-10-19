using LgTyLib.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace LgTyLib.Modules.ObjectPooling
{
    public class ObjectPoolManager : BaseSingleton<ObjectPoolManager>
    {
        private GameObject emptyHolder;
        private static Transform rootHolder;
        private static Dictionary<string, Transform> groupHolders;
        private static Dictionary<GameObject, Transform> prefabToHolder;
        private static Dictionary<GameObject, ObjectPool<GameObject>> objectPools;
        private static Dictionary<GameObject, GameObject> cloneToPrefabMap;
        private static Dictionary<GameObject, HashSet<GameObject>> activeObjects; // prefab -> spawned (active) clones

        protected override void Awake()
        {
            base.Awake();
            objectPools = new();
            cloneToPrefabMap = new();
            groupHolders = new();
            prefabToHolder = new();
            activeObjects = new();

            emptyHolder = new GameObject("Object Pools");
            DontDestroyOnLoad(emptyHolder);
            rootHolder = emptyHolder.transform;
        }

        // group folder -> per-prefab subfolder
        private static Transform GetHolder(string group, string subName)
        {
            group = string.IsNullOrEmpty(group) ? PoolGroup.GameObjects : group;

            if (!groupHolders.TryGetValue(group, out Transform groupT) || groupT == null)
            {
                groupT = new GameObject(group).transform;
                groupT.SetParent(rootHolder);
                groupHolders[group] = groupT;
            }

            Transform sub = groupT.Find(subName);
            if (sub == null)
            {
                sub = new GameObject(subName).transform;
                sub.SetParent(groupT);
            }
            return sub;
        }

        // Custom container wins; otherwise fall back to the group/prefab folder.
        private static Transform ResolveHolder(GameObject prefab, string group, Transform container)
        {
            if (container != null) return container;

            if (prefabToHolder.TryGetValue(prefab, out Transform existing) && existing != null)
                return existing;

            return GetHolder(group, prefab.name);
        }

        // Shared by both spawn paths
        private static GameObject GetFromPool(GameObject prefab, Vector3 pos, Quaternion rot,
            string group, Transform container)
        {
            if (!objectPools.ContainsKey(prefab))
                CreatePool(prefab, pos, rot, group, container);
            else if (container != null)
                prefabToHolder[prefab] = container; // re-point holder; affects new objects and future returns

            GameObject obj = objectPools[prefab].Get();
            if (obj == null) return null;

            if (!cloneToPrefabMap.ContainsKey(obj))
                cloneToPrefabMap.Add(obj, prefab);

            activeObjects[prefab].Add(obj);
            return obj;
        }

        // ════════════════════════════════════════════════════════════════════════
        // SpawnObject — world-space, parent optional
        // ════════════════════════════════════════════════════════════════════════

        private static T SpawnObject<T>(
            GameObject objectToSpawn,
            Vector3 spawnPos, Quaternion spawnRot,
            Transform parent = null,
            string group = PoolGroup.GameObjects,
            Transform container = null)
            where T : Object
        {
            GameObject obj = GetFromPool(objectToSpawn, spawnPos, spawnRot, group, container);
            if (obj == null) return null;

            if (parent != null)
                obj.transform.SetParent(parent, worldPositionStays: true);

            obj.transform.position = spawnPos;
            obj.transform.rotation = spawnRot;
            obj.SetActive(true);

            if (typeof(T) == typeof(GameObject))
                return obj as T;

            T component = obj.GetComponent<T>();
            if (component == null)
                Debug.LogError($"{objectToSpawn.name} has no component of type {typeof(T)}");

            return component;
        }

        // ── GameObject overloads ─────────────────────────────────────────────────

        public GameObject SpawnObject(
            GameObject objectToSpawn, Vector3 spawnPos,
            string group = PoolGroup.GameObjects, Transform container = null)
            => SpawnObject<GameObject>(objectToSpawn, spawnPos, Quaternion.identity, null, group, container);

        public GameObject SpawnObject(
            GameObject objectToSpawn, Vector3 spawnPos, Transform parent,
            string group = PoolGroup.GameObjects, Transform container = null)
            => SpawnObject<GameObject>(objectToSpawn, spawnPos, Quaternion.identity, parent, group, container);

        public GameObject SpawnObject(
            GameObject objectToSpawn, Vector3 spawnPos, Quaternion spawnRot,
            string group = PoolGroup.GameObjects, Transform container = null)
            => SpawnObject<GameObject>(objectToSpawn, spawnPos, spawnRot, null, group, container);

        public GameObject SpawnObject(
            GameObject objectToSpawn, Vector3 spawnPos, Quaternion spawnRot, Transform parent,
            string group = PoolGroup.GameObjects, Transform container = null)
            => SpawnObject<GameObject>(objectToSpawn, spawnPos, spawnRot, parent, group, container);

        // ── Component overloads ──────────────────────────────────────────────────

        public T SpawnObject<T>(
            T typePrefab, Vector3 spawnPos,
            string group = PoolGroup.GameObjects, Transform container = null)
            where T : Component
            => SpawnObject<T>(typePrefab.gameObject, spawnPos, Quaternion.identity, null, group, container);

        public T SpawnObject<T>(
            T typePrefab, Vector3 spawnPos, Transform parent,
            string group = PoolGroup.GameObjects, Transform container = null)
            where T : Component
            => SpawnObject<T>(typePrefab.gameObject, spawnPos, Quaternion.identity, parent, group, container);

        public T SpawnObject<T>(
            T typePrefab, Vector3 spawnPos, Quaternion spawnRot,
            string group = PoolGroup.GameObjects, Transform container = null)
            where T : Component
            => SpawnObject<T>(typePrefab.gameObject, spawnPos, spawnRot, null, group, container);

        public T SpawnObject<T>(
            T typePrefab, Vector3 spawnPos, Quaternion spawnRot, Transform parent,
            string group = PoolGroup.GameObjects, Transform container = null)
            where T : Component
            => SpawnObject<T>(typePrefab.gameObject, spawnPos, spawnRot, parent, group, container);

        // ════════════════════════════════════════════════════════════════════════
        // SpawnObjectLocal — local-space, parent required
        // ════════════════════════════════════════════════════════════════════════

        private T SpawnObjectLocal<T>(
            GameObject objectToSpawn,
            Transform parent,
            Vector3 localPos, Quaternion localRot, Vector3 localScale,
            string group = PoolGroup.GameObjects,
            Transform container = null)
            where T : Object
        {
            GameObject obj = GetFromPool(objectToSpawn, parent.position, localRot, group, container);
            if (obj == null) return null;

            obj.transform.SetParent(parent, worldPositionStays: false);
            obj.transform.localPosition = localPos;
            obj.transform.localRotation = localRot;
            obj.transform.localScale = localScale;
            obj.SetActive(true);

            if (typeof(T) == typeof(GameObject))
                return obj as T;

            T component = obj.GetComponent<T>();
            if (component == null)
                Debug.LogError($"{objectToSpawn.name} has no component of type {typeof(T)}");

            return component;
        }

        // ── GameObject overloads ─────────────────────────────────────────────────

        public GameObject SpawnObjectLocal(
            GameObject objectToSpawn, Transform parent,
            string group = PoolGroup.GameObjects, Transform container = null)
            => SpawnObjectLocal<GameObject>(objectToSpawn, parent, Vector3.zero, Quaternion.identity, Vector3.one, group, container);

        public GameObject SpawnObjectLocal(
            GameObject objectToSpawn, Transform parent, Vector3 localPos,
            string group = PoolGroup.GameObjects, Transform container = null)
            => SpawnObjectLocal<GameObject>(objectToSpawn, parent, localPos, Quaternion.identity, Vector3.one, group, container);

        public GameObject SpawnObjectLocal(
            GameObject objectToSpawn, Transform parent, Vector3 localPos, Quaternion localRot,
            string group = PoolGroup.GameObjects, Transform container = null)
            => SpawnObjectLocal<GameObject>(objectToSpawn, parent, localPos, localRot, Vector3.one, group, container);

        public GameObject SpawnObjectLocal(
            GameObject objectToSpawn, Transform parent, Vector3 localPos, Quaternion localRot, Vector3 localScale,
            string group = PoolGroup.GameObjects, Transform container = null)
            => SpawnObjectLocal<GameObject>(objectToSpawn, parent, localPos, localRot, localScale, group, container);

        // ── Component overloads ──────────────────────────────────────────────────

        public T SpawnObjectLocal<T>(
            T typePrefab, Transform parent,
            string group = PoolGroup.GameObjects, Transform container = null)
            where T : Component
            => SpawnObjectLocal<T>(typePrefab.gameObject, parent, Vector3.zero, Quaternion.identity, Vector3.one, group, container);

        public T SpawnObjectLocal<T>(
            T typePrefab, Transform parent, Vector3 localPos,
            string group = PoolGroup.GameObjects, Transform container = null)
            where T : Component
            => SpawnObjectLocal<T>(typePrefab.gameObject, parent, localPos, Quaternion.identity, Vector3.one, group, container);

        public T SpawnObjectLocal<T>(
            T typePrefab, Transform parent, Vector3 localPos, Quaternion localRot,
            string group = PoolGroup.GameObjects, Transform container = null)
            where T : Component
            => SpawnObjectLocal<T>(typePrefab.gameObject, parent, localPos, localRot, Vector3.one, group, container);

        public T SpawnObjectLocal<T>(
            T typePrefab, Transform parent, Vector3 localPos, Quaternion localRot, Vector3 localScale,
            string group = PoolGroup.GameObjects, Transform container = null)
            where T : Component
            => SpawnObjectLocal<T>(typePrefab.gameObject, parent, localPos, localRot, localScale, group, container);

        // ════════════════════════════════════════════════════════════════════════
        // Pool internals
        // ════════════════════════════════════════════════════════════════════════

        private static void CreatePool(GameObject prefab, Vector3 pos, Quaternion rot,
            string group, Transform container)
        {
            prefabToHolder[prefab] = ResolveHolder(prefab, group, container);
            activeObjects[prefab] = new HashSet<GameObject>();

            var pool = new ObjectPool<GameObject>(
                createFunc: () => CreateObject(prefab, pos, rot),
                actionOnGet: OnGetObject,
                actionOnRelease: OnReleaseObject,
                actionOnDestroy: OnDestroyObject
            );
            objectPools.Add(prefab, pool);
        }

        private static GameObject CreateObject(GameObject prefab, Vector3 pos, Quaternion rot)
        {
            prefab.SetActive(false);
            GameObject obj = Instantiate(prefab, pos, rot);
            prefab.SetActive(true);

            obj.transform.SetParent(prefabToHolder[prefab], worldPositionStays: false);
            return obj;
        }

        private static void OnGetObject(GameObject obj) { }

        private static void OnReleaseObject(GameObject obj)
        {
            if (cloneToPrefabMap.TryGetValue(obj, out GameObject prefab) &&
                activeObjects.TryGetValue(prefab, out var set))
                set.Remove(obj);

            obj.SetActive(false);
        }

        private static void OnDestroyObject(GameObject obj)
        {
            if (cloneToPrefabMap.TryGetValue(obj, out GameObject prefab) &&
                activeObjects.TryGetValue(prefab, out var set))
                set.Remove(obj);

            cloneToPrefabMap.Remove(obj);
        }

        // ════════════════════════════════════════════════════════════════════════
        // Query spawned (active) objects of a pooled prefab
        // ════════════════════════════════════════════════════════════════════════

        private static HashSet<GameObject> GetActiveSet(GameObject prefab)
        {
            if (prefab == null || !activeObjects.TryGetValue(prefab, out var set)) return null;
            set.RemoveWhere(o => o == null); // purge clones destroyed externally (e.g. scene unload)
            return set;
        }

        /// <summary>Number of currently spawned (active) instances of this prefab.</summary>
        public int GetSpawnedCount(GameObject prefab)
            => GetActiveSet(prefab)?.Count ?? 0;

        public int GetSpawnedCount<T>(T typePrefab) where T : Component
            => GetSpawnedCount(typePrefab.gameObject);

        /// <summary>Fills 'results' (cleared first) with spawned instances. No allocation if the list is reused.</summary>
        public void GetSpawnedNonAlloc(GameObject prefab, List<GameObject> results)
        {
            results.Clear();
            var set = GetActiveSet(prefab);
            if (set == null) return;
            results.AddRange(set);
        }

        /// <summary>Fills 'results' (cleared first) with the component of each spawned instance.</summary>
        public void GetSpawnedNonAlloc<T>(T typePrefab, List<T> results) where T : Component
        {
            results.Clear();
            var set = GetActiveSet(typePrefab.gameObject);
            if (set == null) return;

            foreach (var go in set)
                if (go.TryGetComponent(out T comp))
                    results.Add(comp);
        }

        /// <summary>Snapshot list of spawned instances. Safe to iterate while returning objects to the pool.</summary>
        public List<GameObject> GetSpawned(GameObject prefab)
        {
            var list = new List<GameObject>();
            GetSpawnedNonAlloc(prefab, list);
            return list;
        }

        /// <summary>Snapshot list of components on spawned instances.</summary>
        public List<T> GetSpawned<T>(T typePrefab) where T : Component
        {
            var list = new List<T>();
            GetSpawnedNonAlloc(typePrefab, list);
            return list;
        }

        // ════════════════════════════════════════════════════════════════════════
        // ReturnObjectToPool — with optional delay (mirrors Object.Destroy signature)
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>Return an object to its pool immediately.</summary>
        public void ReturnObjectToPool(GameObject obj)
            => ReturnObjectToPoolInternal(obj);

        /// <summary>Return an object to its pool after a delay (mirrors Object.Destroy(obj, t)).</summary>
        public void ReturnObjectToPool(GameObject obj, float delay)
        {
            if (delay <= 0f) { ReturnObjectToPoolInternal(obj); return; }
            StartCoroutine(ReturnAfterDelay(obj, delay));
        }

        private IEnumerator ReturnAfterDelay(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj != null) ReturnObjectToPoolInternal(obj); // may have been destroyed meanwhile
        }

        private void ReturnObjectToPoolInternal(GameObject obj)
        {
            if (!cloneToPrefabMap.TryGetValue(obj, out GameObject prefab))
            {
                Debug.LogWarning($"Trying to return an object that is not pooled: {obj.name}");
                return;
            }

            Transform holder = prefabToHolder[prefab];
            if (holder != null && obj.transform.parent != holder)
                obj.transform.SetParent(holder, worldPositionStays: false);

            if (objectPools.TryGetValue(prefab, out var pool))
                pool.Release(obj);
        }
    }
}