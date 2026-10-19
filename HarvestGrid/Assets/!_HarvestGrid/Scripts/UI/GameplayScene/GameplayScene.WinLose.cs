using LgTyLib.Modules.DataPersistence;
using LgTyLib.SceneManagement;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public partial class GameplayScene
{
    [Header("UI")]
    public ItemInfoPanel itemInfoPanel;

    public RectTransform plantRequieContainer;
    
    public RectTransform wonPanel;
    public TextMeshProUGUI timeText;
    public RectTransform losePanel;

    public void ShowItemInfo(Item item)
    {
        itemInfoPanel.Display(item);
        itemInfoPanel.gameObject.SetActive(true);
    }

    public void WinGame()
    {
        TimeManager.Instance.Pause();
        timeText.text = PassedTimeFormatted;
        UserManager.Instance.Load();
        LevelsManager.Instance.FinishLevel(
            LevelSO.levelID,
            new PlayTime(PassedMinutes, PassedSeconds)
            );
        wonPanel.gameObject.SetActive(true);
    }

    public void LoseGame()
    {
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