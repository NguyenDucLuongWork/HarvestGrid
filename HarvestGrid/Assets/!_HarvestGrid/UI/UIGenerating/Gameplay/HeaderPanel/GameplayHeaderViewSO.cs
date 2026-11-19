using System;
using System.Collections.Generic;
using UIGen;
using UnityEngine;

[Serializable]
public class CropChipData
{
    public string cropId = "cuc";
    public string cropName = "Cucumber";
    public string icon = "🥒";
    public Sprite iconSprite;
    public int have = 3;
    public int need = 7;
    public int tier = 2;
    public int growingCount = 9;
    public int sellPrice = 12;

    public bool IsCompleted => need > 0 && have >= need;
    public string CountDisplay => need > 0 ? $"{Mathf.Min(have, need)}/{need}" : have.ToString();
}

[Serializable]
public class GameplayHeaderView
{
    [UIField(ControlType.Label, Label = "Level")]
    public int level = 6;

    [UIField(ControlType.Label, Label = "Time Remaining")]
    public string timeRemaining = "0:38";

    [UIField(ControlType.ProgressBar, Label = "Level Progress", Min = 0, Max = 1)]
    public float progress = 0.65f;

    [UIField(ControlType.Label, Label = "Gems")]
    public int gems = 365;

    [UIField(ControlType.Label, Label = "Coins")]
    public int coins = 1532;

    public List<CropChipData> crops = new();
}

[CreateAssetMenu(fileName = "GameplayHeaderViewSO", menuName = "Scriptable Objects/View/GameplayHeaderViewSO")]
public class GameplayHeaderViewSO : ScriptableObject
{
    [SerializeField]
    public GameplayHeaderView headerView = new GameplayHeaderView();

    private void Reset()
    {
        InitializeDefaultData();
    }

    public void InitializeDefaultData()
    {
        headerView.level = 6;
        headerView.timeRemaining = "0:38";
        headerView.progress = 0.65f;
        headerView.gems = 365;
        headerView.coins = 1532;
        headerView.crops = new List<CropChipData>
        {
            new CropChipData { cropId = "cuc", cropName = "Cucumber", icon = "🥒", have = 3, need = 7, tier = 2, growingCount = 9, sellPrice = 12 },
            new CropChipData { cropId = "wheat", cropName = "Wheat", icon = "🌾", have = 8, need = 8, tier = 2, growingCount = 8, sellPrice = 10 },
            new CropChipData { cropId = "corn", cropName = "Corn", icon = "🌽", have = 4, need = 4, tier = 1, growingCount = 4, sellPrice = 15 }
        };
    }
}
