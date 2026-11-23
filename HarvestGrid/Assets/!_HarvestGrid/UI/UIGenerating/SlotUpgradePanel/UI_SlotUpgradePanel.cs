using LgTyLib.Modules.AutoBindedPanel;
using UnityEngine;
using UnityEngine.UIElements;

public class UI_SlotUpgradePanel : BindedPanel
{
    [SerializeField]
    public SlotUpgradeViewSO so;

    protected override object DataSource => so != null ? so.slotUpgrade : null;

    public void Open(int slotIndex, int slotLevel, int price)
    {
        if (so == null) return;

        int money = 0;
        if (UserManager.Instance != null && UserManager.Instance.userData != null)
            money = UserManager.Instance.userData.coin;

        so.SetData(slotIndex, slotLevel, price, money);
        gameObject.SetActive(true);
        Refresh();
        UpdateAffordability();
    }

    [UIAction("yes")]
    public void OnYesClicked()
    {
        if (so == null || so.slotUpgrade == null) return;
        if (!so.slotUpgrade.CanAfford)
        {
            Debug.LogWarning("[UI_SlotUpgradePanel] Not enough coins to buy/upgrade.");
            return;
        }

        Debug.Log($"[UI_SlotUpgradePanel] Yes: slot {so.slotUpgrade.slotIndex}, level {so.slotUpgrade.slotLevel}, price {so.slotUpgrade.price}");
        gameObject.SetActive(false);
    }

    [UIAction("no")]
    public void OnNoClicked()
    {
        Debug.Log("[UI_SlotUpgradePanel] Cancelled slot buy/upgrade.");
        gameObject.SetActive(false);
    }

    protected override void OnBound()
    {
        base.OnBound();
        if (so != null)
        {
            so.SyncMoneyFromUser();
            Refresh();
        }
        UpdateAffordability();
    }

    void UpdateAffordability()
    {
        if (root == null || so == null || so.slotUpgrade == null) return;

        bool canAfford = so.slotUpgrade.CanAfford;
        var yes = root.Q<Button>("btn-yes");
        if (yes != null) yes.SetEnabled(canAfford);

        var warn = root.Q<Label>("val-warning");
        if (warn != null)
        {
            if (canAfford) warn.AddToClassList("hidden");
            else warn.RemoveFromClassList("hidden");
        }
    }
}
