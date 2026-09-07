using UnityEngine;
using HarvestGrid.Farming;
using HarvestGrid.Item.Strategy;

namespace HarvestGrid.Item
{
    public class ItemUsage : MonoBehaviour
    {
        private IItemUsageStrategy strategy;

        public void SetStrategy(IItemUsageStrategy newStrategy)
        {
            strategy = newStrategy;
        }

        public void Use(Plant target)
        {
            if (strategy != null)
            {
                strategy.Execute(this, target);
            }
            else
            {
                Debug.LogWarning($"ItemUsage on {gameObject.name} has no strategy assigned!");
            }
        }
    }
}
