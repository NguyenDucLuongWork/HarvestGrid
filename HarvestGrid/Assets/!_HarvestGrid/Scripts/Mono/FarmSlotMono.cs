using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FarmSlotMono : MonoBehaviour
{
    [SerializeField]
    private Sprite[] spriteByLevel;

    [SerializeField]
    private FarmSlot farmSlot;
    public FarmSlot FarmSlot => farmSlot;

    private PlantMono plantMono;


    public void CachePlantMono()
    {
        plantMono = farmSlot.plantGameObject != null
            ? farmSlot.plantGameObject.GetComponent<PlantMono>()
            : null;
    }

    public void SetFarmSlot(FarmSlot farmSlot)
    {
        this.farmSlot = farmSlot;
        GetComponent<Image>().sprite = spriteByLevel[farmSlot.level];
    }
    
    public void Upgrade()
    {
        if(farmSlot.level == 1)
        {
            return;
        }
        FarmMono.Instance.UpgradeSlot(farmSlot);
    }
}