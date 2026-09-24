using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public partial class GameplayScene
{
    [Header("UI")]
    public ItemInfoPanel itemInfoPanel;

    public RectTransform plantRequieContainer;
    

    public void ShowItemInfo(Item item)
    {
        itemInfoPanel.Display(item);
        itemInfoPanel.gameObject.SetActive(true);
    }
}