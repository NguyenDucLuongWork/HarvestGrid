using LgTyLib.Modules.DataPersistence;
using UnityEngine;

[RequireComponent(typeof(ItemUIComponent))]
public class ItemMono : MonoBehaviour
{
    [SerializeField]
    private Item item;

    public Item Item => item;

    private ItemUIComponent ui;

    private void Awake()
    {
        ui = GetComponent<ItemUIComponent>();

        if (ui == null)
        {
            Debug.LogError($"{name}: ItemUIComponent is missing!", this);
        }
    }

    public void Init(Item item)
    {
        if (item == null)
        {
            Debug.LogError($"{name}: Cannot initialize ItemMono with null Item.", this);
            return;
        }

        this.item = item;
        item.gameObject = gameObject;

        gameObject.name = item.Name;

        // Initialize UI
        ui.SetItem(item);

        // Listen for progress changes
        if (ui == null) ui = GetComponent<ItemUIComponent>();
        item.ProgressTimer.OnProgressChanged += UpdateProgress;
    }

    public void Run()
    {
        if (item == null)
        {
            Debug.LogWarning($"{name}: ItemMono has not been initialized.");
            return;
        }

        item.Start();
    }

    private void OnDestroy()
    {
        if (item?.ProgressTimer != null)
        {
            item.ProgressTimer.OnProgressChanged -= UpdateProgress;
        }
    }

    private void UpdateProgress(float progress)
    {
        Debug.LogWarning("Setting progress");
        if (ui == null)
            return;

        ui.SetProgress(progress);
    }
}