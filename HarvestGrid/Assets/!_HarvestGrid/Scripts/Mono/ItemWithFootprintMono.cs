using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemWithFootprintMono : MonoBehaviour
{
    [Header("Config")]
    public GameObject requiringCellPrefab;
    public GameObject emptyCellPrefab; // optional; leave null to use a blank placeholder

    [SerializeField]
    private Item item;
    public Item Item => item;

    [SerializeField]
    private GridLayoutGroup gridLayoutGroup;
    private readonly List<GameObject> spawnedCells = new();
    [SerializeField]
    private Image image;
    [SerializeField]
    private GridLayoutGroupHelper gridLayoutGroupHelper;

    private int rotated;
    public int Rotated => rotated;

    [SerializeField]
    private Vector2Int storedPivot;
    public Vector2Int StorePivot => storedPivot;

    public bool bought { get; private set;  }
    [SerializeField]
    private StoredObject storedObject;
    private bool addedToInventory;
    public void SetData(Item item)
    {
        addedToInventory = false;
        this.item = item;
        this.image.sprite = item.Icon;

        OnDataFootprintChanged();
        this.image.SetNativeSize();
        rotated = 0;
    }

    public void Rotate()
    {
        if (item == null)
            return;

        // Rotate footprint data
        item.Footprint.Rotate();

        // Track rotation state: 0 -> 1 -> 2 -> 3 -> 0
        rotated = (rotated + 1) % 4;

        // Rotate visual object around Z
        image.gameObject.transform.localRotation = Quaternion.Euler(0f, 0f, rotated * -90f);

        OnDataFootprintChanged();
        this.image.SetNativeSize();
        if (storedObject != null)
        {
            storedObject.Rotated = rotated;
            storedObject.Pivot = storedPivot;
        }
        StoringSpaceMono.Instance.AutoUpdateDataRefreshUI();

    }

    public void OnDataFootprintChanged()
    {
        ClearCells();

        if (item.Footprint == null || requiringCellPrefab == null)
            return;


        bool[,] requiring = item.Footprint.Requiring;
        int width = requiring.GetLength(0);
        int height = requiring.GetLength(1);

        // Flexible constraint can't guarantee a width x height shape; force it.
        gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayoutGroup.constraintCount = width;

        // Sibling order must be row-major, top row first, to match
        // Start Corner = Lower Left / Start Axis = Vertical in the inspector.
        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                bool isRequiring = requiring[x, y];
                GameObject prefab = isRequiring ? requiringCellPrefab : emptyCellPrefab;
                GameObject cell;

                if (prefab != null)
                {
                    cell = Instantiate(prefab, gridLayoutGroup.transform);
                }
                else
                {
                    // No empty-cell prefab assigned: spawn a blank placeholder
                    // so the layout still reserves this slot.
                    cell = new GameObject("EmptyCell", typeof(RectTransform));
                    cell.transform.SetParent(gridLayoutGroup.transform, false);
                }

                spawnedCells.Add(cell);
            }
        }
        gridLayoutGroupHelper.ResizeToFit();
    }

    private void ClearCells()
    {
        for (int i = 0; i < spawnedCells.Count; i++)
        {
            if (spawnedCells[i] != null)
                Destroy(spawnedCells[i]);
        }

        spawnedCells.Clear();
    }

    public void TryToSnapToStoringSpaceFromDrag()
    {
        if (TryToSnapToStoringSpace())
        {
            StartCoroutine(LateFixSnapNextFrame());
        }
        else
        {
            // placement was rejected — snap back / handle failure here
        }
    }

    private IEnumerator LateFixSnapNextFrame()
    {
        yield return null; // wait 1 frame
        LateFixSnap();
    }
    public bool TryToSnapToStoringSpace()
    {
        var storingHelper = StoringSpaceMono.Instance.gridLayoutGroupHelper;

        // 1. Where would this land?
        if (!gridLayoutGroupHelper.TryComputeSnapCellIndex(storingHelper, out Vector2Int pivot))
            return false;

        // 2. Is that actually a legal placement per the footprint's real shape
        if (!StoringSpaceMono.Instance.TryPlaceFootprint(item.Footprint, pivot))
            return false;

        AddToInventory(pivot);
        gridLayoutGroupHelper.Snap(storingHelper, (RectTransform)transform, pivot);
        
        bought = true;
        return true;
    }

    private void AddToInventory(Vector2Int pivot)
    {
        storedPivot = pivot;

        if (!addedToInventory)
        {
            ItemFactory.Instance.SpawnItemWithoutClone(item);
            storedObject = new StoredObject(item, rotated, pivot);
            InventoryMono.Instance.Inventory.AddItem(storedObject);
            addedToInventory = true;
            this.transform.SetParent(ItemFactory.Instance.itemPlaceHolder, true);
            
        }
        storedObject.Pivot = storedPivot;
        storedObject.Rotated = rotated;
        StoringSpaceMono.Instance.AutoUpdateDataRefreshUI();
    }

    public void ForceAddToInventory(StoredObject existing)
    {
        // Use this when LOADING a save: 'existing' already lives in
        // Inventory.Items, so this only rebuilds the visual/grid state
        // and reserves cells — it never re-adds to inventory data or
        // spawns an extra pickup via ItemFactory.
        if (existing == null || existing.Item == null)
            return;

        addedToInventory = false;
        item = existing.Item;
        image.sprite = item.Icon;
        storedObject = existing;

        int normalizedRotation = ((existing.Rotated % 4) + 4) % 4;
        rotated = normalizedRotation;
        image.gameObject.transform.localRotation = Quaternion.Euler(0f, 0f, rotated * -90f);

        // The saved Item already contains its serialized footprint shape.
        // Reapplying the rotation here would rotate it a second time on load.
        OnDataFootprintChanged();
        image.SetNativeSize();

        if (!PlaceAndSnap(existing.Pivot))
        {
            Debug.LogWarning(
                $"[{nameof(ItemWithFootprintMono)}] ForceAddToInventory: failed to restore '{item.Name}' at {existing.Pivot} — cells may already be occupied."
            );
            return;
        }

        addedToInventory = true;
        storedObject.Pivot = storedPivot;
        storedObject.Rotated = rotated;
        bought = true;
    }

    private void ApplyRotationAndFootprint(int rotation)
    {
        rotation = ((rotation % 4) + 4) % 4;

        for (int i = 0; i < rotation; i++)
            item.Footprint.Rotate();

        rotated = rotation;
        image.gameObject.transform.localRotation = Quaternion.Euler(0f, 0f, rotated * -90f);

        OnDataFootprintChanged();
        image.SetNativeSize();
    }

    private bool PlaceAndSnap(Vector2Int pivot)
    {
        if (!StoringSpaceMono.Instance.TryPlaceFootprint(item.Footprint, pivot))
            return false;

        var storingHelper = StoringSpaceMono.Instance.gridLayoutGroupHelper;
        gridLayoutGroupHelper.Snap(storingHelper, (RectTransform)transform, pivot);

        storedPivot = pivot;
        return true;
    }

    [ContextMenu("TestSnap")]
    public void LateFixSnap()
    {
        gridLayoutGroupHelper.Snap(StoringSpaceMono.Instance.gridLayoutGroupHelper, (RectTransform)transform,
            storedPivot
            );
    }

    [ContextMenu("TestSnap2")]
    public void TestSnap2()
    {
        this.TryToSnapToStoringSpaceFromDrag();
    }
}