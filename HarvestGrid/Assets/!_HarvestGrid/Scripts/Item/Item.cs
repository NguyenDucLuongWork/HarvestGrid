using System.Collections;
using UnityEngine;
using HarvestGrid.Item.Data;

namespace HarvestGrid.Item
{
    public class Item : MonoBehaviour
    {
        public ItemData data;
        public float cooldown = 5f;
        public bool autoUse = true;

        private void Start()
        {
            if (autoUse)
            {
                StartCoroutine(AutoUseRoutine());
            }
        }

        private IEnumerator AutoUseRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(cooldown);
                
                if (InventoryBase.Instance != null)
                {
                    // Automatically trigger UseItem from Inventory when cooldown finishes
                    InventoryBase.Instance.UseItem(this);
                }
            }
        }
    }
}
