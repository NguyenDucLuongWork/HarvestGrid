using System.Collections.Generic;
using UIGen;
using UnityEngine;
using UnityEngine.UIElements;

public class UIGameplayHeaderPanel : BindedPanel
{
    [SerializeField]
    public GameplayHeaderViewSO so;

    [Tooltip("Sub-UXML template for each crop chip")]
    [SerializeField]
    public VisualTreeAsset cropChipTemplate;

    protected override object DataSource => so != null ? so.headerView : null;

    [UIAction("shop-toggle")]
    public void OnShopToggleClicked()
    {
        Debug.Log("[UIGameplayHeaderPanel] Shop button clicked!");
        // Hook to Shop toggle or GameplayScene shop trigger if desired
        if (GameplayScene.Instance != null)
        {
            // Can trigger shop animation/visibility
        }
    }

    protected override void OnBound()
    {
        base.OnBound();
        RefreshHeaderVisuals();
    }

    public void RefreshHeaderVisuals()
    {
        if (root == null || so == null || so.headerView == null) return;

        UpdateProgressBar();
        PopulateCropChips();
    }

    public void UpdateProgressBar()
    {
        if (root == null || so == null) return;

        var xpFill = root.Q<VisualElement>("xp-fill");
        if (xpFill != null)
        {
            float pct = Mathf.Clamp01(so.headerView.progress) * 100f;
            xpFill.style.width = Length.Percent(pct);
        }
    }

    public void PopulateCropChips()
    {
        if (root == null) return;

        var container = root.Q<VisualElement>("chips-container");
        if (container == null) return;

        container.Clear();

        if (so == null || so.headerView == null || so.headerView.crops == null) return;

#if UNITY_EDITOR
        if (cropChipTemplate == null)
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("UICropChip t:VisualTreeAsset");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                cropChipTemplate = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            }
        }
#endif

        if (cropChipTemplate == null)
        {
            Debug.LogWarning("[UIGameplayHeaderPanel] Missing 'cropChipTemplate' (UICropChip.uxml).", this);
            return;
        }

        foreach (var crop in so.headerView.crops)
        {
            var chipInstance = cropChipTemplate.Instantiate();
            BindCropChip(chipInstance, crop);
            container.Add(chipInstance);
        }
    }

    private void BindCropChip(VisualElement chipElement, CropChipData crop)
    {
        var lblIcon = chipElement.Q<Label>("lbl-icon");
        var imgIcon = chipElement.Q<VisualElement>("img-icon");
        var lblStock = chipElement.Q<Label>("lbl-stock");
        var lblTier = chipElement.Q<Label>("lbl-tier");
        var lblCount = chipElement.Q<Label>("lbl-count");
        var lblPrice = chipElement.Q<Label>("lbl-price");
        var chipRoot = chipElement.Q<VisualElement>("crop-chip-root") ?? chipElement;

        if (crop.iconSprite != null && imgIcon != null)
        {
            imgIcon.style.display = DisplayStyle.Flex;
            imgIcon.style.backgroundImage = new StyleBackground(crop.iconSprite);
            if (lblIcon != null) lblIcon.style.display = DisplayStyle.None;
        }
        else if (lblIcon != null)
        {
            lblIcon.style.display = DisplayStyle.Flex;
            lblIcon.text = crop.icon;
            if (imgIcon != null) imgIcon.style.display = DisplayStyle.None;
        }

        if (lblStock != null)
        {
            lblStock.text = crop.growingCount.ToString();
            lblStock.style.display = crop.growingCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (lblTier != null)
        {
            lblTier.text = crop.need > 0 ? $"{crop.tier}★" : "";
            lblTier.style.display = crop.need > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (lblCount != null)
        {
            lblCount.text = crop.CountDisplay;
        }

        if (lblPrice != null)
        {
            lblPrice.text = $"🪙 {crop.sellPrice}";
        }

        if (crop.IsCompleted)
        {
            chipRoot.AddToClassList("done");
        }
        else
        {
            chipRoot.RemoveFromClassList("done");
        }

        // Click to sell 1 crop
        chipRoot.RegisterCallback<ClickEvent>(evt =>
        {
            if (crop.have > 0)
            {
                crop.have--;
                so.headerView.coins += crop.sellPrice;
                Debug.Log($"[UIGameplayHeaderPanel] Sold 1 {crop.cropName} for {crop.sellPrice} coins!");
                Refresh();
                RefreshHeaderVisuals();
            }
        });
    }
}
