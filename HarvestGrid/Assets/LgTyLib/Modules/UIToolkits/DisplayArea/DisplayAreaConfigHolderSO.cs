using UnityEngine;
using UnityEditor;
using System.Linq;

[CreateAssetMenu(
    fileName = "DisplayAreaConfigHolderSO",
    menuName = "Scriptable Objects/DisplayAreaConfigHolderSO")]
public class DisplayAreaConfigHolderSO : ScriptableObject
{
    static DisplayAreaConfigHolderSO _instance;

    public static DisplayAreaConfigHolderSO Instance
    {
        get
        {
            if (_instance == null)
            {
                string[] guids = AssetDatabase.FindAssets(
                    $"t:{nameof(DisplayAreaConfigHolderSO)}"
                );

                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _instance = AssetDatabase.LoadAssetAtPath<DisplayAreaConfigHolderSO>(path);
                }
                else
                {
                    Debug.LogError(
                        $"{nameof(DisplayAreaConfigHolderSO)} not found. Using defaults."
                    );

                    _instance = CreateInstance<DisplayAreaConfigHolderSO>();
                }
            }

            return _instance;
        }
    }

    public DisplayAreaConfig displayAreaConfig = new DisplayAreaConfig();
}