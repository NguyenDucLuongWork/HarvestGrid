using System.Collections.Generic;
using UnityEngine;

public class FarmSlotMono : MonoBehaviour
{
    [SerializeField]
    private FarmSlot farmSlot;
    public FarmSlot FarmSlot => farmSlot;

    private PlantMono plantMono;

    [SerializeField]
    private bool harvestActive;
    public bool HarvestActive => harvestActive;


    public void CachePlantMono()
    {
        plantMono = farmSlot.plantGameObject != null
            ? farmSlot.plantGameObject.GetComponent<PlantMono>()
            : null;
    }

    public void SetFarmSlot(FarmSlot farmSlot)
    {
        this.farmSlot = farmSlot;
    }
}