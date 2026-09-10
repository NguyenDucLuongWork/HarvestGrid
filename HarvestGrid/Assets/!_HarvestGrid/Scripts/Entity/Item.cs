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

    public string Id => id;
    public string Name => name;
    public Sprite Icon => icon;
    public ItemRarity Rarity => rarity;
    public IReadOnlyList<ItemUse> Uses => uses;

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
    }

    public Item Clone()
    {
        return new Item(this);
    }

    public void Start()
    {
        progressTimer.OnCompleted += Use;
        progressTimer.Play();
    }

    public void OnDestroy()
    {
        if (progressTimer == null) return;
        progressTimer.OnCompleted -= Use;
        progressTimer.Stop();
    }

    private void Use()
    {

        Use(new ItemUseContext());
    }

    public void Use(ItemUseContext ctx)
    {
        foreach (var use in uses)
        {
            use.Apply(ctx);
        }
    }

    public void SetItemUse(List<ItemUse> uses)
    {
        this.uses = uses;
    }
}