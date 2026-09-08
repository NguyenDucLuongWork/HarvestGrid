using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class Item : ICloneable<Item>
{
    [SerializeField]
    private string id;
    [SerializeField]
    private string name;
    [SerializeField]
    private Sprite icon;
    [SerializeField]
    private ProgressTimer progressTimer;
    [SerializeField]
    private Dictionary<ItemUsesType, int> usesDict;

    public GameObject gameObject;

    public string Id => id;
    public string Name => name;
    public Sprite Icon => icon;
    public ProgressTimer ProgressTimer => progressTimer;
    public Dictionary<ItemUsesType, int> UsesDict => usesDict;

    public Item(Item original)
    {
        this.id = original.Id;
        this.name = original.Name;
        this.icon = original.Icon;
        this.usesDict = new Dictionary<ItemUsesType, int>(original.usesDict);
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
        ItemManager.Instance.Use(this);
    }
}