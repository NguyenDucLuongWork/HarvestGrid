using UnityEngine;
using UnityEngine.UI;

public class PlantMono : MonoBehaviour
{
    public FarmSlotMono farmSlotMono;

    private Image image;

    [SerializeField]
    private PlantRequiringPanel plantRequiringPanel;

    private void OnEnable()
    {
        image = GetComponent<Image>();
        farmSlotMono = GetComponentInParent<FarmSlotMono>();
    }

    public void UpdateSprite(Sprite sprite)
    {
        if (sprite == null)
        {
            image.sprite = null;
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        image.sprite = sprite;
        image.SetNativeSize();
    }

    public void UpdateRequiringPanel(Plant plant)
    {
        if (plantRequiringPanel == null)
            return;

        plantRequiringPanel.SetData(plant.NextStageRequirement);
    }

    public void UpdatePlantVisual(Plant plant)
    {
        if (plant == null)
        {
            UpdateSprite(null);
            plantRequiringPanel?.Clear();
            return;
        }

        if (plant.CurrentState != null)
            UpdateSprite(plant.CurrentState.Sprite);
        else
            UpdateSprite(null);

        plantRequiringPanel?.SetData(plant.NextStageRequirement);
    }

    public void Harvest(Plant plant)
    {
        farmSlotMono.FarmSlot.RemovePlant(plant);
        plantRequiringPanel.gameObject.SetActive(false);
    }
}