using TMPro;
using UnityEngine;

public class ItemInShop : MonoBehaviour
{
    [SerializeField]
    private ItemWithFootprintMono itemWithFootprintMonoPrefab;
    [SerializeField]
    private float itemMonoScale = 0.7f;
    [SerializeField]
    private TextMeshProUGUI label;
    [SerializeField]
    private ItemWithFootprintMono itemMono;
    [SerializeField]
    private TextMeshProUGUI moneyLabel;
   
    public void SetData(Item item)
    {
        if (itemMono.bought)
        {
            itemMono = Instantiate(itemWithFootprintMonoPrefab,
                this.transform
                );
            itemMono.transform.localScale = new Vector3(itemMonoScale, itemMonoScale, itemMonoScale );
        }
        itemMono.SetRotateZero();
        label.text = item.Name;
        itemMono.SetData(item);
        moneyLabel.text = item.Price + "";
        moneyLabel.color = Color.white;
    }

    public void ShowInfo()
    {
        GameplayScene.Instance.ShowItemInfo(itemMono.Item);
    }

    public void TryBuy()
    {
        if (itemMono.Buy())
        {
            moneyLabel.text = "Owned";
            moneyLabel.color = Color.gray;
        }
    }
}
