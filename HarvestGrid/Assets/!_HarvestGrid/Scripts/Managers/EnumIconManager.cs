using LgTyLib.Core;
using System.Collections.Generic;
using UnityEngine;

public class EnumIconManager : BaseSingleton<EnumIconManager>
{
    [SerializeField]
    public Dictionary<CosmeticsType, Sprite> cosmeticsTypeIconDict;
    [SerializeField]
    public Dictionary<CurrencyType, Sprite> currencyTypeIconDict;
    [SerializeField]
    public Dictionary<ItemRarity, Sprite> itemRarityIconDict;
    [SerializeField]
    public Dictionary<Resource, Sprite> resourceIconDict; 
}
