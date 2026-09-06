using UnityEngine;
using System.Collections.Generic;
using HarvestGrid.Farming.Data;

namespace HarvestGrid.Farming
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class Plant : MonoBehaviour
    {
        public PlantState currentState;
        private Dictionary<Resource, int> currentResourceProgress = new Dictionary<Resource, int>();
        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (currentState != null)
            {
                ChangeState(currentState);
            }
        }

        public void AddResource(Resource resource, int amount)
        {
            if (currentState == null) return;

            // Find if the resource is required for the current state
            int requiredIndex = currentState.requiredResources.FindIndex(r => r.resource == resource);
            if (requiredIndex == -1) return; // Resource not needed

            RequiredResource req = currentState.requiredResources[requiredIndex];

            if (!currentResourceProgress.ContainsKey(resource))
            {
                currentResourceProgress[resource] = 0;
            }

            // Add resource but don't exceed the required amount
            currentResourceProgress[resource] = Mathf.Min(currentResourceProgress[resource] + amount, req.amount);

            CheckStateTransition();
        }

        private void CheckStateTransition()
        {
            if (currentState == null) return;

            // Check if all requirements are met
            foreach (var req in currentState.requiredResources)
            {
                if (!currentResourceProgress.ContainsKey(req.resource) || currentResourceProgress[req.resource] < req.amount)
                {
                    return; // Requirement not met yet
                }
            }

            // All requirements met, transition to the next state if one exists
            if (currentState.nextState != null)
            {
                ChangeState(currentState.nextState);
            }
        }

        private void ChangeState(PlantState newState)
        {
            currentState = newState;
            currentResourceProgress.Clear();
            
            if (spriteRenderer != null && currentState.sprite != null)
            {
                spriteRenderer.sprite = currentState.sprite;
            }
        }

        // Just an accessor if needed by external systems
        public PlantState GetCurrentState()
        {
            return currentState;
        }
    }
}
