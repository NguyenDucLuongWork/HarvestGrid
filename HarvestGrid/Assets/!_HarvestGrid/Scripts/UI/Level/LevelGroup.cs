using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
public class LevelGroup : MonoBehaviour
{
    [SerializeField] private LevelButton levelPrefab;

    private GridLayoutGroup gridLayout;

    private void Awake()
    {
        gridLayout = GetComponent<GridLayoutGroup>();
    }

    public void UpdateLevels()
    {
        if (levelPrefab == null)
        {
            Debug.LogError("Level prefab is not assigned.", this);
            return;
        }

        LevelsManager manager = LevelsManager.Instance;

        // Remove previously generated buttons.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        LevelSO[] levels = manager.Levels;

        for (int i = 0; i < levels.Length; i++)
        {
            LevelSO level = levels[i];

            if (level == null)
                continue;

            LevelButton button = Instantiate(levelPrefab, transform);

            if (manager.TryGetBestTime(i, out PlayTime bestTime))
            {
                button.SetDataFinished(level, bestTime);
            }
            else
            {
                button.SetData(level);
            }

            // Connect the button click to loading this level.
            Button uiButton = button.GetComponent<Button>();
            uiButton.onClick.RemoveAllListeners();
            uiButton.onClick.AddListener(button.LoadLevel);
        }
    }
}