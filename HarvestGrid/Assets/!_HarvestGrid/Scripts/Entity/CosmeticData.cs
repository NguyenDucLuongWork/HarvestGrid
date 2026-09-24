using UnityEngine;

[CreateAssetMenu(fileName = "NewCosmeticData", menuName = "HarvestGrid/Cosmetic Data")]
public class CosmeticData : ScriptableObject
{
    [Tooltip("Unique ID for saving/loading. Keep this stable!")]
    public string id;
    public string cosmeticName;
    [TextArea(2, 4)]
    public string description;
    public CosmeticsType cosmeticsType;
    public Sprite icon;
    
    [Header("Visual Replacements")]
    [Tooltip("If this cosmetic replaces a 2D Sprite, set it here")]
    public Sprite previewSprite;
    
    [Tooltip("If this cosmetic is a 3D Object or complex Prefab, set it here")]
    public GameObject prefab;
}
