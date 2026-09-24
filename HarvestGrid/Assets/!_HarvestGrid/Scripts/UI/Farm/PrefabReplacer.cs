using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PrefabReplacer : MonoBehaviour
{
    [Header("Replacement")]
    [SerializeField] private GameObject replacementPrefab;

    [Header("Objects To Replace")]
    [SerializeField] private GameObject[] objects;

    [ContextMenu("Replace All")]
    private void ReplaceAll()
    {
#if UNITY_EDITOR
        if (replacementPrefab == null)
        {
            Debug.LogError("Replacement prefab is not assigned.", this);
            return;
        }

        if (!PrefabUtility.IsPartOfPrefabAsset(replacementPrefab))
        {
            Debug.LogError("Replacement must be a prefab asset.", this);
            return;
        }

        if (objects == null || objects.Length == 0)
        {
            Debug.LogWarning("No objects to replace.", this);
            return;
        }

        Undo.IncrementCurrentGroup();

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Replace UI Prefabs");

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject oldObject = objects[i];

            if (oldObject == null)
                continue;

            // Save transform information
            Transform oldTransform = oldObject.transform;

            Transform parent = oldTransform.parent;
            Vector3 worldPosition = oldTransform.position;
            Quaternion worldRotation = oldTransform.rotation;
            Vector3 worldScale = oldTransform.lossyScale;

            // Instantiate as a real prefab instance.
            GameObject newObject =
                (GameObject)PrefabUtility.InstantiatePrefab(
                    replacementPrefab,
                    parent
                );

            if (newObject == null)
            {
                Debug.LogError(
                    $"Failed to instantiate prefab for {oldObject.name}.",
                    this
                );

                continue;
            }

            Undo.RegisterCreatedObjectUndo(newObject, "Replace UI Prefab");

            Transform newTransform = newObject.transform;

            // Preserve world transform.
            newTransform.position = worldPosition;
            newTransform.rotation = worldRotation;

            // For UI objects, setting lossyScale directly is not ideal.
            // Preserve local scale when parent remains the same.
            newTransform.localScale = oldTransform.localScale;

            // Match sibling position.
            newTransform.SetSiblingIndex(oldTransform.GetSiblingIndex());

            // Replace reference in array.
            objects[i] = newObject;

            // Delete old object.
            Undo.DestroyObjectImmediate(oldObject);
        }

        Undo.CollapseUndoOperations(undoGroup);

        EditorUtility.SetDirty(this);
#endif
    }
}