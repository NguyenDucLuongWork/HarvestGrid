using UnityEngine;
using UnityEngine.UI;

public class ItemUIComponent : MonoBehaviour
{
    public static readonly string processProperty = "_Process";

    [SerializeField]
    private Image itemIcon;

    [SerializeField]
    private GameObject processVisual; // UI canvas image

    private Material processMaterial;

    private void Awake()
    {
        if (processVisual == null)
            return;

        Image processImage = processVisual.GetComponent<Image>();

        if (processImage == null)
        {
            Debug.LogWarning($"{name}: Process visual does not have an Image component.");
            return;
        }

        // Create an instance so changing _Process doesn't affect the shared material
        processMaterial = new Material(processImage.material);
        processImage.material = processMaterial;
    }

    public void SetItem(Item item)
    {
        if (item == null)
            return;

        itemIcon.sprite = item.Icon;
    }

    public void SetProgress(float progress)
    {
        if (processMaterial == null)
            return;

        // Keep process between 0 and 1
        progress = Mathf.Clamp01(progress);

        processMaterial.SetFloat(processProperty, progress);
    }
}