using LgTyLib.Core;
using LgTyLib.Modules.DataPersistence;
using LgTyLib.SceneManagement;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelsManager : BaseSingleton<LevelsManager>
{
    [SerializeField] private LevelGroup levelGroup;
    [SerializeField] private LevelSO[] levels;
    public LevelSO[] Levels => levels;
    [SerializeField] private List<PlayTime> finishedLevels = new();

    [SerializeField] private Button levelBtn;
    [SerializeField] private GameplaySceneDataSO gameplaySceneDataSO;

    private void Start()
    {
        DataPersistenceManager.Instance.LoadUserData(
            UserManager.Instance.UserName
        );

        if (SceneManager.GetActiveScene().name == "GameplayScene")
            return;

        levelBtn.onClick.AddListener(LoadNextLevel);

        UpdateLevelButton();
        levelGroup?.UpdateLevels();
    }

    protected override void OnDestroy()
    {
        if (levelBtn != null)
            levelBtn.onClick.RemoveListener(LoadNextLevel);
        base.OnDestroy();
    }

    public void LoadLevel(LevelSO levelSO)
    {
        gameplaySceneDataSO.level = levelSO;
        SceneLoader.Instance.LoadScene("GameplayScene");
    }

    private void UpdateLevelButton()
    {
        int nextLevel = GetNextLevel();

        TextMeshProUGUI text =
            levelBtn.GetComponentInChildren<TextMeshProUGUI>();

        if (nextLevel < 0)
        {
            text.text = "All Levels Completed";
            levelBtn.interactable = false;
            return;
        }

        text.text = "Level " + (nextLevel);
        levelBtn.interactable = true;
    }

    public void LoadNextLevel()
    {
        int nextLevel = GetNextLevel();

        if (nextLevel < 0 || nextLevel >= levels.Length)
        {
            Debug.LogWarning("No more levels available.");
            return;
        }

        gameplaySceneDataSO.level = levels[nextLevel];

        SceneLoader.Instance.LoadScene("GameplayScene");
    }

    public IReadOnlyList<PlayTime> FinishedLevels => finishedLevels;

    public void LoadUserData(UserData userData)
    {
        finishedLevels = userData.finishedLevels ?? new List<PlayTime>();
    }

    public void SaveUserData(ref UserData userData)
    {
        userData.finishedLevels = finishedLevels;
    }

    public int GetLevelIndex(int levelID)
    {
        if (levels != null)
        {
            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] != null && levels[i].levelID == levelID)
                    return i;
            }
        }
        return levelID;
    }

    public void FinishLevel(int level, PlayTime playTime)
    {
        int index = GetLevelIndex(level);
        if (index < 0 || index >= levels.Length)
        {
            Debug.LogError($"Invalid level index: {index} (level ID: {level})");
            return;
        }

        if (playTime == null)
        {
            Debug.LogError("PlayTime cannot be null.");
            return;
        }

        // Level already has a recorded completion time.
        if (index < finishedLevels.Count && finishedLevels[index] != null)
        {
            PlayTime bestTime = finishedLevels[index];

            // Replace only if the new time is better.
            if (playTime.TotalSeconds < bestTime.TotalSeconds)
            {
                finishedLevels[index] = playTime;
            }
        }
        else
        {
            // Fill missing indices if necessary.
            while (finishedLevels.Count < index)
            {
                finishedLevels.Add(null);
            }

            if (index == finishedLevels.Count)
            {
                finishedLevels.Add(playTime);
            }
            else
            {
                finishedLevels[index] = playTime;
            }
        }
        UserManager.Instance.SetFinishedLevels(finishedLevels);
    }

    public bool IsLevelFinished(int level)
    {
        int index = GetLevelIndex(level);
        return index >= 0
            && index < finishedLevels.Count
            && finishedLevels[index] != null;
    }

    public bool TryGetBestTime(int level, out PlayTime playTime)
    {
        playTime = null;
        int index = GetLevelIndex(level);

        if (index < 0 || index >= finishedLevels.Count || finishedLevels[index] == null)
            return false;

        playTime = finishedLevels[index];
        return true;
    }

    private int GetNextLevel()
    {
        for (int i = 0; i < levels.Length; i++)
        {
            if (!IsLevelFinished(i))
                return i;
        }

        return -1;
    }
}
