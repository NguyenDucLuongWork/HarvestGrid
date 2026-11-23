using LgTyLib.Modules.AutoBindedPanel;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class UIBottomBarPanel : BindedPanel
{
    [Header("Data")]
    [SerializeField]
    public BottomBarViewSO so;

    [Header("Panel Renderers")]
    [SerializeField] private GameObject homePanelRenderer;
    [SerializeField] private GameObject shopPanelRenderer;
    [SerializeField] private GameObject loadPanelRenderer;
    [SerializeField] private GameObject settingPanelRenderer;
    [SerializeField] private GameObject bagPanelRenderer;

    private Coroutine switchPanelCoroutine;
    private string pendingTabKey = "home";

    protected override object DataSource =>
        so != null ? so.bottomBar : null;

    [UIAction("home")]
    public void OnHomeClicked()
    {
        SelectTab("home");
    }

    [UIAction("shop")]
    public void OnShopClicked()
    {
        SelectTab("shop");
    }

    [UIAction("load")]
    public void OnLoadClicked()
    {
        SelectTab("load");
    }

    [UIAction("setting")]
    public void OnSettingClicked()
    {
        SelectTab("setting");
    }

    [UIAction("bag")]
    public void OnBagClicked()
    {
        SelectTab("bag");
    }

    public void SelectTab(string tabKey)
    {
        if (string.IsNullOrWhiteSpace(tabKey))
            return;

        tabKey = tabKey.ToLowerInvariant();

        if (!IsValidTab(tabKey))
            return;

        if (so != null)
        {
            so.SetActiveTab(tabKey);
            Refresh();
        }

        UpdateTabVisuals(tabKey);

        // Keep only the latest requested tab.
        pendingTabKey = tabKey;

        if (switchPanelCoroutine != null)
        {
            StopCoroutine(switchPanelCoroutine);
            switchPanelCoroutine = null;
        }

        switchPanelCoroutine =
            StartCoroutine(ApplyPanelSwitchNextFrame());
    }

    protected override void OnBound()
    {
        base.OnBound();

        string activeTab =
            so != null &&
            so.bottomBar != null &&
            !string.IsNullOrEmpty(so.bottomBar.currentTab)
                ? so.bottomBar.currentTab.ToLowerInvariant()
                : "home";

        if (!IsValidTab(activeTab))
            activeTab = "home";

        pendingTabKey = activeTab;

        UpdateTabVisuals(activeTab);

        if (switchPanelCoroutine != null)
        {
            StopCoroutine(switchPanelCoroutine);
            switchPanelCoroutine = null;
        }

        switchPanelCoroutine =
            StartCoroutine(ApplyPanelSwitchNextFrame());
    }

    private IEnumerator ApplyPanelSwitchNextFrame()
    {
        // Wait until the current UI Toolkit update cycle has finished.
        yield return null;

        UpdatePanelRenderers(pendingTabKey);

        switchPanelCoroutine = null;
    }

    private bool IsValidTab(string tabKey)
    {
        switch (tabKey)
        {
            case "home":
            case "shop":
            case "load":
            case "setting":
            case "bag":
                return true;

            default:
                return false;
        }
    }

    private void UpdateTabVisuals(string activeKey)
    {
        if (root == null)
            return;

        string[] tabs =
        {
            "bag",
            "load",
            "home",
            "shop",
            "setting"
        };

        foreach (string tab in tabs)
        {
            Button button = root.Q<Button>("btn-" + tab);

            if (button == null)
                continue;

            if (string.Equals(
                    tab,
                    activeKey,
                    StringComparison.OrdinalIgnoreCase))
            {
                button.AddToClassList("on");
            }
            else
            {
                button.RemoveFromClassList("on");
            }
        }
    }

    private void UpdatePanelRenderers(string activeKey)
    {
        SetRendererActive(homePanelRenderer, activeKey == "home");
        SetRendererActive(shopPanelRenderer, activeKey == "shop");
        SetRendererActive(loadPanelRenderer, activeKey == "load");
        SetRendererActive(settingPanelRenderer, activeKey == "setting");
        SetRendererActive(bagPanelRenderer, activeKey == "bag");
    }

    private void SetRendererActive(GameObject panelRenderer, bool active)
    {
        if (panelRenderer != null &&
            panelRenderer.activeSelf != active)
        {
            panelRenderer.SetActive(active);
        }
    }

    private void OnDisable()
    {
        if (switchPanelCoroutine != null)
        {
            StopCoroutine(switchPanelCoroutine);
            switchPanelCoroutine = null;
        }
    }
}
