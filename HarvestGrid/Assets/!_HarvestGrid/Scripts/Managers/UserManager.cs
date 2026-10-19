using LgTyLib.Core;
using LgTyLib.Modules.DataPersistence;
using System;
using System.Collections.Generic;
using System.Text;

public class UserManager : BaseSingleton<UserManager>, IUserDataPersistence
{
    public string UserName = "N138";
    public UserData userData;
    public void Load()
    {
        DataPersistenceManager.Instance.LoadUserData(UserName);
    }

    public void LoadUserData(UserData userData)
    {
        this.userData = userData; 
    }

    public void Save()
    {
        DataPersistenceManager.Instance.SaveUserData(UserName);
    }

    public void SaveUserData(ref UserData userData)
    {
        userData = this.userData;
    }

    public void SetSlotsLevels(List<int> levels)
    {
        userData.slotsLevels = levels;
        Save();
    }

    public void SetFinishedLevels(List<PlayTime> finishedLevels)
    {
        userData.finishedLevels = finishedLevels;
        Save();
    }
}