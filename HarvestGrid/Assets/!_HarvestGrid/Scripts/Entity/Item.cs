using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class Item : ICloneable<Item>
{
    // Attibute
    [SerializeField]
    private string id;
    [SerializeField]
    private string name;
    [SerializeField]
    private Sprite icon;
    [SerializeField]
    private ItemRarity rarity;
    [SerializeReference] 
    private List<ItemUse> uses;
    [SerializeField]
    private int price;
    [SerializeField]
    private Footprint footprint;

    // Getter
    public string Id => id;
    public string Name => name;
    public Sprite Icon => icon;
    public ItemRarity Rarity => rarity;
    public IReadOnlyList<ItemUse> Uses => uses;
    public int Price => price;
    public Footprint Footprint => footprint;

    //References
    [SerializeField]
    private ProgressTimer progressTimer;
    public ProgressTimer ProgressTimer => progressTimer;

    //Visual
    public GameObject gameObject;

    public Item(Item original)
    {
        this.id = original.Id;
        this.name = original.Name;
        this.icon = original.Icon;
        this.rarity = original.Rarity;
        this.uses = new List<ItemUse>();
        foreach (var use in original.uses)
            this.uses.Add(use.Clone());
        this.progressTimer = original.ProgressTimer.Clone();
        this.price = original.Price;
        this.footprint = original.Footprint.Clone();
    }

    public Item Clone()
    {
        return new Item(this);
    }

    public void Start()
    {
        progressTimer.OnCompleted -= Use;   // prevent stacking
        progressTimer.OnCompleted += Use;
        progressTimer.Play();
    }

    public void OnDestroy()
    {
        if (progressTimer == null) return;
        progressTimer.OnCompleted -= Use;
        progressTimer.Stop();
    }

    // Item.Use()
    public void Use()
    {
        foreach (var use in uses)
        {
            if (use == null) continue;

            FarmSlot target = FarmMono.Instance.GetTarget(use);
            if (target == null) continue;

            var ctx = new ItemUseContext      // new context per use
            {
                Item = this,
                Use = use,
                TargetSlot = target,
            };

            ItemManager.Instance.ApplyItemEffect(ctx);
        }
    }

    public void SetItemUse(List<ItemUse> uses)
    {
        this.uses = uses;
    }

    public void SetFootprint(Footprint footprint)
    {
        this.footprint = footprint;
    }
}