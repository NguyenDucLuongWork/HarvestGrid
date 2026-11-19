using System;
using System.Collections.Generic;
using UIGen;
using UnityEngine;

[Serializable]
public class FarmSlotData
{
    public int slotIndex;
    public int level; // 0=empty, 1=sprout, 2=single, 3=double, 4=sunflower, 5=ready/max
    public int count = 1; // 1 = single plant, 2 = two side-by-side
    public string cropType = "cuc";
    public string plantName = "Crops";
    public string plantIcon = "🌱";
    public Sprite plantSprite;
    public string requiredResource = "water"; // "water", "nutrients", "light", "growth"
    public int gotAmount = 0;
    public int needAmount = 2;
    public bool isMax = false;
    public bool isLocked = false;

    public string RequirementDisplay => isMax || level >= 5 ? "MAX" : $"{gotAmount}/{needAmount}";
    public string ResourceIcon => requiredResource switch
    {
        "water" => "💧",
        "nutrients" or "nutrient" => "🍃",
        "light" or "sun" => "☀️",
        "growth" => "🌱",
        _ => "💧"
    };
}

[Serializable]
public class FarmView
{
    [UIField(ControlType.Label, Label = "Farm Name")]
    public string farmName = "My Farm";

    [UIField(ControlType.Label, Label = "Columns")]
    public int columns = 5;

    [UIField(ControlType.Label, Label = "Rows")]
    public int rows = 5;

    public List<FarmSlotData> slots = new();
}

[CreateAssetMenu(fileName = "FarmViewSO", menuName = "Scriptable Objects/View/FarmViewSO")]
public class FarmViewSO : ScriptableObject
{
    [SerializeField]
    public FarmView farmView = new FarmView();

    private void Reset()
    {
        InitializeDefaultFarm();
    }

    public void InitializeDefaultFarm()
    {
        farmView.farmName = "My Farm";
        farmView.columns = 5;
        farmView.rows = 5;
        farmView.slots = new List<FarmSlotData>();

        // Exact 25 slots matching the visual mockup (5 rows of 5)
        // Row 0: 🌱 0/2 | 🌱 0/1 | (empty) 0/1 | 🌱 0/2 | 🌱 0/1
        farmView.slots.Add(new FarmSlotData { slotIndex = 0, level = 1, count = 1, plantIcon = "🌱", gotAmount = 0, needAmount = 2 });
        farmView.slots.Add(new FarmSlotData { slotIndex = 1, level = 1, count = 1, plantIcon = "🌱", gotAmount = 0, needAmount = 1 });
        farmView.slots.Add(new FarmSlotData { slotIndex = 2, level = 0, count = 0, plantIcon = "", gotAmount = 0, needAmount = 1 });
        farmView.slots.Add(new FarmSlotData { slotIndex = 3, level = 1, count = 1, plantIcon = "🌱", gotAmount = 0, needAmount = 2 });
        farmView.slots.Add(new FarmSlotData { slotIndex = 4, level = 1, count = 1, plantIcon = "🌱", gotAmount = 0, needAmount = 1 });

        // Row 1: 🌽🌽 0/2 | 🥒🥒 0/2 | 🌻 0/2 | (empty) 0/1 | 🥒🥒 1/2
        farmView.slots.Add(new FarmSlotData { slotIndex = 5, level = 3, count = 2, plantIcon = "🌽", gotAmount = 0, needAmount = 2 });
        farmView.slots.Add(new FarmSlotData { slotIndex = 6, level = 3, count = 2, plantIcon = "🥒", gotAmount = 0, needAmount = 2 });
        farmView.slots.Add(new FarmSlotData { slotIndex = 7, level = 4, count = 1, plantIcon = "🌻", gotAmount = 0, needAmount = 2 });
        farmView.slots.Add(new FarmSlotData { slotIndex = 8, level = 0, count = 0, plantIcon = "", gotAmount = 0, needAmount = 1 });
        farmView.slots.Add(new FarmSlotData { slotIndex = 9, level = 3, count = 2, plantIcon = "🥒", gotAmount = 1, needAmount = 2 });

        // Row 2: 🌻 1/2 | (empty) 0/1 | 🌱 1/2 | 🌻 1/2 | 🌽🌽 1/2
        farmView.slots.Add(new FarmSlotData { slotIndex = 10, level = 4, count = 1, plantIcon = "🌻", gotAmount = 1, needAmount = 2 });
        farmView.slots.Add(new FarmSlotData { slotIndex = 11, level = 0, count = 0, plantIcon = "", gotAmount = 0, needAmount = 1 });
        farmView.slots.Add(new FarmSlotData { slotIndex = 12, level = 1, count = 1, plantIcon = "🌱", gotAmount = 1, needAmount = 2 });
        farmView.slots.Add(new FarmSlotData { slotIndex = 13, level = 4, count = 1, plantIcon = "🌻", gotAmount = 1, needAmount = 2 });
        farmView.slots.Add(new FarmSlotData { slotIndex = 14, level = 3, count = 2, plantIcon = "🌽", gotAmount = 1, needAmount = 2 });

        // Row 3: 🌱 1/2 | 🌱 0/1 | (empty) 0/1 | 🌱 1/2 | 🌻 1/2
        farmView.slots.Add(new FarmSlotData { slotIndex = 15, level = 1, count = 1, plantIcon = "🌱", gotAmount = 1, needAmount = 2 });
        farmView.slots.Add(new FarmSlotData { slotIndex = 16, level = 1, count = 1, plantIcon = "🌱", gotAmount = 0, needAmount = 1 });
        farmView.slots.Add(new FarmSlotData { slotIndex = 17, level = 0, count = 0, plantIcon = "", gotAmount = 0, needAmount = 1 });
        farmView.slots.Add(new FarmSlotData { slotIndex = 18, level = 1, count = 1, plantIcon = "🌱", gotAmount = 1, needAmount = 2 });
        farmView.slots.Add(new FarmSlotData { slotIndex = 19, level = 4, count = 1, plantIcon = "🌻", gotAmount = 1, needAmount = 2 });

        // Row 4: 🌽🌽 1/2 | 🌱 1/2 | 🌻 0/2 | 🌽🌽 1/2 | 🌱 0/2
        farmView.slots.Add(new FarmSlotData { slotIndex = 20, level = 3, count = 2, plantIcon = "🌽", gotAmount = 1, needAmount = 2 });
        farmView.slots.Add(new FarmSlotData { slotIndex = 21, level = 1, count = 1, plantIcon = "🌱", gotAmount = 1, needAmount = 2 });
        farmView.slots.Add(new FarmSlotData { slotIndex = 22, level = 4, count = 1, plantIcon = "🌻", gotAmount = 0, needAmount = 2 });
        farmView.slots.Add(new FarmSlotData { slotIndex = 23, level = 3, count = 2, plantIcon = "🌽", gotAmount = 1, needAmount = 2 });
        farmView.slots.Add(new FarmSlotData { slotIndex = 24, level = 1, count = 1, plantIcon = "🌱", gotAmount = 0, needAmount = 2 });
    }
}
