using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ItemFootprintSO))]
public class ItemFootprintSOEditor : Editor
{
    private ItemFootprintSO targetSO;

    private void OnEnable()
    {
        targetSO = (ItemFootprintSO)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField(
            "Footprint Size",
            EditorStyles.boldLabel
        );

        targetSO.width = EditorGUILayout.IntField(
            "Width",
            targetSO.width
        );

        targetSO.height = EditorGUILayout.IntField(
            "Height",
            targetSO.height
        );

        EditorGUILayout.Space();

        if (GUILayout.Button("Initialize Grid"))
        {
            Undo.RecordObject(targetSO, "Initialize Footprint Grid");

            targetSO.InitializeGrid();

            EditorUtility.SetDirty(targetSO);
        }

        if (GUILayout.Button("Load From Footprint"))
        {
            Undo.RecordObject(targetSO, "Load Footprint");

            targetSO.LoadFromFootprint();

            EditorUtility.SetDirty(targetSO);
        }

        EditorGUILayout.Space();

        DrawGrid();

        EditorGUILayout.Space();

        if (GUILayout.Button("Save To Footprint"))
        {
            Undo.RecordObject(targetSO, "Save Footprint");

            targetSO.SaveToFootprint();

            EditorUtility.SetDirty(targetSO);
            AssetDatabase.SaveAssets();
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField(
            "Saved Footprint",
            EditorStyles.boldLabel
        );

        EditorGUI.BeginDisabledGroup(true);

        if (targetSO.footprint != null &&
            targetSO.footprint.Requiring != null)
        {
            EditorGUILayout.LabelField(
                "Size",
                $"{targetSO.footprint.Requiring.GetLength(0)} x " +
                $"{targetSO.footprint.Requiring.GetLength(1)}"
            );
        }
        else
        {
            EditorGUILayout.LabelField("Empty");
        }

        EditorGUI.EndDisabledGroup();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawGrid()
    {
        if (targetSO.width <= 0 || targetSO.height <= 0)
            return;

        EditorGUILayout.LabelField(
            "Footprint Grid",
            EditorStyles.boldLabel
        );

        for (int y = targetSO.height - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();

            for (int x = 0; x < targetSO.width; x++)
            {
                bool value = targetSO.GetCell(x, y);

                bool newValue = GUILayout.Toggle(
                    value,
                    "",
                    GUI.skin.button,
                    GUILayout.Width(30),
                    GUILayout.Height(30)
                );

                if (newValue != value)
                {
                    Undo.RecordObject(
                        targetSO,
                        "Change Footprint Cell"
                    );

                    targetSO.SetCell(x, y, newValue);

                    EditorUtility.SetDirty(targetSO);
                }
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}