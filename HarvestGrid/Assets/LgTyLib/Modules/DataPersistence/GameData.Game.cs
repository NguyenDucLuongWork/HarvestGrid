// GameData.Game.cs  ← YOUR file, edit freely
using NUnit.Framework;
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
                // Add your fields here, e.g:
        public int level;
        public bool bossDefeated;
        // --- Game-specific fields ---
        public string playerId;
        public string username;

        public float score;
        public List<Item> items;
        
        // Cosmetic Data
        public List<string> unlockedCosmetics = new List<string>();
        public List<EquippedCosmeticData> equippedCosmetics = new List<EquippedCosmeticData>();
        public string level;
        public Farm farm;
        public Inventory inventory;
    }
}