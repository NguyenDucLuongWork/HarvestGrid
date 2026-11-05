using System;
using UIGen;
using UnityEngine;
using LgTyLib.Modules.DataPersistence;

[CreateAssetMenu(fileName = "UserViewSO", menuName = "Scriptable Objects/View/UserViewSO")]
public class UserViewSO : ScriptableObject
{
    [SerializeField]
    public UserView userView = new UserView();

    public void SetData(UserData userData)
    {
        if (userData == null) return;
        if (!string.IsNullOrEmpty(userData.username))
            userView.username = userData.username;
        if (!string.IsNullOrEmpty(userData.displayName))
            userView.username = userData.displayName;

        userView.coin = userData.coin;

        if (userData.finishedLevels != null)
        {
            int count = 0;
            foreach (var finish in userData.finishedLevels)
            {
                if (finish != null) count++;
            }
            userView.completedLevels = count;
            userView.level = count + 1;
        }
    }

    public void SaveData(ref UserData userData)
    {
        if (userData == null) return;
        userData.displayName = userView.username;
        userData.coin = userView.coin;
    }
}

[Serializable]
public class UserView
{
    [UIField(ControlType.TextField, Label = "Farmer Name")]
    public string username = "Farmer Bo";

    [UIField(ControlType.Label, Label = "Account ID")]
    public string userId = "HG-7492";

    [UIField(ControlType.Label, Label = "Current Level")]
    public int level = 3;

    [UIField(ControlType.Label, Label = "Coins")]
    public int coin = 1250;

    [UIField(ControlType.Label, Label = "Completed Levels")]
    public int completedLevels = 2;
}
