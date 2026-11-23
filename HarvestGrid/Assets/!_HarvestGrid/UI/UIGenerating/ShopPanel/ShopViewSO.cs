using System;
using System.Collections.Generic;
using UnityEngine;

public enum ShopCategory
{
    Seed,
    Tool,
    Deco
}

[Serializable]
public class ShopItemData
{
    public string icon = "🥕";
    public string itemName = "Cà rốt";
    public string description = "Lớn sau 2 phút";
    public int price = 30;
    public ShopCategory category = ShopCategory.Seed;
}

[Serializable]
public class ShopView
{
    public string activeCategory = "Seed";
    public List<ShopItemData> items = new();
}

[CreateAssetMenu(fileName = "ShopViewSO", menuName = "Scriptable Objects/View/ShopViewSO")]
public class ShopViewSO : ScriptableObject
{
    [SerializeField]
    public ShopView shopView = new ShopView();

    private void Reset()
    {
        InitializeDefaultShop();
    }

    public void InitializeDefaultShop()
    {
        shopView.activeCategory = "Seed";
        shopView.items = new List<ShopItemData>
        {
            // Seed
            new ShopItemData { icon = "🥕", itemName = "Cà rốt", description = "Lớn sau 2 phút", price = 30, category = ShopCategory.Seed },
            new ShopItemData { icon = "🍅", itemName = "Cà chua", description = "Lớn sau 4 phút", price = 55, category = ShopCategory.Seed },
            new ShopItemData { icon = "🌽", itemName = "Ngô", description = "Lớn sau 6 phút", price = 80, category = ShopCategory.Seed },
            new ShopItemData { icon = "🍓", itemName = "Dâu tây", description = "Lớn sau 8 phút", price = 120, category = ShopCategory.Seed },

            // Tool
            new ShopItemData { icon = "💧", itemName = "Bình tưới+", description = "Tưới nhanh gấp đôi", price = 250, category = ShopCategory.Tool },
            new ShopItemData { icon = "🪓", itemName = "Rìu sắt", description = "Chặt cây nhanh hơn", price = 300, category = ShopCategory.Tool },
            new ShopItemData { icon = "🧺", itemName = "Giỏ lớn", description = "Thêm 10 ô túi", price = 400, category = ShopCategory.Tool },
            new ShopItemData { icon = "🚜", itemName = "Máy cày", description = "Cày 3 ô cùng lúc", price = 900, category = ShopCategory.Tool },

            // Deco
            new ShopItemData { icon = "🌻", itemName = "Hoa hướng dương", description = "Tăng độ đẹp +5", price = 90, category = ShopCategory.Deco },
            new ShopItemData { icon = "🪵", itemName = "Hàng rào", description = "Bảo vệ vườn", price = 60, category = ShopCategory.Deco },
            new ShopItemData { icon = "⛲", itemName = "Đài phun nước", description = "Tăng độ đẹp +20", price = 700, category = ShopCategory.Deco },
            new ShopItemData { icon = "🏡", itemName = "Nhà gỗ", description = "Nhà mới cho chủ trại", price = 1500, category = ShopCategory.Deco }
        };
    }
}
