// GameData.Game.cs  ← YOUR file, edit freely
using GameJson;
using System;
using System.Collections.Generic;

namespace LgTyLib.Modules.DataPersistence
{
    [Serializable]
    public struct EquippedCosmeticData
    {
        public CosmeticsType type;
        public string cosmeticId;
    }

    // ╔══════════════════════════════════════════════════════╗
    // ║  GAME FILE — EDIT THIS ONE                          ║
    // ║  Add / remove fields here for each new project      ║
    // ╚══════════════════════════════════════════════════════╝
    public partial class GameData
    {
        // --- Game-specific fields (marked with [ContextData] for clean AI JSON) ---
        [ContextData]
        public float score;

        [ContextData]
        public List<Item> items;
        
        // Cosmetic Data (Visual only, omitted from AI context unless marked)
        public List<string> unlockedCosmetics = new List<string>();
        public List<EquippedCosmeticData> equippedCosmetics = new List<EquippedCosmeticData>();

        [ContextData]
        public int levelID;

        [ContextData]
        public int levelNumber;

        [ContextData]
        public Farm farm;

        [ContextData]
        public Inventory inventory;

        [ContextData]
        public List<ItemEffect> itemEffects;
    }
}
