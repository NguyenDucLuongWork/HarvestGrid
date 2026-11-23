using System;
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
    public string title = "🌱 Mua ô đất";

    public int slotIndex = 3;

    public int slotLevel = 0;

    public int price = 200;

    public int playerMoney = 1250;

    public string warning = "";

    public bool CanAfford => playerMoney >= price;

    public void RefreshDerived()
    {
        title = slotLevel <= 0 ? "🌱 Mua ô đất" : "⬆ Nâng cấp ô đất";
        warning = CanAfford ? "" : "Không đủ xu!";
    }
}
