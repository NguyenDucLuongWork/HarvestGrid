using System.Collections.Generic;
using UIGen;
using UnityEngine;
using UnityEngine.UIElements;

public class UIShopPanel : BindedPanel
{
    [SerializeField]
    public ShopViewSO so;

    [Tooltip("Sub-UXML template for each shop item card")]
    [SerializeField]
    public VisualTreeAsset shopItemTemplate;

    protected override object DataSource => so != null ? so.shopView : null;

    [UIAction("tab-seed")]
    public void OnTabSeedClicked()
    {
        Debug.Log("[UIShopPanel] Switched to Seed tab");
        SelectCategory(ShopCategory.Seed);
    }

    [UIAction("tab-tool")]
    public void OnTabToolClicked()
    {
        Debug.Log("[UIShopPanel] Switched to Tool tab");
        SelectCategory(ShopCategory.Tool);
    }

    [UIAction("tab-deco")]
    public void OnTabDecoClicked()
    {
        Debug.Log("[UIShopPanel] Switched to Deco tab");
        SelectCategory(ShopCategory.Deco);
    }

    protected override void OnBound()
    {
        base.OnBound();
        ShopCategory initialCat = ShopCategory.Seed;
        if (so != null && !string.IsNullOrEmpty(so.shopView.activeCategory))
        {
            if (System.Enum.TryParse<ShopCategory>(so.shopView.activeCategory, true, out var parsed))
                initialCat = parsed;
        }

        SelectCategory(initialCat);
    }

    public void SelectCategory(ShopCategory category)
    {
        if (so != null)
        {
            so.shopView.activeCategory = category.ToString();
            Refresh();
        }

        UpdateTabVisuals(category);
        PopulateShopGrid(category);
    }

    private void UpdateTabVisuals(ShopCategory category)
    {
        if (root == null) return;

        UpdateTabButton("btn-tab-seed", category == ShopCategory.Seed);
        UpdateTabButton("btn-tab-tool", category == ShopCategory.Tool);
        UpdateTabButton("btn-tab-deco", category == ShopCategory.Deco);
    }

    private void UpdateTabButton(string buttonName, bool isActive)
    {
        var btn = root.Q<Button>(buttonName);
        if (btn != null)
        {
            if (isActive)
                btn.AddToClassList("on");
            else
                btn.RemoveFromClassList("on");
        }
    }

    public void PopulateShopGrid(ShopCategory category)
    {
        if (root == null) return;

        var grid = root.Q<VisualElement>("shop-grid");
        if (grid == null)
        {
            Debug.LogWarning("[UIShopPanel] Missing 'shop-grid' container in UXML.");
            return;
        }

        grid.Clear();

        if (so == null || so.shopView == null || so.shopView.items == null) return;

#if UNITY_EDITOR
        if (shopItemTemplate == null)
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("UIShopItem t:VisualTreeAsset");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                shopItemTemplate = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            }
        }
#endif

        if (shopItemTemplate == null)
        {
            Debug.LogWarning("[UIShopPanel] Please assign 'shopItemTemplate' (UIShopItem.uxml) in Inspector.", this);
            return;
        }

        foreach (var item in so.shopView.items)
        {
            if (item.category != category) continue;

            var itemInstance = shopItemTemplate.Instantiate();
            BindShopItem(itemInstance, item);
            grid.Add(itemInstance);
        }
    }

    private void BindShopItem(VisualElement itemElement, ShopItemData data)
    {
        var lblIcon = itemElement.Q<Label>("lbl-icon");
        var lblName = itemElement.Q<Label>("lbl-name");
        var lblDesc = itemElement.Q<Label>("lbl-desc");
        var btnBuy = itemElement.Q<Button>("btn-buy");

        if (lblIcon != null) lblIcon.text = data.icon;
        if (lblName != null) lblName.text = data.itemName;
        if (lblDesc != null) lblDesc.text = data.description;

        if (btnBuy != null)
        {
            btnBuy.text = $"🪙 {data.price}";
            btnBuy.clicked += () =>
            {
                Debug.Log($"[UIShopPanel] Purchase clicked: Bought '{data.itemName}' ({data.icon}) for {data.price} coins.");
            };
        }
    }
}
