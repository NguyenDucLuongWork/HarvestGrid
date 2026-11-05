// UserData.Game.cs  ← YOUR file, edit freely
using System.Collections.Generic;
using UnityEngine;

namespace LgTyLib.Modules.DataPersistence
{
    // ╔══════════════════════════════════════════════════════╗
    // ║  GAME FILE — EDIT THIS ONE                          ║
    // ║  Add / remove global (non-playthrough) fields here  ║
    // ╚══════════════════════════════════════════════════════╝
    public partial class UserData
    {
        public string username;
        public string displayName;
        public int coin;
        public List<int> slotsLevels;
        public List<PlayTime> finishedLevels;



        // Progress that should persist even if a playthrough is deleted
        public List<string> globalUnlockedAchievements = new();
        public List<string> globalUnlockedCosmetics = new();
    }
}