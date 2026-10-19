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

    public void FinishLevel(int level, PlayTime playTime)
    {
        if (level < 0 || level >= levels.Length)
        {
            Debug.LogError($"Invalid level index: {level}");
            return;
        }

        if (playTime == null)
        {
            Debug.LogError("PlayTime cannot be null.");
            return;
        }

        // Level already has a recorded completion time.
        if (level < finishedLevels.Count && finishedLevels[level] != null)
        {
            PlayTime bestTime = finishedLevels[level];

            // Replace only if the new time is better.
            if (playTime.TotalSeconds < bestTime.TotalSeconds)
            {
                finishedLevels[level] = playTime;
            }
        }
        else
        {
            // Fill missing indices if necessary.
            while (finishedLevels.Count < level)
            {
                finishedLevels.Add(null);
            }

            if (level == finishedLevels.Count)
            {
                finishedLevels.Add(playTime);
            }
            else
            {
                finishedLevels[level] = playTime;
            }
        }
        UserManager.Instance.SetFinishedLevels(finishedLevels);
    }

    public bool IsLevelFinished(int level)
    {
        return level >= 0
            && level < finishedLevels.Count
            && finishedLevels[level] != null;
    }

    public bool TryGetBestTime(int level, out PlayTime playTime)
    {
        playTime = null;

        if (!IsLevelFinished(level))
            return false;

        playTime = finishedLevels[level];
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