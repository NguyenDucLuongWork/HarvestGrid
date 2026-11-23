using LgTyLib.Modules.AutoBindedPanel;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

[CreateAssetMenu(fileName = "FarmViewSO", menuName = "Scriptable Objects/View/FarmViewSO")]
public class FarmViewSO : ViewSO<Farm>
{
    // ══ Stats synced to UXML labels ══════════════════════════════════════════
    [UIField.Sync(nameof(totalSlotsLink))]
    public string totalSlots;

    [UIField.Sync(nameof(harvestableLink))]
    public string harvestable;

    [UIField.Sync(nameof(growingLink))]
    public string growing;

    public VisualTreeAsset slotsTemplate;
    [UIField.List(Template = nameof(slotsTemplate), View = typeof(FarmSlotView))]
    public IList slots => Data?.FarmSlots;



    // ── Computed links ───────────────────────────────────────────────────────
    string totalSlotsLink  => Data != null ? Data.FarmSlots.Count.ToString() : "0";
    string harvestableLink => Data != null ? Data.GetHarvestablePlants().Count.ToString() : "0";
    string growingLink     => Data != null ? Data.GetGrowablePlants().Count.ToString() : "0";
}
public class FarmSlotView : BindedView<FarmSlot>   // [Serializable] isn't needed on a plain view
{
    [UIField.Sync(nameof(slotSpriteLink))] public Sprite slotSprite;
    [UIField.Sync(nameof(plantSpriteLink))] public Sprite plantSprite;
    [UIField.Sync(nameof(statusLink))] public string status;

    Sprite slotSpriteLink
    {
        get
        {
            var mgr = AssetManager.Instance;
            if (mgr?.slotSpriteByLevels == null || mgr.slotSpriteByLevels.Length == 0) return null;
            int lvl = Item?.level ?? 0;
            return mgr.slotSpriteByLevels[Mathf.Clamp(lvl, 0, mgr.slotSpriteByLevels.Length - 1)];
        }
    }

    Sprite plantSpriteLink => Item?.Plant?.CurrentState?.Sprite;

    string statusLink
    {
        get
        {
            if (Item == null || Item.IsEmpty()) return "empty";
            return Item.Plant.IsFullyGrown ? "harvestable" : "growing";
        }
    }
}