using LgTyLib.Core;
using HarvestGrid.Farming;
using HarvestGrid.Item.Data;
using UnityEngine;

namespace HarvestGrid.System
{
    public class ItemSystem : BaseSingleton<ItemSystem>
    {
        public void ProcessEffect(ItemEffectData effectData)
        {
            if (effectData.target == null)
            {
                Debug.LogWarning("ItemSystem received effect with null target.");
                return;
            }

            switch (effectData.effectType)
            {
                case ItemEffectType.AddResource:
                    PlantSystem.Instance.AddResourceToPlant(effectData.target, effectData.resource, effectData.amount);
                    break;

                case ItemEffectType.Harvest:
                    PlantSystem.Instance.HarvestPlant(effectData.target);
                    break;
            }
        }
    }
}
