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
        // --- Game-specific fields ---
        // Add your fields here, e.g:
        // public int level;
        // public bool bossDefeated;
        public float score;
        public List<Item> items;
        
        // Cosmetic Data
        public List<string> unlockedCosmetics = new List<string>();
        public List<EquippedCosmeticData> equippedCosmetics = new List<EquippedCosmeticData>();
    }
}