using UnityEngine;

namespace HarvestGrid.Farming.Data
{
    [CreateAssetMenu(fileName = "NewResource", menuName = "HarvestGrid/Farming/Resource")]
    public class Resource : ScriptableObject
    {
        public string id;
        public string resourceName;
        public Sprite sprite;
    }
}
