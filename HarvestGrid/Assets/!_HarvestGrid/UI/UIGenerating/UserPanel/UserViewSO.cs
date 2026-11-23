using System;
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
    public string username = "Farmer Bo";

    public string userId = "HG-7492";

    public int level = 3;

    public int coin = 1250;

    public int completedLevels = 2;
}
