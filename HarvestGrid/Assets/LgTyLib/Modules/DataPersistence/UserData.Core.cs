// UserData.Core.cs  ← stays in the library, never edit this
using System;

namespace LgTyLib.Modules.DataPersistence
{
    // ╔══════════════════════════════════════════════════════╗
    // ║  LIBRARY FILE — DO NOT EDIT                         ║
    // ║  Add your game fields in UserData.Game.cs instead   ║
    // ╚══════════════════════════════════════════════════════╝
    [Serializable]
    public partial class UserData
    {
        // --- Metadata (library-owned) ---
        public string userId;       // stable id for this install/account
        public int schemaVersion;   // bump when you change field layout, for migrations

        public UserData()
        {
            userId = Guid.NewGuid().ToString();
            schemaVersion = 1;
        }
    }
}