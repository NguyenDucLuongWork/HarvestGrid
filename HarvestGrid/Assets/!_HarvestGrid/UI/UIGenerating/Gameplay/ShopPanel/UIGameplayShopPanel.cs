using System.Collections.Generic;
using UIGen;
using UnityEngine;
using UnityEngine.UIElements;

public class UIGameplayShopPanel : BindedPanel
{
    [SerializeField]
    public GameplayShopViewSO so;

    [Tooltip("Sub-UXML template for each shop offer card")]
    [SerializeField]
    public VisualTreeAsset shopOfferTemplate;

    protected override object DataSource => so != null ? so.shopView : null;

    [UIAction("reroll")]
    public void OnRerollClicked()
    {
        Debug.Log("[UIGameplayShopPanel] Reroll shop requested!");

        if (Shop.HasInstance)
        {
            Shop.Instance.RerollShop();
            SyncWithShopMono();
        }
        else if (so != null)
        {
            so.InitializeDefaultOffers();
        }

        Refresh();
        PopulateOffers();
    }

    protected override void OnBound()
    {
        base.OnBound();
        SyncWithShopMono();
        PopulateOffers();
    }

    public void SyncWithShopMono()
    {
        if (Shop.HasInstance && Shop.Instance.currentItems != null && so != null)
        {
            var items = Shop.Instance.currentItems;
            if (items.Count > 0)
            {
                so.shopView.offers.Clear();
                foreach (var item in items)
                {
                    if (item == null) continue;

                    var offer = new GameplayShopOfferData
                    {
                        itemId = item.Id,
                        itemName = item.Name,
                        iconSprite = item.Icon,
                        price = item.Price,
                        currency = "coin",
                        isSold = false,
                        isHeld = false
                    };

                    if (item.Footprint != null && item.Footprint.Requiring != null)
                    {
                        int w = item.Footprint.Requiring.GetLength(0);
                        int h = item.Footprint.Requiring.GetLength(1);
                        offer.width = w;
                        offer.height = h;
                        offer.footprint = new bool[w * h];

                        for (int x = 0; x < w; x++)
                            for (int y = 0; y < h; y++)
                                offer.footprint[(h - 1 - y) * w + x] = item.Footprint.Requiring[x, y];
                    }

                    so.shopView.offers.Add(offer);
                }
            }
        }
    }

    public void PopulateOffers()
    {
        if (root == null) return;

        var container = root.Q<VisualElement>("offers-container");
        if (container == null) return;

        container.Clear();

        if (so == null || so.shopView == null || so.shopView.offers == null) return;

#if UNITY_EDITOR
        if (shopOfferTemplate == null)
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("UIShopOfferItem t:VisualTreeAsset");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                shopOfferTemplate = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            }
        }
#endif

        if (shopOfferTemplate == null)
        {
            Debug.LogWarning("[UIGameplayShopPanel] Missing 'shopOfferTemplate' (UIShopOfferItem.uxml).", this);
            return;
        }

        foreach (var offer in so.shopView.offers)
        {
            var cardInstance = shopOfferTemplate.Instantiate();
            BindOfferCard(cardInstance, offer);
            container.Add(cardInstance);
        }
    }

    private void BindOfferCard(VisualElement cardElement, GameplayShopOfferData offer)
    {
        var lblName = cardElement.Q<Label>("lbl-name");
        var lblInfo = cardElement.Q<Label>("lbl-info");
        var btnBuy = cardElement.Q<Button>("btn-buy");
        var lblIcon = cardElement.Q<Label>("lbl-icon");
        var imgIcon = cardElement.Q<VisualElement>("img-icon");
        var fpGrid = cardElement.Q<VisualElement>("fp-preview-grid");
        var cardRoot = cardElement.Q<VisualElement>("offer-card") ?? cardElement;

        cardElement.dataSource = offer;

        if (lblName != null) lblName.text = offer.itemName;
        if (lblInfo != null) lblInfo.text = offer.PriceDisplay;

        // Icon
        var imgImage = cardElement.Q<Image>("img-icon");
        if (offer.iconSprite != null)
        {
            if (imgImage != null)
            {
                imgImage.sprite = offer.iconSprite;
                imgImage.style.display = DisplayStyle.Flex;
            }
            else if (imgIcon != null)
            {
                imgIcon.style.backgroundImage = new StyleBackground(offer.iconSprite);
                imgIcon.style.display = DisplayStyle.Flex;
            }
            if (lblIcon != null) lblIcon.style.display = DisplayStyle.None;
        }
        else
        {
            if (imgImage != null) imgImage.style.display = DisplayStyle.None;
            else if (imgIcon != null) imgIcon.style.display = DisplayStyle.None;
            if (lblIcon != null)
            {
                lblIcon.style.display = DisplayStyle.Flex;
                lblIcon.text = string.IsNullOrEmpty(offer.icon) ? "🌾" : offer.icon;
            }
        }

        // Mini Footprint Grid Preview
        if (fpGrid != null && offer.footprint != null)
        {
            fpGrid.Clear();
            int w = Mathf.Max(1, offer.width);
            int h = Mathf.Max(1, offer.height);

            for (int r = 0; r < h; r++)
            {
                var row = new VisualElement();
                row.AddToClassList("fp-preview-row");
                for (int c = 0; c < w; c++)
                {
                    var cell = new VisualElement();
                    cell.AddToClassList("fp-preview-cell");
                    int idx = r * w + c;
                    bool on = idx >= 0 && idx < offer.footprint.Length && offer.footprint[idx];
                    cell.AddToClassList(on ? "on" : "off");
                    row.Add(cell);
                }
                fpGrid.Add(row);
            }
        }

        // Card state styling
        if (offer.isSold)
            cardRoot.AddToClassList("sold");
        else
            cardRoot.RemoveFromClassList("sold");

        if (offer.isHeld)
            cardRoot.AddToClassList("held");
        else
            cardRoot.RemoveFromClassList("held");

        // Buy button
        if (btnBuy != null)
        {
            btnBuy.text = offer.ButtonText;
            btnBuy.SetEnabled(!offer.isSold && !offer.isHeld);

            btnBuy.clicked += () =>
            {
                if (offer.isSold || offer.isHeld) return;
                Debug.Log($"[UIGameplayShopPanel] Bought offer: {offer.itemName} for {offer.price} {offer.currency}");
                offer.isHeld = true;
                btnBuy.text = "DRAG";
                cardRoot.AddToClassList("held");
            };
        }
    }
}
