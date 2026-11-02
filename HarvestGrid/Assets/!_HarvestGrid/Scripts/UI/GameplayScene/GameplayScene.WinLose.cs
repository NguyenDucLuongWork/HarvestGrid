using LgTyLib.Modules.DataPersistence;
using LgTyLib.SceneManagement;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using HarvestGrid.Leaderboard;

public partial class GameplayScene
{
    [Header("UI")]
    public ItemInfoPanel itemInfoPanel;

    public RectTransform plantRequieContainer;
    
    public RectTransform wonPanel;
    public TextMeshProUGUI timeText;
    public RectTransform losePanel;

    private bool isGameOver;

    public void ShowItemInfo(Item item)
    {
        itemInfoPanel.Display(item);
        itemInfoPanel.gameObject.SetActive(true);
    }

    public void WinGame()
    {
        if (isGameOver)
            return;

        isGameOver = true;

        TimeManager.Instance.Pause();
        timeText.text = PassedTimeFormatted;
        UserManager.Instance.Load();

        float finalTime = PassedTime;

        // Check prior best time BEFORE FinishLevel updates it!
        bool isNewBest = true;
        float previousBest = 0f;
        if (LevelsManager.HasInstance && LevelsManager.Instance.TryGetBestTime(LevelSO.levelID, out PlayTime oldRecord) && oldRecord != null)
        {
            previousBest = oldRecord.TotalSeconds;
            isNewBest = (finalTime < previousBest);
        }

        LevelsManager.Instance.FinishLevel(
            LevelSO.levelID,
            new PlayTime(PassedMinutes, PassedSeconds)
        );
        wonPanel.gameObject.SetActive(true);

        // Submit to PlayFab Leaderboard
        var leaderboardHandler = wonPanel.GetComponentInChildren<LevelCompletionLeaderboardHandler>(true);
        if (leaderboardHandler != null)
        {
            leaderboardHandler.HandleLevelCompleted(LevelSO.levelID, finalTime, isNewBest, previousBest);
        }
        else if (LeaderboardManager.HasInstance)
        {
            LeaderboardManager.Instance.SubmitLevelScore(LevelSO.levelID, finalTime, null, isNewBest, previousBest);
        }
    }

    public void LoseGame()
    {
        if (isGameOver)
            return;

        isGameOver = true;

        TimeManager.Instance.Pause();
        losePanel.gameObject.SetActive(true);
    }

    public void BackHome()
    {

    }

    public float PassedTime => Mathf.Max(0f, timeBudget - remainingTime);

    public int PassedMinutes => Mathf.FloorToInt(PassedTime / 60f);

    public float PassedSeconds => PassedTime % 60f;

    public string PassedTimeFormatted =>
        $"{PassedMinutes}m {PassedSeconds:.0}s";
}
