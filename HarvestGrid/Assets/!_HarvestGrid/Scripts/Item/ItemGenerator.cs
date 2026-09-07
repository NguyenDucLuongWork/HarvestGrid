using LgTyLib.Core;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ItemGenerator : BaseSingleton<ItemGenerator>
{
    [Header("Item Settings")]
    public GameObject itemToExport;

    [Header("Export Settings")]
    public string folder = "Assets/!_HarvestGrid/Prefabs/Items";

    [ContextMenu("ExportToPrefab")]
    public void ExportToPrefab()
    {
#if UNITY_EDITOR
        if (itemToExport == null)
        {
            Debug.LogError("Item To Export is null!");
            return;
        }

        // Create folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder(folder))
        {
            CreateFolderRecursive(folder);
        }

        string prefabPath = $"{folder}/{itemToExport.name}.prefab";

        // Save GameObject as Prefab
        PrefabUtility.SaveAsPrefabAsset(
            itemToExport,
            prefabPath
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Prefab exported successfully: {prefabPath}");
#else
        Debug.LogWarning("ExportToPrefab only works inside Unity Editor.");
#endif
    }

#if UNITY_EDITOR
    private void CreateFolderRecursive(string path)
    {
        string[] folders = path.Split('/');

        string currentPath = folders[0];

        for (int i = 1; i < folders.Length; i++)
        {
            string newFolder = currentPath + "/" + folders[i];

            if (!AssetDatabase.IsValidFolder(newFolder))
            {
                AssetDatabase.CreateFolder(currentPath, folders[i]);
            }

            currentPath = newFolder;
        }
    }
#endif
}