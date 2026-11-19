using System.Collections.Generic;
using UIGen;
using UnityEngine;
using UnityEngine.UIElements;

public class UIFarmPanel : BindedPanel
{
    [SerializeField]
    public FarmViewSO so;

    [Tooltip("Sub-UXML template for each farm plot slot")]
    [SerializeField]
    public VisualTreeAsset farmSlotTemplate;

    protected override object DataSource => so != null ? so.farmView : null;

    protected override void OnBound()
    {
        base.OnBound();
        SyncWithFarmMono();
        PopulateFarmGrid();
    }

    public void SyncWithFarmMono()
    {
        if (FarmMono.HasInstance && FarmMono.Instance.Farm != null && so != null)
        {
            var farm = FarmMono.Instance.Farm;
            if (farm.FarmSlots != null && farm.FarmSlots.Count > 0)
            {
                so.farmView.slots.Clear();
                for (int i = 0; i < farm.FarmSlots.Count; i++)
                {
                    var slot = farm.FarmSlots[i];
                    var plant = slot.plant;

                    var slotData = new FarmSlotData
                    {
                        slotIndex = i,
                        level = slot.level,
                        isLocked = slot.level <= 0,
                        count = 1
                    };

                    if (plant != null)
                    {
                        slotData.plantName = plant.Name;
                        slotData.plantSprite = plant.CurrentState != null ? plant.CurrentState.Sprite : null;
                        slotData.level = plant.IsFullyGrown ? 5 : (plant.CurrentState != null ? 2 : 1);
                        slotData.count = plant.IsFullyGrown ? 1 : 1;

                        var req = plant.NextStageRequirement;
                        if (req != null && req.Count > 0)
                        {
                            slotData.isMax = false;
                            foreach (var kvp in req)
                            {
                                slotData.requiredResource = kvp.Key.ToString().ToLower();
                                slotData.gotAmount = 0;
                                slotData.needAmount = kvp.Value;
                                break;
                            }
                        }
                        else
                        {
                            slotData.isMax = true;
                        }
                    }
                    else
                    {
                        slotData.plantIcon = "";
                    }

                    so.farmView.slots.Add(slotData);
                }
            }
        }
    }

    public void PopulateFarmGrid()
    {
        if (root == null) return;

        var grid = root.Q<VisualElement>("farm-grid");
        if (grid == null) return;

        grid.Clear();

        if (so == null || so.farmView == null || so.farmView.slots == null) return;

#if UNITY_EDITOR
        if (farmSlotTemplate == null)
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("UIFarmSlot t:VisualTreeAsset");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                farmSlotTemplate = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            }
        }
#endif

        if (farmSlotTemplate == null)
        {
            Debug.LogWarning("[UIFarmPanel] Missing 'farmSlotTemplate' (UIFarmSlot.uxml).", this);
            return;
        }

        int cols = Mathf.Max(1, so.farmView.columns);
        VisualElement currentRow = null;

        for (int i = 0; i < so.farmView.slots.Count; i++)
        {
            if (i % cols == 0)
            {
                currentRow = new VisualElement();
                currentRow.AddToClassList("farm-row");
                grid.Add(currentRow);
            }

            var slot = so.farmView.slots[i];
            var slotInstance = farmSlotTemplate.Instantiate();
            BindFarmSlot(slotInstance, slot);
            currentRow.Add(slotInstance);
        }
    }

    private void BindFarmSlot(VisualElement slotElement, FarmSlotData slot)
    {
        var lblSingle = slotElement.Q<Label>("lbl-single-plant");
        var twoWrap = slotElement.Q<VisualElement>("fs-two-plants");
        var lblTwo1 = slotElement.Q<Label>("lbl-two-1");
        var lblTwo2 = slotElement.Q<Label>("lbl-two-2");
        var imgIcon = slotElement.Q<VisualElement>("img-plant-icon");
        var reqWrap = slotElement.Q<VisualElement>("fs-req-wrap");
        var lblRes = slotElement.Q<Label>("lbl-res-icon");
        var lblReq = slotElement.Q<Label>("lbl-req-text");
        var slotRoot = slotElement.Q<VisualElement>("fs-root") ?? slotElement;

        // Visual presentation
        if (slot.plantSprite != null && imgIcon != null)
        {
            imgIcon.style.display = DisplayStyle.Flex;
            imgIcon.style.backgroundImage = new StyleBackground(slot.plantSprite);
            if (lblSingle != null) lblSingle.style.display = DisplayStyle.None;
            if (twoWrap != null) twoWrap.style.display = DisplayStyle.None;
        }
        else if (slot.level <= 0 || string.IsNullOrEmpty(slot.plantIcon))
        {
            // Empty soil plot
            if (lblSingle != null) lblSingle.style.display = DisplayStyle.None;
            if (twoWrap != null) twoWrap.style.display = DisplayStyle.None;
            if (imgIcon != null) imgIcon.style.display = DisplayStyle.None;
        }
        else if (slot.count >= 2 && twoWrap != null)
        {
            // Two crops side by side (e.g. 🌽🌽, 🥒🥒)
            twoWrap.style.display = DisplayStyle.Flex;
            if (lblSingle != null) lblSingle.style.display = DisplayStyle.None;
            if (imgIcon != null) imgIcon.style.display = DisplayStyle.None;
            if (lblTwo1 != null) lblTwo1.text = slot.plantIcon;
            if (lblTwo2 != null) lblTwo2.text = slot.plantIcon;
        }
        else
        {
            // Single plant (sprout 🌱, sunflower 🌻)
            if (lblSingle != null)
            {
                lblSingle.style.display = DisplayStyle.Flex;
                lblSingle.text = slot.plantIcon;
            }
            if (twoWrap != null) twoWrap.style.display = DisplayStyle.None;
            if (imgIcon != null) imgIcon.style.display = DisplayStyle.None;
        }

        // Bottom requirement text (💧 0/2 or MAX)
        if (reqWrap != null)
        {
            reqWrap.style.display = DisplayStyle.Flex;
            if (slot.isMax)
            {
                if (lblRes != null) lblRes.text = "✨";
                if (lblReq != null) lblReq.text = "MAX";
            }
            else
            {
                if (lblRes != null) lblRes.text = slot.ResourceIcon;
                if (lblReq != null) lblReq.text = $"{slot.gotAmount}/{slot.needAmount}";
            }
        }

        slotRoot.RegisterCallback<ClickEvent>(evt =>
        {
            Debug.Log($"[UIFarmPanel] Clicked farm slot #{slot.slotIndex} ({slot.plantName})");
        });
    }
}
