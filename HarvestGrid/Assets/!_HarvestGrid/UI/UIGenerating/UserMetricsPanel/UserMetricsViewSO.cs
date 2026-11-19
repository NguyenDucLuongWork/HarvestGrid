using LgTyLib.Modules.DataPersistence;
using System;
using UIGen;
using UnityEngine;

[CreateAssetMenu(fileName = "UserMetricsViewSO", 
    menuName = "Scriptable Objects/View/UserMetricsViewSO")]
public class UserMetricsViewSO : ScriptableObject
{
    [SerializeField]
    public UserMetrics userMetrics = new UserMetrics();

    public void SetData(UserData userData)
    {
        if (userData == null) return;
        if (!string.IsNullOrEmpty(userData.displayName))
            userMetrics.name = userData.displayName;
        else if (!string.IsNullOrEmpty(userData.username))
            userMetrics.name = userData.username;

        userMetrics.coin = userData.coin;
    }
}

[Serializable]
public class UserMetrics
{
    [UIField(ControlType.Label, Label = "Player Name")]
    public string name = "Farmer Bo";

    [UIField(ControlType.Label, Label = "Coins")]
    public int coin = 1250;
}
