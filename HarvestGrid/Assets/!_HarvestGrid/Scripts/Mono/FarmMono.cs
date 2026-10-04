using LgTyLib.Core;
using LgTyLib.Modules.DataPersistence;
using System.Collections.Generic;
using UnityEngine;

public class FarmMono : BaseSingleton<FarmMono>, IDataPersistence
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
        TrackDataAndSort();
        farm.SetFarmSlotForFarmSlotMono();
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

    public bool AddPlant(Plant plant, FarmSlot farmSlot)
    {
        if (farmSlot == null)
        {
            Debug.LogWarning("No empty farm slots available.");
            return false;
        }

        farmSlot.AddPlant(plant);
        return true;
    }


    public void ProvidingResource(Resource resource, int amount, FarmSlot farmSlot)
    {

        // Get random plant
        Plant plantToGrow = farmSlot.Plant;

        int absorbed = plantToGrow.Absorb(resource, amount);

        Debug.LogWarning("Remain resource:" + (amount - amount));
    }

    public void HarvestRandom(FarmSlot farmSlot, float effective = 1f)
    {
        Plant plant = farmSlot.Plant;

        Dictionary<Crop, int> result = plant.Harvest(effective);
        foreach (var harvest in result) {
            InventoryMono.Instance.Inventory.AddCrop(harvest.Key, harvest.Value);
        }
    }

    public void LoadGame(GameData gameData)
    {
        farm.CopyData(gameData.farm);
    }

    public void SaveGame(ref GameData gameData)
    {
        gameData.farm = farm.Clone();
    }

    public void UpdateFarmFullyFromData()
    {
        farm.ValidateData();
        farm.SetFarmSlotForFarmSlotMono();
        var plants = farm.GetPlants();
        foreach (var plant in plants) {
            if (plant == null || plant.PlantID == "")
            {
                plant.gameObject.SetActive(false);
                continue;
            } 
            plant.gameObject.GetComponent<PlantMono>().UpdateSprite(plant.CurrentState.Sprite);
            plant.gameObject.SetActive(true);
        }
    }

    public FarmSlot GetTarget(ItemUse use)
    {
        if (use == null)
            return null;

        switch (use)
        {
            case ProviceResourceUse resourceUse:
                return GetRandomResourceTargetSlot(resourceUse.Resource);

            default:
                switch (use.UseType)
                {
                    case ItemUseType.AddPlant:
                        return GetRandomEmptySlot();

                    case ItemUseType.Harvest:
                        return GetRandomHarvestableSlot();

                    default:
                        return null;
                }
        }
    }


    public FarmSlot GetRandomEmptySlot()
    {
        if (farm == null || farm.FarmSlots == null)
            return null;

        List<FarmSlot> availableSlots = farm.FarmSlots.FindAll(
            slot => slot != null && !slot.beingEffected && slot.Plant == null
        );

        if (availableSlots.Count == 0)
            return null;

        return availableSlots[
            UnityEngine.Random.Range(0, availableSlots.Count)
        ];
    }

    public FarmSlot GetRandomHarvestableSlot()
    {
        if (farm == null || farm.FarmSlots == null)
            return null;

        List<FarmSlot> availableSlots = farm.FarmSlots.FindAll(
            slot =>
                slot != null &&
                !slot.beingEffected &&
                slot.Plant != null &&
                slot.Plant.IsFullyGrown
        );

        if (availableSlots.Count == 0)
            return null;

        return availableSlots[
            UnityEngine.Random.Range(0, availableSlots.Count)
        ];
    }

    public FarmSlot GetRandomResourceTargetSlot(Resource resource)
    {
        if (farm == null || farm.FarmSlots == null)
            return null;

        List<FarmSlot> availableSlots = farm.FarmSlots.FindAll(
            slot =>
                slot != null &&
                !slot.beingEffected &&
                slot.Plant != null &&
                !slot.Plant.IsFullyGrown &&
                slot.Plant.CheckIfNeed(resource)
        );

        if (availableSlots.Count == 0)
            return null;

        return availableSlots[UnityEngine.Random.Range(0, availableSlots.Count)];
    }

    public FarmSlot GetFarmSlotByID(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        if (farm == null || farm.FarmSlots == null)
            return null;

        return farm.FarmSlots.Find(slot =>
            slot != null &&
            slot.SlotID == id
        );
    }
}