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
    private TextMeshProUGUI amount;

    public void SetData(Crop crop, int amount)
    {
        icon.sprite = crop.Icon;
        stars.text = crop.Stars + "";
        this.amount.text = amount + "";
    }
}