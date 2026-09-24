using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ItemInfoPanel : MonoBehaviour
{
    [SerializeField]
    private Transform content;
    [SerializeField]
    private TextMeshProUGUI nameLabel;
    [SerializeField]
    private TextMeshProUGUI durationLabel;
    [SerializeField]
    private TextMeshProUGUI priceLabel;

    [SerializeField]
    private Dictionary<ItemUseType, ItemUsePanel> prefabs;

    private readonly List<ItemUsePanel> spawned = new();

    /// <summary>
    /// Clears the panel and spawns one ItemUsePanel per ItemUse on the given item.
    /// </summary>
    public void Display(Item item)
    {
        Clear();

        if (item == null || item.Uses == null)
            return;

        nameLabel.text = item.Name;
        durationLabel.text = item.ProgressTimer.Duration + "s";
        priceLabel.text = item.Price + "";

        foreach (var use in item.Uses)
        {
            if (use == null)
                continue;

            var type = ItemUseTypeAttribute.Resolve(use);

            if (!prefabs.TryGetValue(type, out var prefab) || prefab == null)
            {
                Debug.LogWarning(
                    $"ItemInfoPanel: no prefab registered for ItemUseType.{type}."
                );
                continue;
            }

            var panel = Instantiate(prefab, content);
            panel.Bind(use);
            spawned.Add(panel);
        }
    }

    public void Clear()
    {
        foreach (var panel in spawned)
        {
            if (panel != null)
                Destroy(panel.gameObject);
        }

        spawned.Clear();
    }
}