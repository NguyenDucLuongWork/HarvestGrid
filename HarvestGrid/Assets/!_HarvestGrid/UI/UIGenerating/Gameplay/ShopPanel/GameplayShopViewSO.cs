using System;
using System.Collections.Generic;
using UIGen;
using UnityEngine;

[Serializable]
public class GameplayShopOfferData
{
    public string itemId = "tractor";
    public string itemName = "Mini Tractor";
    public string icon = "🚜";
    public Sprite iconSprite;
    public string resourceType = "sun"; // "sun", "nutrient", "water"
    public string currency = "gem";     // "coin", "gem"
    public int price = 150;
    public int width = 3;
    public int height = 2;
    public bool[] footprint;            // row-major flat array of width * height
    public bool isSold = false;
    public bool isHeld = false;

    public string ResourceIcon => resourceType switch
    {
        "water" => "💧",
        "nutrients" or "nutrient" => "🍃",
        "light" or "sun" => "☀️",
        _ => "💧"
    };

    public string CurrencyIcon => currency == "gem" ? "💎" : "🪙";
    public string PriceDisplay => $"{ResourceIcon} {width}×{height} · {CurrencyIcon} {price:N0}";
    public string ButtonText => isSold ? "SOLD" : isHeld ? "DRAG" : "BUY";
}

[Serializable]
public class GameplayShopView
{
    [UIField(ControlType.Label, Label = "Reroll Gem Cost")]
    public int rerollGems = 10;

    public List<GameplayShopOfferData> offers = new();
}

[CreateAssetMenu(fileName = "GameplayShopViewSO", menuName = "Scriptable Objects/View/GameplayShopViewSO")]
public class GameplayShopViewSO : ScriptableObject
{
    [SerializeField]
    public GameplayShopView shopView = new GameplayShopView();

    private void Reset()
    {
        InitializeDefaultOffers();
    }

    private void OnEnable()
    {
#if UNITY_EDITOR
        EnsureDefaultSprites();
#endif
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureDefaultSprites();
    }

    public void EnsureDefaultSprites()
    {
        if (shopView == null || shopView.offers == null) return;
        foreach (var offer in shopView.offers)
        {
            if (offer.iconSprite == null)
            {
                string id = offer.itemId != null ? offer.itemId.ToLower() : "";
                if (id.Contains("scythe"))
                {
                    var p = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemPrototype>("Assets/!_HarvestGrid/ScriptableObjects/Items/Scythe.asset");
                    if (p != null && p.item != null) offer.iconSprite = p.item.Icon;
                }
                else if (id.Contains("can") || id.Contains("water"))
                {
                    var p = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemPrototype>("Assets/!_HarvestGrid/ScriptableObjects/Items/WaterCan.asset");
                    if (p != null && p.item != null) offer.iconSprite = p.item.Icon;
                }
                else if (id.Contains("tractor") || id.Contains("buff") || id.Contains("lantern"))
                {
                    var p = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemPrototype>("Assets/!_HarvestGrid/ScriptableObjects/Items/Lantern.asset");
                    if (p != null && p.item != null) offer.iconSprite = p.item.Icon;
                }
            }
        }
    }
#endif

    public void InitializeDefaultOffers()
    {
        shopView.rerollGems = 10;
        shopView.offers = new List<GameplayShopOfferData>
        {
            new GameplayShopOfferData
            {
                itemId = "tractor",
                itemName = "Mini Tractor",
                icon = "🚜",
                resourceType = "sun",
                currency = "gem",
                price = 150,
                width = 3,
                height = 2,
                footprint = new bool[] { true, true, true, true, true, true },
                isSold = false,
                isHeld = false
            },
            new GameplayShopOfferData
            {
                itemId = "can",
                itemName = "Watering Can",
                icon = "🪣",
                resourceType = "water",
                currency = "coin",
                price = 300,
                width = 2,
                height = 2,
                footprint = new bool[] { true, true, true, true },
                isSold = false,
                isHeld = false
            },
            new GameplayShopOfferData
            {
                itemId = "scythe",
                itemName = "Super Scythe",
                icon = "🌾",
                resourceType = "sun",
                currency = "coin",
                price = 1200,
                width = 2,
                height = 2,
                footprint = new bool[] { true, true, true, false },
                isSold = true,
                isHeld = false
            }
        };
    }
}
