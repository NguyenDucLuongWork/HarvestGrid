using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CropUIComponent : MonoBehaviour
{
    [SerializeField]
    private Image icon;
    [SerializeField]
    private TextMeshProUGUI stars;
    [SerializeField]
    private TextMeshProUGUI amountText;
    [SerializeField]
    private TextMeshProUGUI sellPriceText;

    [SerializeField]
    private Crop crop;

    public void SetData(Crop crop, int amount)
    {
        this.crop = crop;
        icon.sprite = crop.Icon;
        stars.text = crop.Stars + "";
        sellPriceText.text = crop.GetSellPrice() + "";
        this.amountText.text = amount + "";
    }

    public void SellOne()
    {
        InventoryMono.Instance.SellOne(crop);
    }
}