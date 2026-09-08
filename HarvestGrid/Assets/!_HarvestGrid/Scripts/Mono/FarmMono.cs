using LgTyLib.Core;
using System.Collections.Generic;
using UnityEngine;

public class FarmMono : BaseSingleton<FarmMono>
{
    [SerializeField]
    private Farm farm;

    [ContextMenu("TrackData")]
    public void TrackData()
    {
        if (farm == null)
        {
            farm = new Farm();
        }

        farm.FarmSlots.Clear();
        farm.gameObject = gameObject;

        FarmSlotMono[] slotMonos =
            GetComponentsInChildren<FarmSlotMono>();

        for (int i = 0; i < slotMonos.Length; i++)
        {
            FarmSlotMono slotMono = slotMonos[i];
            FarmSlot farmSlot = slotMono.FarmSlot;

            if (farmSlot == null)
            {
                Debug.LogWarning(
                    $"FarmSlotMono '{slotMono.name}' has no FarmSlot data.",
                    slotMono
                );

                continue;
            }

            // Set ID
            farmSlot.SetID($"slot_{i}");

            // Link FarmSlot -> GameObject
            farmSlot.slotGameObject = slotMono.gameObject;

            // Find PlantMono
            PlantMono plantMono =
                slotMono.GetComponentInChildren<PlantMono>();

            if (plantMono != null)
            {
                farmSlot.plantGameObject = plantMono.gameObject;
            }
            else
            {
                farmSlot.plantGameObject = null;
            }

            farm.FarmSlots.Add(farmSlot);
        }

        Debug.Log(
            $"Farm '{name}' tracked successfully. " +
            $"Slots: {farm.FarmSlots.Count}",
            this
        );
    }

    // TODO : remove
    public void ProvideResource(Item item, ItemUsesType usesType, int value)
    {
        var slots = farm.FarmSlots;
        var plants = new List<Plant>();
        foreach (var slot in slots)
        {
            plants.Add(slot.Plant);
        }
    }
}