using LgTyLib.Core;
using HarvestGrid.Farming;
using HarvestGrid.Farming.Data;
using UnityEngine;

namespace HarvestGrid.System
{
    public class PlantSystem : BaseSingleton<PlantSystem>
    {
        public void AddResourceToPlant(Plant target, Resource resource, int amount)
        {
            if (target != null && resource != null)
            {
                target.AddResource(resource, amount);
            }
        }

        public void HarvestPlant(Plant target)
        {
            if (target != null)
            {
                // Simple logic for harvesting: verify if the plant is in a final state 
                // or a state with no next state.
                if (target.currentState != null && target.currentState.nextState == null)
                {
                    Debug.Log($"Successfully harvested {target.gameObject.name}!");
                    
                    // TODO: Implement reward spawning logic here
                    
                    // Reset plant or destroy it based on game design
                    // Destroy(target.gameObject);
                }
                else
                {
                    Debug.Log($"Tried to harvest {target.gameObject.name}, but it is not ready yet.");
                }
            }
        }
    }
}
