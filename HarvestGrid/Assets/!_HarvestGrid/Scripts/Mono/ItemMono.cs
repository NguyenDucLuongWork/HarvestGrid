using LgTyLib.Modules.DataPersistence;
using UnityEngine;

public class ItemMono : MonoBehaviour
{
    [SerializeField]
    private Item item;
    public Item Item => item;

    public void Init(Item item)
    {
        this.item = item;
        item.gameObject = this.gameObject;
        gameObject.name = item.Name;
    }

    public void Run()
    {
        item.Start();
    }
}