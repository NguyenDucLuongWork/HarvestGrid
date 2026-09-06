using System;

namespace HarvestGrid.Farming.Data
{
    [Serializable]
    public struct RequiredResource
    {
        public Resource resource;
        public int amount;
    }
}
