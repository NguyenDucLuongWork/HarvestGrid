using UnityEngine;
using UnityEngine.UI;

public class PlantMono : MonoBehaviour
{
    private Image image;
    private void OnEnable()
    {
        image = GetComponent<Image>();
    }

    public void UpdateSprite(Sprite sprite)
    {
        this.image.sprite = sprite;
        this.image.SetNativeSize();
    }
}
