using System;
using UIGen;
using UnityEngine;

[CreateAssetMenu(fileName = "SlotUpgradeViewSO", menuName = "Scriptable Objects/View/SlotUpgradeViewSO")]
public class SlotUpgradeViewSO : ScriptableObject
{
    [SerializeField]
    public SlotUpgradeView slotUpgrade = new SlotUpgradeView();

    public void SetData(int slotIndex, int slotLevel, int price, int playerMoney)
    {
        slotUpgrade.slotIndex = slotIndex;
        slotUpgrade.slotLevel = slotLevel;
        slotUpgrade.price = price;
        slotUpgrade.playerMoney = playerMoney;
        slotUpgrade.RefreshDerived();
    }

    public void SyncMoneyFromUser()
    {
        if (UserManager.Instance != null && UserManager.Instance.userData != null)
            slotUpgrade.playerMoney = UserManager.Instance.userData.coin;
        slotUpgrade.RefreshDerived();
    }
}

[Serializable]
public class SlotUpgradeView
{
    [UIField(ControlType.Label, Label = "Title")]
    public string title = "🌱 Mua ô đất";

    [UIField(ControlType.Label, Label = "Ô đất")]
    public int slotIndex = 3;

    [UIField(ControlType.Label, Label = "Cấp hiện tại")]
    public int slotLevel = 0;

    [UIField(ControlType.Label, Label = "Giá")]
    public int price = 200;

    [UIField(ControlType.Label, Label = "Xu của bạn")]
    public int playerMoney = 1250;

    [UIField(ControlType.Label, Label = "Warning")]
    public string warning = "";

    public bool CanAfford => playerMoney >= price;

    public void RefreshDerived()
    {
        title = slotLevel <= 0 ? "🌱 Mua ô đất" : "⬆ Nâng cấp ô đất";
        warning = CanAfford ? "" : "Không đủ xu!";
    }
}
