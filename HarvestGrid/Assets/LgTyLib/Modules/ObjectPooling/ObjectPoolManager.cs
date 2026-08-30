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

        private static GameObject particleSystemsEmpty;
        private static GameObject gameObjectsEmpty;
        private static GameObject soundFXEmpty;

        private static Dictionary<GameObject, ObjectPool<GameObject>> objectPools;
        private static Dictionary<GameObject, GameObject> cloneToPrefabMap;

        public enum PoolType
        {
            ParticleSystems,
            GameObjects,
            SoundFX
        }

        public static PoolType poolingType;

        protected override void Awake()
        {
            base.Awake();
            objectPools = new Dictionary<GameObject, ObjectPool<GameObject>>();
            cloneToPrefabMap = new Dictionary<GameObject, GameObject>();
            SetupEmpties();
        }

        private void SetupEmpties()
        {
            emptyHolder = new GameObject("Object Pools");
            DontDestroyOnLoad(emptyHolder);

            particleSystemsEmpty = new GameObject("Particle Effects");
            particleSystemsEmpty.transform.SetParent(emptyHolder.transform);

            gameObjectsEmpty = new GameObject("GameObjects");
            gameObjectsEmpty.transform.SetParent(emptyHolder.transform);

            soundFXEmpty = new GameObject("Sound FX");
            soundFXEmpty.transform.SetParent(emptyHolder.transform);
        }

        private static void CreatePool(
            GameObject prefab, Vector3 pos, Quaternion rot,
            PoolType poolType = PoolType.GameObjects)
        {
            ObjectPool<GameObject> pool = new ObjectPool<GameObject>(
                createFunc: () => CreateObject(prefab, pos, rot, poolType),
                actionOnGet: OnGetObject,
                actionOnRelease: OnReleaseObject,
                actionOnDestroy: OnDestroyObject
            );

            objectPools.Add(prefab, pool);
        }

        private static GameObject CreateObject(
            GameObject prefab, Vector3 pos, Quaternion rot,
            PoolType poolType = PoolType.GameObjects)
        {
            prefab.SetActive(false);

            GameObject obj = Instantiate(prefab, pos, rot);

            prefab.SetActive(true);

            obj.transform.SetParent(SetParentObject(poolType).transform);

            return obj;
        }

        private static void OnGetObject(GameObject obj) { }

        private static void OnReleaseObject(GameObject obj)
        {
            obj.SetActive(false);
        }

        private static void OnDestroyObject(GameObject obj)
        {
            cloneToPrefabMap.Remove(obj);
        }

        private static GameObject SetParentObject(PoolType poolType)
        {
            switch (poolType)
            {
                case PoolType.ParticleSystems: return particleSystemsEmpty;
                case PoolType.GameObjects: return gameObjectsEmpty;
                case PoolType.SoundFX: return soundFXEmpty;
                default: return gameObjectsEmpty;
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // SpawnObject — world-space, parent optional
        // ════════════════════════════════════════════════════════════════════════

        private static T SpawnObject<T>(
            GameObject objectToSpawn,
            Vector3 spawnPos, Quaternion spawnRot,
            Transform parent = null,
            PoolType poolType = PoolType.GameObjects)
            where T : Object
        {
            if (!objectPools.ContainsKey(objectToSpawn))
                CreatePool(objectToSpawn, spawnPos, spawnRot, poolType);

            GameObject obj = objectPools[objectToSpawn].Get();

            if (obj == null) return null;

            if (!cloneToPrefabMap.ContainsKey(obj))
                cloneToPrefabMap.Add(obj, objectToSpawn);

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

        /// <summary>Spawn a GameObject at a world-space position.</summary>
        public GameObject SpawnObject(
            GameObject objectToSpawn, Vector3 spawnPos,
            PoolType poolType = PoolType.GameObjects)
            => SpawnObject<GameObject>(objectToSpawn, spawnPos, Quaternion.identity, null, poolType);

        /// <summary>Spawn a GameObject at a world-space position under a parent.</summary>
        public GameObject SpawnObject(
            GameObject objectToSpawn, Vector3 spawnPos, Transform parent,
            PoolType poolType = PoolType.GameObjects)
            => SpawnObject<GameObject>(objectToSpawn, spawnPos, Quaternion.identity, parent, poolType);

        /// <summary>Spawn a GameObject at a world-space position and rotation.</summary>
        public GameObject SpawnObject(
            GameObject objectToSpawn, Vector3 spawnPos, Quaternion spawnRot,
            PoolType poolType = PoolType.GameObjects)
            => SpawnObject<GameObject>(objectToSpawn, spawnPos, spawnRot, null, poolType);

        /// <summary>Spawn a GameObject at a world-space position and rotation under a parent.</summary>
        public GameObject SpawnObject(
            GameObject objectToSpawn, Vector3 spawnPos, Quaternion spawnRot, Transform parent,
            PoolType poolType = PoolType.GameObjects)
            => SpawnObject<GameObject>(objectToSpawn, spawnPos, spawnRot, parent, poolType);

        // ── Component overloads ──────────────────────────────────────────────────

        /// <summary>Spawn a Component prefab at a world-space position.</summary>
        public T SpawnObject<T>(
            T typePrefab, Vector3 spawnPos,
            PoolType poolType = PoolType.GameObjects)
            where T : Component
            => SpawnObject<T>(typePrefab.gameObject, spawnPos, Quaternion.identity, null, poolType);

        /// <summary>Spawn a Component prefab at a world-space position under a parent.</summary>
        public T SpawnObject<T>(
            T typePrefab, Vector3 spawnPos, Transform parent,
            PoolType poolType = PoolType.GameObjects)
            where T : Component
            => SpawnObject<T>(typePrefab.gameObject, spawnPos, Quaternion.identity, parent, poolType);

        /// <summary>Spawn a Component prefab at a world-space position and rotation.</summary>
        public T SpawnObject<T>(
            T typePrefab, Vector3 spawnPos, Quaternion spawnRot,
            PoolType poolType = PoolType.GameObjects)
            where T : Component
            => SpawnObject<T>(typePrefab.gameObject, spawnPos, spawnRot, null, poolType);

        /// <summary>Spawn a Component prefab at a world-space position and rotation under a parent.</summary>
        public T SpawnObject<T>(
            T typePrefab, Vector3 spawnPos, Quaternion spawnRot, Transform parent,
            PoolType poolType = PoolType.GameObjects)
            where T : Component
            => SpawnObject<T>(typePrefab.gameObject, spawnPos, spawnRot, parent, poolType);

        // ════════════════════════════════════════════════════════════════════════
        // SpawnObjectLocal — local-space, parent required
        // ════════════════════════════════════════════════════════════════════════

        private T SpawnObjectLocal<T>(
            GameObject objectToSpawn,
            Transform parent,
            Vector3 localPos, Quaternion localRot, Vector3 localScale,
            PoolType poolType = PoolType.GameObjects)
            where T : Object
        {
            if (!objectPools.ContainsKey(objectToSpawn))
                CreatePool(objectToSpawn, parent.position, localRot, poolType);

            GameObject obj = objectPools[objectToSpawn].Get();

            if (obj == null) return null;

            if (!cloneToPrefabMap.ContainsKey(obj))
                cloneToPrefabMap.Add(obj, objectToSpawn);

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

        /// <summary>Spawn a GameObject in local-space under a parent at local zero.</summary>
        public GameObject SpawnObjectLocal(
            GameObject objectToSpawn, Transform parent,
            PoolType poolType = PoolType.GameObjects)
            => SpawnObjectLocal<GameObject>(objectToSpawn, parent, Vector3.zero, Quaternion.identity, Vector3.one, poolType);

        /// <summary>Spawn a GameObject in local-space under a parent at a local position.</summary>
        public GameObject SpawnObjectLocal(
            GameObject objectToSpawn, Transform parent, Vector3 localPos,
            PoolType poolType = PoolType.GameObjects)
            => SpawnObjectLocal<GameObject>(objectToSpawn, parent, localPos, Quaternion.identity, Vector3.one, poolType);

        /// <summary>Spawn a GameObject in local-space under a parent at a local position and rotation.</summary>
        public GameObject SpawnObjectLocal(
            GameObject objectToSpawn, Transform parent, Vector3 localPos, Quaternion localRot,
            PoolType poolType = PoolType.GameObjects)
            => SpawnObjectLocal<GameObject>(objectToSpawn, parent, localPos, localRot, Vector3.one, poolType);

        /// <summary>Spawn a GameObject in local-space under a parent with full local transform.</summary>
        public GameObject SpawnObjectLocal(
            GameObject objectToSpawn, Transform parent, Vector3 localPos, Quaternion localRot, Vector3 localScale,
            PoolType poolType = PoolType.GameObjects)
            => SpawnObjectLocal<GameObject>(objectToSpawn, parent, localPos, localRot, localScale, poolType);

        // ── Component overloads ──────────────────────────────────────────────────

        /// <summary>Spawn a Component prefab in local-space under a parent at local zero.</summary>
        public T SpawnObjectLocal<T>(
            T typePrefab, Transform parent,
            PoolType poolType = PoolType.GameObjects)
            where T : Component
            => SpawnObjectLocal<T>(typePrefab.gameObject, parent, Vector3.zero, Quaternion.identity, Vector3.one, poolType);

        /// <summary>Spawn a Component prefab in local-space under a parent at a local position.</summary>
        public T SpawnObjectLocal<T>(
            T typePrefab, Transform parent, Vector3 localPos,
            PoolType poolType = PoolType.GameObjects)
            where T : Component
            => SpawnObjectLocal<T>(typePrefab.gameObject, parent, localPos, Quaternion.identity, Vector3.one, poolType);

        /// <summary>Spawn a Component prefab in local-space under a parent at a local position and rotation.</summary>
        public T SpawnObjectLocal<T>(
            T typePrefab, Transform parent, Vector3 localPos, Quaternion localRot,
            PoolType poolType = PoolType.GameObjects)
            where T : Component
            => SpawnObjectLocal<T>(typePrefab.gameObject, parent, localPos, localRot, Vector3.one, poolType);

        /// <summary>Spawn a Component prefab in local-space under a parent with full local transform.</summary>
        public T SpawnObjectLocal<T>(
            T typePrefab, Transform parent, Vector3 localPos, Quaternion localRot, Vector3 localScale,
            PoolType poolType = PoolType.GameObjects)
            where T : Component
            => SpawnObjectLocal<T>(typePrefab.gameObject, parent, localPos, localRot, localScale, poolType);

        // ════════════════════════════════════════════════════════════════════════
        // ReturnObjectToPool — with optional delay (mirrors Object.Destroy signature)
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>Return an object to its pool immediately.</summary>
        public void ReturnObjectToPool(GameObject obj, PoolType poolType = PoolType.GameObjects)
            => Instance.ReturnObjectToPoolInternal(obj, poolType);

        /// <summary>Return an object to its pool after a delay (mirrors Object.Destroy(obj, t)).</summary>
        public void ReturnObjectToPool(GameObject obj, float delay, PoolType poolType = PoolType.GameObjects)
        {
            if (delay <= 0f)
            {
                Instance.ReturnObjectToPoolInternal(obj, poolType);
                return;
            }
            Instance.StartCoroutine(Instance.ReturnAfterDelay(obj, delay, poolType));
        }

        private IEnumerator ReturnAfterDelay(GameObject obj, float delay, PoolType poolType)
        {
            yield return new WaitForSeconds(delay);
            ReturnObjectToPoolInternal(obj, poolType);
        }

        private void ReturnObjectToPoolInternal(GameObject obj, PoolType poolType)
        {
            if (!cloneToPrefabMap.TryGetValue(obj, out GameObject prefab))
            {
                Debug.LogWarning($"Trying to return an object that is not pooled: {obj.name}");
                return;
            }

            GameObject parentObject = SetParentObject(poolType);
            if (obj.transform.parent != parentObject.transform)
                obj.transform.SetParent(parentObject.transform);

            if (objectPools.TryGetValue(prefab, out ObjectPool<GameObject> pool))
                pool.Release(obj);
        }
    }
}