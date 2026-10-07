using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public partial class GameplayScene
{
    [Header("UI")]
    public ItemInfoPanel itemInfoPanel;

    public RectTransform plantRequieContainer;
    
    public RectTransform wonPanel;
    public RectTransform losePanel;

    public void ShowItemInfo(Item item)
    {
        itemInfoPanel.Display(item);
        itemInfoPanel.gameObject.SetActive(true);
    }

    public void WinGame()
    {
        TimeManager.Instance.Pause();
        wonPanel.gameObject.SetActive(true);
        
    }

    public void LoseGame()
    {
        TimeManager.Instance.Pause();
        losePanel.gameObject.SetActive(true);
    }
}