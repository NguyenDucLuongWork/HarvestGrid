// GameData.Game.cs  ← YOUR file, edit freely
using NUnit.Framework;
using System.Collections.Generic;

namespace LgTyLib.Modules.DataPersistence
{
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
        public string level;
        public Farm farm;
        public Inventory inventory;
    }
}