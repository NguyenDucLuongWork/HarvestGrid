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

    public bool PlantToRandomSlot(Plant plant)
    {
        FarmSlot farmSlot = farm.GetEmptySlot();

        if (farmSlot == null)
        {
            Debug.LogWarning("No empty farm slots available.");
            return false;
        }

        farmSlot.AddPlant(plant);
        return true;
    }

    [ContextMenu("ProvidingToRandom")]
    public void ProvidingToRandom()
    {
        List<Plant> growable = farm.GetGrowablePlants();

        if (growable.Count == 0)
        {
            Debug.LogWarning("No growable plants available.");
            return;
        }

        for (int i = 0; i < 4; i++)
        {
            // Get random plant
            Plant plantToGrow = growable[UnityEngine.Random.Range(0, growable.Count)];

            // Plant may have finished growing (or hit its last stage) during a
            // previous iteration of this loop — skip it instead of crashing.
            if (plantToGrow.IsFullyGrown || plantToGrow.NextStageRequirement == null)
            {
                continue;
            }

            // Get resources this plant needs
            List<Resource> neededResources = new List<Resource>();

            foreach (var requirement in plantToGrow.NextStageRequirement)
            {
                if (requirement.Value > 0)
                {
                    neededResources.Add(requirement.Key);
                }
            }

            if (neededResources.Count == 0)
                continue;

            // Get random required resource
            Resource resource = neededResources[
                UnityEngine.Random.Range(0, neededResources.Count)
            ];

            // Provide 6
            int absorbed = plantToGrow.Absorb(resource, 6);

            Debug.Log(
                $"Provided {absorbed} {resource} to {plantToGrow.PlantName}"
            );
        }
    }

    public void ProvidingResourceToRandom(Resource resource, int amount)
    {
        List<Plant> growable = farm.GetGrowablePlants();

        if (growable.Count == 0)
        {
            //Debug.LogWarning("No growable plants available.");
            return;
        }

        for (int i = 0; i < 4; i++)
        {
            // Get random plant
            Plant plantToGrow = growable[UnityEngine.Random.Range(0, growable.Count)];

            // Plant may have finished growing (or hit its last stage) during a
            // previous iteration of this loop — skip it instead of crashing.
            if (plantToGrow.IsFullyGrown || plantToGrow.NextStageRequirement == null)
            {
                continue;
            }


            // Provide 6
            int absorbed = plantToGrow.Absorb(resource, amount);

            Debug.Log(
                $"Provided {absorbed} {resource} to {plantToGrow.PlantName}"
            );
        }
    }

    [ContextMenu("HarvestRandom")]
    public void HarvestRandom(float effective = 1f)
    {
        List<Plant> harvestablePlants = farm.GetHarvestablePlants();

        if (harvestablePlants == null || harvestablePlants.Count == 0)
        {
            Debug.LogWarning("No harvestable plants available.");
            return;
        }

        int randomIndex = UnityEngine.Random.Range(0, harvestablePlants.Count);
        Plant plant = harvestablePlants[randomIndex];

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
}