using System;
using System.Collections.Generic;
using UIGen;
using UnityEngine;

public enum LevelStatus
{
    Locked,
    InProgress,
    Completed
}

[Serializable]
public class LevelRequirementData
{
    public string description = "Thu hoạch nông sản";
    public int current = 5;
    public int target = 5;
    public bool IsMet => current >= target;
}

[Serializable]
public class LevelItemData
{
    public int levelIndex = 0;
    public int levelId = 1;
    public string levelName = "Vườn rau đầu tiên";
    public LevelStatus status = LevelStatus.Completed;
    public string completionTime = "02:14";
    public List<LevelRequirementData> requirements = new();
    public LevelSO levelSO;
}

[Serializable]
public class LoadView
{
    [UIField(ControlType.Label, Label = "Total Levels")]
    public int totalLevels = 6;
    public List<LevelItemData> levels = new();
}

[CreateAssetMenu(fileName = "LoadViewSO", menuName = "Scriptable Objects/View/LoadViewSO")]
public class LoadViewSO : ScriptableObject
{
    [SerializeField]
    public LoadView loadView = new LoadView();

    private void Reset()
    {
        InitializeDefaultLevels();
    }

    public void SetLevelsData(LevelSO[] levels, IReadOnlyList<PlayTime> finishedLevels)
    {
        loadView.levels ??= new List<LevelItemData>();
        loadView.levels.Clear();

        if (levels == null)
        {
            loadView.totalLevels = 0;
            return;
        }

        int nextPlayable = -1;
        for (int i = 0; i < levels.Length; i++)
        {
            if (!IsFinished(finishedLevels, i))
            {
                nextPlayable = i;
                break;
            }
        }

        for (int i = 0; i < levels.Length; i++)
        {
            LevelSO level = levels[i];
            if (level == null) continue;

            level.EnsureInitialized();

            bool finished = IsFinished(finishedLevels, i);
            PlayTime best = finished ? finishedLevels[i] : null;

            LevelStatus status;
            if (finished)
                status = LevelStatus.Completed;
            else if (nextPlayable < 0 || i == nextPlayable)
                status = LevelStatus.InProgress;
            else
                status = LevelStatus.Locked;

            loadView.levels.Add(new LevelItemData
            {
                levelIndex = i,
                levelId = level.levelID,
                levelName = ResolveLevelName(level),
                status = status,
                completionTime = FormatPlayTime(best),
                requirements = BuildRequirements(level, finished),
                levelSO = level
            });
        }

        loadView.totalLevels = loadView.levels.Count;
    }

    static bool IsFinished(IReadOnlyList<PlayTime> finishedLevels, int index)
    {
        return finishedLevels != null
            && index >= 0
            && index < finishedLevels.Count
            && finishedLevels[index] != null;
    }

    static string ResolveLevelName(LevelSO level)
    {
        if (level == null) return "Level";
        if (!string.IsNullOrEmpty(level.name) && level.name != "LevelSO")
            return level.name;
        return "Level " + level.levelID;
    }

    static string FormatPlayTime(PlayTime playTime)
    {
        if (playTime == null) return "--:--";
        int total = Mathf.Max(0, Mathf.FloorToInt(playTime.TotalSeconds));
        int minutes = total / 60;
        int seconds = total % 60;
        return $"{minutes:00}:{seconds:00}";
    }

    static List<LevelRequirementData> BuildRequirements(LevelSO level, bool finished)
    {
        var list = new List<LevelRequirementData>();
        if (level?.requiringCrops == null) return list;

        foreach (var pair in level.requiringCrops)
        {
            Crop crop = pair.Key;
            int target = Mathf.Max(0, pair.Value);
            string cropName = crop != null && !string.IsNullOrEmpty(crop.Name)
                ? crop.Name
                : crop != null ? crop.CropID : "Nông sản";
            int stars = crop != null ? crop.Stars : 0;

            list.Add(new LevelRequirementData
            {
                description = stars > 0
                    ? $"Thu hoạch {cropName} ({stars}★)"
                    : $"Thu hoạch {cropName}",
                current = finished ? target : 0,
                target = target
            });
        }

        return list;
    }

    public void InitializeDefaultLevels()
    {
        loadView.levels = new List<LevelItemData>
        {
            new LevelItemData
            {
                levelIndex = 0,
                levelId = 1,
                levelName = "Vườn rau đầu tiên",
                status = LevelStatus.Completed,
                completionTime = "02:14",
                requirements = new List<LevelRequirementData>
                {
                    new LevelRequirementData { description = "Thu hoạch cà rốt", current = 5, target = 5 },
                    new LevelRequirementData { description = "Tưới nước", current = 10, target = 10 }
                }
            },
            new LevelItemData
            {
                levelIndex = 1,
                levelId = 2,
                levelName = "Chuồng gà nhỏ",
                status = LevelStatus.Completed,
                completionTime = "03:48",
                requirements = new List<LevelRequirementData>
                {
                    new LevelRequirementData { description = "Nhặt trứng", current = 10, target = 10 },
                    new LevelRequirementData { description = "Cho gà ăn", current = 6, target = 6 }
                }
            },
            new LevelItemData
            {
                levelIndex = 2,
                levelId = 3,
                levelName = "Cánh đồng ngô",
                status = LevelStatus.InProgress,
                completionTime = "--:--",
                requirements = new List<LevelRequirementData>
                {
                    new LevelRequirementData { description = "Thu hoạch ngô", current = 7, target = 12 },
                    new LevelRequirementData { description = "Bán nông sản", current = 90, target = 200 }
                }
            },
            new LevelItemData
            {
                levelIndex = 3,
                levelId = 4,
                levelName = "Trại bò sữa",
                status = LevelStatus.Locked,
                completionTime = "--:--",
                requirements = new List<LevelRequirementData>
                {
                    new LevelRequirementData { description = "Vắt sữa", current = 0, target = 8 },
                    new LevelRequirementData { description = "Xây chuồng bò", current = 0, target = 1 }
                }
            },
            new LevelItemData
            {
                levelIndex = 4,
                levelId = 5,
                levelName = "Chợ cuối tuần",
                status = LevelStatus.Locked,
                completionTime = "--:--",
                requirements = new List<LevelRequirementData>
                {
                    new LevelRequirementData { description = "Bán 30 món", current = 0, target = 30 },
                    new LevelRequirementData { description = "Đạt 1500 xu", current = 0, target = 1500 }
                }
            },
            new LevelItemData
            {
                levelIndex = 5,
                levelId = 6,
                levelName = "Vườn cây ăn trái",
                status = LevelStatus.Locked,
                completionTime = "--:--",
                requirements = new List<LevelRequirementData>
                {
                    new LevelRequirementData { description = "Trồng 6 cây", current = 0, target = 6 },
                    new LevelRequirementData { description = "Thu hoạch táo", current = 0, target = 15 }
                }
            }
        };
        loadView.totalLevels = loadView.levels.Count;
    }
}
