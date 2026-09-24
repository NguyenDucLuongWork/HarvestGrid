using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlantRequiringPanel : MonoBehaviour
{
    [SerializeField] private GameObject growth;
    [SerializeField] private GameObject water;
    [SerializeField] private GameObject light;
    [SerializeField] private GameObject nutrients;

    [SerializeField] private RectTransform originalHolder;

    private void Awake()
    {
        gameObject.transform.parent = GameplayScene.Instance.plantRequieContainer;
    }

    private void Start()
    {
        gameObject.SetActive(false);
    }
    public void SetData(IReadOnlyDictionary<Resource, int> requirements)
    {
        if(requirements == null)
        {
            gameObject.SetActive(false);
            
            return;
        }
        if(requirements.Count == 0 && gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
        else
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }
        SetActive(growth, requirements, Resource.Growth);
        SetActive(water, requirements, Resource.Water);
        SetActive(light, requirements, Resource.Light);
        SetActive(nutrients, requirements, Resource.Nutrients);

        SortIcons();
    }

    private void SetActive(
        GameObject icon,
        IReadOnlyDictionary<Resource, int> requirements,
        Resource resource)
    {
        bool active =
            requirements != null &&
            requirements.TryGetValue(resource, out int amount) &&
            amount > 0;

        icon.SetActive(active);
    }

    private void SortIcons()
    {
        GameObject[] icons =
        {
            growth,
            water,
            light,
            nutrients
        };

        int index = 0;

        // Active icons first
        foreach (GameObject icon in icons)
        {
            if (icon.activeSelf)
                icon.transform.SetSiblingIndex(index++);
        }

        // Inactive icons after
        foreach (GameObject icon in icons)
        {
            if (!icon.activeSelf)
                icon.transform.SetSiblingIndex(index++);
        }
    }
    public void Clear()
    {
        growth.SetActive(false);
        water.SetActive(false);
        light.SetActive(false);
        nutrients.SetActive(false);

        SortIcons();
        this.gameObject.SetActive(false);
    }
}