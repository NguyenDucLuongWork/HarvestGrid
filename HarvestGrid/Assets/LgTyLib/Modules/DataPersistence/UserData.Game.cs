// UserData.Game.cs  ← YOUR file, edit freely
using System.Collections.Generic;

namespace LgTyLib.Modules.DataPersistence
{
    // ╔══════════════════════════════════════════════════════╗
    // ║  GAME FILE — EDIT THIS ONE                          ║
    // ║  Add / remove global (non-playthrough) fields here  ║
    // ╚══════════════════════════════════════════════════════╝
    public partial class UserData
    {
        public string displayName;

        // Settings that should apply across every playthrough
        public float musicVolume = 1f;
        public float sfxVolume = 1f;
        public bool hasSeenTutorial;

        // Progress that should persist even if a playthrough is deleted
        public List<string> globalUnlockedAchievements = new();
        public List<string> globalUnlockedCosmetics = new();
    }
}