using TMPro;
using UnityEngine;

public class ItemInShop : MonoBehaviour
{
    [SerializeField]
    private ItemWithFootprintMono itemWithFootprintMonoPrefab;
    [SerializeField]
    private TextMeshProUGUI label;
    [SerializeField]
    private ItemWithFootprintMono footprintMono;
    [SerializeField]
    private TextMeshProUGUI moneyLabel;
   
    public void SetData(Item item)
    {
        if (footprintMono.bought)
        {
            footprintMono = Instantiate(itemWithFootprintMonoPrefab,
                this.transform
                );
        }
        label.text = item.Name;
        footprintMono.SetData(item);
        moneyLabel.text = item.Price + "";
    }
}
