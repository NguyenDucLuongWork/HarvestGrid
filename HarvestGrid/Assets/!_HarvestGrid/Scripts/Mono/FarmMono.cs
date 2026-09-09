using LgTyLib.Core;
using System.Collections.Generic;
using UnityEngine;

public class FarmMono : BaseSingleton<FarmMono>
{
    [SerializeField]
    private Farm farm;

    private List<FarmSlotMono> slotMonoList = new();
    private void Start()
    {
        if (farm == null)
        {
            farm = new Farm();
        }

        int clearedCount = farm.ValidateData();

        if (clearedCount > 0)
        {
            Debug.Log($"Farm '{name}' validated. Cleared {clearedCount} blank-ID plant(s).", this);
        }
    }
    [ContextMenu("Track Data And Sort")]
    public void TrackDataAndSort()
    {
        if (farm == null)
        {
            farm = new Farm();
        }

        farm.FarmSlots.Clear();
        farm.gameObject = gameObject;

        FarmSlotMono[] slotMonos = GetComponentsInChildren<FarmSlotMono>();

        System.Array.Sort(slotMonos, (a, b) =>
        {
            Vector3 aPos = a.transform.localPosition;
            Vector3 bPos = b.transform.localPosition;
            int yCompare = bPos.y.CompareTo(aPos.y);
            return yCompare != 0 ? yCompare : aPos.x.CompareTo(bPos.x);
        });

        for (int i = 0; i < slotMonos.Length; i++)
        {
            slotMonos[i].transform.SetSiblingIndex(i);
        }

        slotMonoList.Clear();

        int clearedCount = 0;

        for (int i = 0; i < slotMonos.Length; i++)
        {
            FarmSlotMono slotMono = slotMonos[i];
            FarmSlot farmSlot = slotMono.FarmSlot;

            if (farmSlot == null)
            {
                Debug.LogWarning($"FarmSlotMono '{slotMono.name}' has no FarmSlot data.", slotMono);
                continue;
            }

            farmSlot.SetID($"slot_{i}");
            farmSlot.slotGameObject = slotMono.gameObject;

            PlantMono plantMono = slotMono.GetComponentInChildren<PlantMono>();
            farmSlot.plantGameObject = plantMono != null ? plantMono.gameObject : null;

            slotMono.CachePlantMono();

            // Clear out plants with a blank/missing ID — treated as invalid/orphaned data
            if (farmSlot.Plant != null && string.IsNullOrEmpty(farmSlot.Plant.PlantID))
            {
                farmSlot.RemovePlant(farmSlot.Plant);

                if (farmSlot.plantGameObject != null)
                    farmSlot.plantGameObject.SetActive(false);

                clearedCount++;
            }

            farm.FarmSlots.Add(farmSlot);
            slotMonoList.Add(slotMono);
        }

        Debug.Log(
            $"Farm '{name}' tracked and sorted successfully. " +
            $"Slots: {farm.FarmSlots.Count}. Cleared blank-ID plants: {clearedCount}",
            this
        );
    }

    [ContextMenu("PlantRandom")]
    public void PlantRandom()
    {
        List<FarmSlotMono> emptySlots = slotMonoList.FindAll(s => s.FarmSlot.Plant == null);

        if (emptySlots.Count == 0)
        {
            Debug.LogWarning("No empty farm slots available.");
            return;
        }

        FarmSlotMono target = emptySlots[Random.Range(0, emptySlots.Count)];
        Plant plant = GameManager.Instance.plantSO.plant.Clone();

        target.AddPlant(plant);
    }

    [ContextMenu("ProvidingToRandom")]
    public void ProvidingToRandom()
    {
        List<FarmSlotMono> growable = slotMonoList.FindAll(
            s => s.FarmSlot.Plant != null && !s.FarmSlot.Plant.IsFullyGrown);

        if (growable.Count == 0)
        {
            Debug.LogWarning("No growable plants available.");
            return;
        }

        FarmSlotMono target = growable[Random.Range(0, growable.Count)];

        // Placeholder: feeds the exact requirement so growth always succeeds.
        // Swap this for real player-provided resources once that system exists.
        var available = new Dictionary<Resource, int>(target.FarmSlot.Plant.NextStageRequirement);

        bool grew = target.TryGrow(available);
        Debug.Log(grew
            ? $"Slot '{target.FarmSlot.SlotID}' grew."
            : $"Slot '{target.FarmSlot.SlotID}' could not grow.");
    }

    [ContextMenu("HarvestAll")]
    public void HarvestAll()
    {
        int harvestedCount = 0;

        foreach (FarmSlotMono slotMono in slotMonoList)
        {
            if (slotMono.FarmSlot.Plant == null || !slotMono.FarmSlot.Plant.IsFullyGrown)
                continue;

            Dictionary<Crop, int> yieldResult = slotMono.Harvest();
            if (yieldResult == null) continue;

            foreach (var kvp in yieldResult)
            {
                // TODO: push kvp.Key (Crop, star-tiered) x kvp.Value into inventory
                Debug.Log($"Harvested {kvp.Value}x crop from slot '{slotMono.FarmSlot.SlotID}'");
            }

            harvestedCount++;
        }

        Debug.Log($"Harvested {harvestedCount} plant(s).");
    }

    // TODO : remove
    public void ProvideResource(Item item, ItemUsesType usesType, int value) { }
}