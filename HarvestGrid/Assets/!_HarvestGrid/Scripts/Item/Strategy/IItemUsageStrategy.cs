using HarvestGrid.Farming;

namespace HarvestGrid.Item.Strategy
{
    public interface IItemUsageStrategy
    {
        void Execute(ItemUsage context, Plant target);
    }
}
