using UIGen;
using UnityEngine;
using UnityEngine.UIElements;

public class UIGameplayRootPanel : BindedPanel
{
    [Header("Scriptable Object Views")]
    [SerializeField] public GameplayHeaderViewSO headerSO;
    [SerializeField] public FarmViewSO farmSO;
    [SerializeField] public GameplayShopViewSO shopSO;
    [SerializeField] public StoringSpaceViewSO storingSO;

    [Header("Sub-Panel Controllers (Optional)")]
    [SerializeField] public UIGameplayHeaderPanel headerPanel;
    [SerializeField] public UIFarmPanel farmPanel;
    [SerializeField] public UIGameplayShopPanel shopPanel;
    [SerializeField] public UIStoringSpacePanel storingPanel;

    protected override object DataSource => headerSO != null ? headerSO.headerView : null;

    protected override void OnBound()
    {
        base.OnBound();
        Debug.Log("[UIGameplayRootPanel] Root Gameplay UI successfully bound.");
    }
}
