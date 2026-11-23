using LgTyLib.Modules.AutoBindedPanel;
using UnityEngine;
using UnityEngine.UIElements;

public class UILoadPanel : BindedPanel
{
    [SerializeField]
    public LoadViewSO so;

    [Tooltip("Sub-UXML template for each level item card")]
    [SerializeField]
    public VisualTreeAsset levelItemTemplate;

    protected override object DataSource => so != null ? so.loadView : null;

    protected override void OnBound()
    {
        base.OnBound();

        if (so == null && UIManager.HasInstance)
            so = UIManager.Instance.loadViewSO;

        if (LevelsManager.HasInstance)
            LevelsManager.Instance.SyncLoadView(so);

        PopulateLevelList();
        Refresh();
    }

    public void PopulateLevelList()
    {
        if (root == null) return;

        var container = root.Q<ScrollView>("levels-scroll") ?? root.Q<VisualElement>("levels");
        if (container == null)
        {
            Debug.LogWarning("[UILoadPanel] Missing 'levels-scroll' or 'levels' container in UXML.");
            return;
        }

        container.Clear();

        if (so == null || so.loadView == null || so.loadView.levels == null) return;

#if UNITY_EDITOR
        if (levelItemTemplate == null)
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("UILevelItem t:VisualTreeAsset");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                levelItemTemplate = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            }
        }
#endif

        if (levelItemTemplate == null)
        {
            Debug.LogWarning("[UILoadPanel] Please assign 'levelItemTemplate' (UILevelItem.uxml) in Inspector.", this);
            return;
        }

        foreach (var level in so.loadView.levels)
        {
            var itemInstance = levelItemTemplate.Instantiate();
            BindLevelItem(itemInstance, level);
            container.Add(itemInstance);
        }
    }

    private void BindLevelItem(VisualElement item, LevelItemData data)
    {
        var lvRoot = item.Q<VisualElement>("lv-root");
        var lblNum = item.Q<Label>("lbl-num");
        var lblTitle = item.Q<Label>("lbl-title");
        var lblBadge = item.Q<Label>("lbl-badge");
        var reqContainer = item.Q<VisualElement>("req-container");
        var lblTime = item.Q<Label>("lbl-time");
        var btnRank = item.Q<Button>("btn-rank");
        var btnGo = item.Q<Button>("btn-go");
        var rankBox = item.Q<VisualElement>("rank-box");

        int displayIndex = data.levelId > 0 ? data.levelId : data.levelIndex + 1;
        if (lblNum != null) lblNum.text = displayIndex.ToString();
        if (lblTitle != null) lblTitle.text = data.levelName;

        if (lblBadge != null && lvRoot != null)
        {
            lblBadge.RemoveFromClassList("done");
            lblBadge.RemoveFromClassList("go");
            lvRoot.RemoveFromClassList("lock");

            switch (data.status)
            {
                case LevelStatus.Completed:
                    lblBadge.text = "✔ Hoàn thành";
                    lblBadge.AddToClassList("done");
                    break;
                case LevelStatus.InProgress:
                    lblBadge.text = "▶ Đang chơi";
                    lblBadge.AddToClassList("go");
                    break;
                case LevelStatus.Locked:
                default:
                    lblBadge.text = "🔒 Khóa";
                    lvRoot.AddToClassList("lock");
                    break;
            }
        }

        if (reqContainer != null)
        {
            reqContainer.Clear();
            if (data.requirements != null)
            {
                foreach (var req in data.requirements)
                {
                    var row = new VisualElement();
                    row.AddToClassList("req-row");

                    var icon = req.IsMet ? "✅" : "⬜";
                    var nameLbl = new Label($"{icon} {req.description}");
                    nameLbl.AddToClassList("req-desc");

                    var progLbl = new Label($"{req.current}/{req.target}");
                    progLbl.AddToClassList("t");

                    row.Add(nameLbl);
                    row.Add(progLbl);
                    reqContainer.Add(row);
                }
            }
        }

        if (lblTime != null)
            lblTime.text = $"⏱ Thời gian: {data.completionTime}";

        bool isLocked = data.status == LevelStatus.Locked;

        if (btnGo != null)
        {
            btnGo.SetEnabled(!isLocked);
            btnGo.text = data.status == LevelStatus.Completed ? "Chơi lại" : "Chơi";
            btnGo.clicked += () => OnPlayLevel(data);
        }

        if (btnRank != null)
        {
            btnRank.SetEnabled(!isLocked);
            btnRank.clicked += () =>
            {
                if (rankBox != null)
                    rankBox.ToggleInClassList("show");
            };
        }

        var meTime = item.Q<Label>("lbl-me-time");
        if (meTime != null)
            meTime.text = data.completionTime;
    }

    void OnPlayLevel(LevelItemData data)
    {
        if (data == null || data.status == LevelStatus.Locked) return;
        if (!LevelsManager.HasInstance) return;

        if (data.levelSO != null)
        {
            LevelsManager.Instance.LoadLevel(data.levelSO);
            return;
        }

        LevelSO[] all = LevelsManager.Instance.Levels;
        if (all != null && data.levelIndex >= 0 && data.levelIndex < all.Length)
            LevelsManager.Instance.LoadLevel(all[data.levelIndex]);
    }
}
