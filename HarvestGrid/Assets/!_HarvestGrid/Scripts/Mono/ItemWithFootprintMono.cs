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
    [SerializeField]
    private FootprintRequiringVisual predictionVisual;
    [SerializeField]
    private int rotated;
    public int Rotated => rotated;

    [SerializeField]
    private Vector2Int storedPivot;
    public Vector2Int StorePivot => storedPivot;

    public bool bought { get; private set;  }
    [SerializeField]
    private StoredObject storedObject;

    public bool isAddedToInventory { get; private set; }

    public void SetData(Item item)
    {
        isAddedToInventory = false;
        bought = false;
        GetComponent<DragableUGUI>().Interactable = false;
        gridLayoutGroupHelper.ShowImages();

        this.item = item;
        this.image.sprite = item.Icon;

        OnDataFootprintChanged();
        this.image.SetNativeSize();
        rotated = 0;
    }

    public void SetStoredObject(StoredObject storedObject)
    {
        this.storedObject = storedObject;
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
        StoringSpaceMono.Instance.AutoUpdateDataAndRefreshUI();
        gridLayoutGroupHelper.HideImages();
        LateSnap();
        
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
        for (int y = 0; y < height; y++)
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
            LateSnap();
        }
        else
        {
            // placement was rejected — snap back / handle failure here
        }
    }

    private IEnumerator LateFixSnapNextFrame(Transform transformToSet, Vector2Int pivot)
    {
        yield return null; // wait 1 frame
        gridLayoutGroupHelper.Snap(StoringSpaceMono.Instance.gridLayoutGroupHelper,
            (RectTransform)transformToSet,
            pivot
            );
        predictionVisual.Hide();
    }
    public bool TryToSnapToStoringSpace()
    {
        var storingHelper = StoringSpaceMono.Instance.gridLayoutGroupHelper;
        // 1. Where would this land?
        if (!gridLayoutGroupHelper.TryComputeSnapCellIndex(storingHelper, out Vector2Int pivot))
        {
            HandleCannotSnap();
            return false;
        }

        // 2. Is that actually a legal placement per the footprint's real shape
        if (!StoringSpaceMono.Instance.TryPlaceFootprint(item.Footprint, pivot))
        {

            HandleCannotSnap();
            return false;
        }
            

        AddToInventory(pivot);
        
        return true;
    }

    private void HandleCannotSnap()
    {
        // Cannot snap
        if (!isAddedToInventory)
        {
            transform.SetParent(InventoryMono.Instance.itemTemporaryHolder);
            transform.localPosition = Vector3.zero;
            transform.localScale = Vector3.one * 0.6f;
            predictionVisual.Hide();

        }
    }

    private void AddToInventory(Vector2Int pivot)
    {
        storedPivot = pivot;

        if (!isAddedToInventory)
        {
            ItemFactory.Instance.SpawnItemWithoutClone(item);
            storedObject = new StoredObject(item, rotated, pivot);
            InventoryMono.Instance.Inventory.AddItem(storedObject);
            isAddedToInventory = true;
            this.transform.SetParent(ItemFactory.Instance.itemPlaceHolder, true);
            transform.localScale = Vector3.one;
        }
        storedObject.Pivot = storedPivot;
        storedObject.Rotated = rotated;
    }

    public void ForceAddToInventory(StoredObject existing)
    {
        // Use this when LOADING a save: 'existing' already lives in
        // Inventory.Items, so this only rebuilds the visual/grid state
        // and reserves cells — it never re-adds to inventory data or
        // spawns an extra pickup via ItemFactory.
        if (existing == null || existing.Item == null)
            return;

        bought = true;
        isAddedToInventory = true;
        item = existing.Item;
        image.sprite = item.Icon;
        storedObject = existing;
        rotated = existing.Rotated;
        this.storedPivot = existing.Pivot;
        image.gameObject.transform.localRotation = Quaternion.Euler(0f, 0f, rotated * -90f);

        // The saved Item already contains its serialized footprint shape.
        // Reapplying the rotation here would rotate it a second time on load.
        OnDataFootprintChanged();
        gridLayoutGroupHelper.HideImages();
        image.SetNativeSize();

        if (!PlaceAndSnap(existing.Pivot))
        {
            Debug.LogWarning(
                $"[{nameof(ItemWithFootprintMono)}] ForceAddToInventory: failed to restore '{item.Name}' at {existing.Pivot} — cells may already be occupied."
            );
            return;
        }
        
        isAddedToInventory = true;
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


    [ContextMenu("LateSnap")]
    public void LateSnap()
    {

        StartCoroutine(LateFixSnapNextFrame(transform, storedPivot));
    }

    public void RemoveFromInventory()
    {
        Debug.Log("Removing Item");
        InventoryMono.Instance.Inventory.RemoveItem(storedObject);
        isAddedToInventory = false;
    }

    public void SetRotateZero()
    {
        if (item == null)
            return;

        // Rotate footprint back to its 0-degree orientation.
        // Current state:
        // 0 -> no rotation
        // 1 -> rotate 270 degrees (3 clockwise rotations)
        // 2 -> rotate 180 degrees (2 rotations)
        // 3 -> rotate 90 degrees (1 rotation)
        int rotationsToReset = (4 - rotated) % 4;

        for (int i = 0; i < rotationsToReset; i++)
        {
            item.Footprint.Rotate();
        }

        rotated = 0;

        // Reset visual rotation
        image.gameObject.transform.localRotation = Quaternion.identity;

        // Rebuild footprint UI
        OnDataFootprintChanged();
        image.SetNativeSize();

        // Keep stored data synchronized
        if (storedObject != null)
        {
            storedObject.Rotated = 0;
            storedObject.Pivot = storedPivot;
        }
    }

    public void SnapFootprintToRequiringWhileDragging()
    {
        var storingHelper = StoringSpaceMono.Instance.gridLayoutGroupHelper;

        // The ORIGINAL helper stays exactly where the drag put it —
        // it's only used to read the candidate pivot, never moved by us.
        if (!gridLayoutGroupHelper.TryComputeSnapCellIndex(storingHelper, out Vector2Int pivot))
        {
            predictionVisual.Hide();
            return;
        }

        predictionVisual.Show();
        predictionVisual.SetFootprint(item.Footprint, rotated);
        predictionVisual.SnapTo(storingHelper, pivot);
    }

    public void Scale(float scale)
    {
        transform.localScale = new Vector3(scale, scale, scale);
    }

    public void OnDragStarted()
    {
        predictionVisual.transform.localPosition = Vector3.zero;
        predictionVisual.Show();
        predictionVisual.SetFootprint(item.Footprint, rotated);

        if (isAddedToInventory)
        {
            RemoveFromInventory();
        }
        else
        {
            Scale(1f);
        }
    }

    public void OnDragEnded()
    {
        if (TryToSnapToStoringSpace())
        {
            
            LateSnap();
        }
        

    }

    public bool Buy()
    {
        if (bought)
            return false;

        if (item == null)
            return false;

        int price = item.Price;

        if (price <= 0)
        {
            bought = true;
            return false;
        }

        if (!InventoryMono.Instance.RemoveMoney(price))
        {
            // Not enough money
            Debug.Log("Have no enough money");
            return false;
        }

        bought = true;
        GetComponent<DragableUGUI>().Interactable = true;
        gridLayoutGroupHelper.HideImages();

        return true;
    }
}