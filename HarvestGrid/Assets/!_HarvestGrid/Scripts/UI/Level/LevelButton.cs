using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelButton : MonoBehaviour
{
    [SerializeField] private Sprite notFinishedBg;
    [SerializeField] private Sprite finishedBg;

    [SerializeField] private TextMeshProUGUI levelID;
    [SerializeField] private TextMeshProUGUI lastFinishTime;
    [SerializeField] private Image bg;

    private LevelSO level;

    public void SetData(LevelSO levelSO)
    {
        level = levelSO;

        levelID.text = "Level " + levelSO.levelID;
        bg.sprite = notFinishedBg;

        if (lastFinishTime != null)
            lastFinishTime.text = "";

        GetComponent<Button>().interactable = true;
    }

    public void SetDataFinished(LevelSO levelSO, PlayTime playTime)
    {
        SetData(levelSO);

        bg.sprite = finishedBg;

        if (lastFinishTime != null)
        {
            lastFinishTime.text =
                $"{playTime.minutes}m {playTime.seconds:F1}s";
        }
    }

    public void LoadLevel()
    {
        if (level == null)
        {
            Debug.LogError("LevelButton has no LevelSO assigned.", this);
            return;
        }

        LevelsManager.Instance.LoadLevel(level);
    }
}