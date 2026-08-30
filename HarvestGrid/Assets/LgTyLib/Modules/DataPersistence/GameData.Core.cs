// GameData.Core.cs  ← stays in the library, never edit this
using System;

namespace LgTyLib.Modules.DataPersistence
{
    // ╔══════════════════════════════════════════════════════╗
    // ║  LIBRARY FILE — DO NOT EDIT                         ║
    // ║  Add your game fields in GameData.Game.cs instead   ║
    // ╚══════════════════════════════════════════════════════╝
    [Serializable]
    public partial class GameData
    {
        // --- Metadata (library-owned) ---
        public string lastSavedAt;  // ISO 8601 timestamp
        public float totalPlayTime; // in seconds

        public GameData()
        {
            lastSavedAt = DateTime.Now.ToString("o");
            totalPlayTime = 0f;
        }
    }
}