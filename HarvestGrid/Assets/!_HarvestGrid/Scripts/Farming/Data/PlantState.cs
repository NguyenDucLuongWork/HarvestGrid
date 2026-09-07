using UnityEngine;
using System.Collections.Generic;

namespace HarvestGrid.Farming.Data
{
    [CreateAssetMenu(fileName = "NewPlantState", menuName = "HarvestGrid/Farming/PlantState")]
    public class PlantState : ScriptableObject
    {
        public string stateId;
        public string stateName;
        public Sprite sprite;
        
        [Tooltip("The state this plant transitions to when all requirements are met. Leave null if final state.")]
        public PlantState nextState;
        
        public List<RequiredResource> requiredResources = new List<RequiredResource>();
    }
}
