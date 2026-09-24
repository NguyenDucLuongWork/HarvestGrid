using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StoringSpaceSO))]
public class StoringSpaceSOEditor : Editor
{
    private const float CellSize = 24f;
    private const float CellSpacing = 2f;

    public override void OnInspectorGUI()
    {
        StoringSpaceSO so = (StoringSpaceSO)target;

        serializedObject.Update();

        // --- Width / Height fields ---
        EditorGUI.BeginChangeCheck();
        int newWidth = EditorGUILayout.IntField("Width", so.width);
        int newHeight = EditorGUILayout.IntField("Height", so.height);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(so, "Change Grid Size");
            so.width = Mathf.Max(1, newWidth);
            so.height = Mathf.Max(1, newHeight);
            EditorUtility.SetDirty(so);
        }

        EditorGUILayout.Space();

        // --- Create Grid button ---
        string buttonLabel = so.storingSpace == null
            ? "Create Grid"
            : "Recreate Grid (clears cells & stored objects)";

        if (GUILayout.Button(buttonLabel))
        {
            if (so.storingSpace == null ||
                EditorUtility.DisplayDialog(
                    "Recreate Grid",
                    "This will discard the current grid, its cell data " +
                    "and all stored objects. Continue?",
                    "Yes, recreate",
                    "Cancel"))
            {
                Undo.RecordObject(so, "Create Grid");
                so.CreateGrid();
                EditorUtility.SetDirty(so);
            }
        }

        EditorGUILayout.Space();

        // --- Cell grid ---
        if (so.storingSpace == null)
        {
            EditorGUILayout.HelpBox(
                "No grid created yet. Set a width/height and press " +
                "\"Create Grid\".",
                MessageType.Info);
        }
        else if (!so.HasValidGrid())
        {
            EditorGUILayout.HelpBox(
                "Width/Height no longer match the created grid. " +
                "Press \"Recreate Grid\" to resize (this clears data).",
                MessageType.Warning);
        }
        else
        {
            DrawGrid(so);
        }

        EditorGUILayout.Space();

        // --- Save button ---
        using (new EditorGUI.DisabledScope(!EditorUtility.IsDirty(so)))
        {
            if (GUILayout.Button("Save"))
            {
                EditorUtility.SetDirty(so);
                AssetDatabase.SaveAssetIfDirty(so);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawGrid(StoringSpaceSO so)
    {
        StoringCellType[,] cells = so.storingSpace.Cells;
        int width = cells.GetLength(0);
        int height = cells.GetLength(1);

        EditorGUILayout.LabelField(
            "Cells (click to toggle Available / Unavailable)",
            EditorStyles.boldLabel);

        // Draw rows top-to-bottom so y increases upward like world space.
        for (int y = height - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();

            for (int x = 0; x < width; x++)
            {
                StoringCellType cell = cells[x, y];
                Color previousColor = GUI.backgroundColor;
                GUI.backgroundColor = ColorForCell(cell);

                string label = cell == StoringCellType.Occupied ? "X" : "";

                if (GUILayout.Button(
                        label,
                        GUILayout.Width(CellSize),
                        GUILayout.Height(CellSize)))
                {
                    OnCellClicked(so, x, y, cell);
                }

                GUI.backgroundColor = previousColor;
                GUILayout.Space(CellSpacing);
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    private void OnCellClicked(
        StoringSpaceSO so,
        int x,
        int y,
        StoringCellType current)
    {
        // Occupied cells belong to a stored footprint; don't let
        // clicking silently corrupt that bookkeeping.
        if (current == StoringCellType.Occupied)
        {
            EditorUtility.DisplayDialog(
                "Cell Occupied",
                "This cell is occupied by a stored object. Remove the " +
                "object via RemoveFootprint before editing this cell.",
                "OK");
            return;
        }

        Undo.RecordObject(so, "Toggle Cell");

        StoringCellType next = current == StoringCellType.Available
            ? StoringCellType.Unavailable
            : StoringCellType.Available;

        so.storingSpace.Cells[x, y] = next;

        EditorUtility.SetDirty(so);
    }

    private static Color ColorForCell(StoringCellType cell)
    {
        switch (cell)
        {
            case StoringCellType.Available:
                return Color.white;
            case StoringCellType.Unavailable:
                return new Color(0.35f, 0.35f, 0.35f);
            case StoringCellType.Occupied:
                return new Color(0.9f, 0.4f, 0.4f);
            default:
                return Color.white;
        }
    }
}