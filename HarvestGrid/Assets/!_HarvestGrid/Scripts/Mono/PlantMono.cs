using UnityEngine;
using UnityEngine.UI;

public class PlantMono : MonoBehaviour
{
    public FarmSlotMono farmSlotMono;
    private Image image;
    private void OnEnable()
    {
        image = GetComponent<Image>();
        farmSlotMono = this.GetComponentInParent<FarmSlotMono>();
    }

    public void UpdateSprite(Sprite sprite)
    {
        this.image.sprite = sprite;
        this.image.SetNativeSize();
    }

    public void Harvest(Plant plant)
    {
        farmSlotMono.FarmSlot.RemovePlant(plant);
    }
}
