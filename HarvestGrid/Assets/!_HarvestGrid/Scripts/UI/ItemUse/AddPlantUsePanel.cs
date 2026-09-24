using UnityEngine;
using UnityEngine.UI;

public class AddPlantUsePanel : ItemUsePanel<AddPlantUse>
{
    [SerializeField] private Text plantNameLabel;
    [SerializeField] private Image plantIcon;

    protected override void OnBind()
    {
        var plant = Use.PlantToAdd; // requires the PlantToAdd getter added to AddPlantUse
        if (plant == null) return;

        if (plantNameLabel != null)
            plantNameLabel.text = plant.PlantID;

        // if Plant exposes an icon sprite, wire it up here, e.g.:
        // if (plantIcon != null) plantIcon.sprite = plant.Icon;
    }
}