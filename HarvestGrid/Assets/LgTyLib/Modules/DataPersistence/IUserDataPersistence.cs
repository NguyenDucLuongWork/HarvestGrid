using System;
using System.Collections.Generic;
using System.Text;

namespace LgTyLib.Modules.DataPersistence
{
    /// <summary>
    /// Implement this on any MonoBehaviour that needs to read/write global
    /// user-level data (settings, profile, cross-save unlocks, etc.) as
    /// opposed to per-playthrough gameplay data (see IDataPersistence).
    /// </summary>
    public interface IUserDataPersistence
    {
        void LoadUserData(UserData userData);
        void SaveUserData(ref UserData userData);
    }
}