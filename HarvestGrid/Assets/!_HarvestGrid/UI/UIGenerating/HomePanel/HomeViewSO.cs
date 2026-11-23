// HomeViewSO now inherits from ViewSO<HomeView> and supports UIField bindings
using LgTyLib.Modules.AutoBindedPanel;
using LgTyLib.Modules.DataPersistence;
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "HomeViewSO", menuName = "Scriptable Objects/View/HomeViewSO")]
public class HomeViewSO : ViewSO<HomeView>
{
    [UIField.Sync(nameof(userGoldLink))]
    public string userGold;
    string userGoldLink => Data != null ? Data.userData.coin + "" : "0";
    // Bindable hint text
    [UIField.Sync(nameof(hintTextLink))]
    public string hintText;
    string hintTextLink => Data != null ? Data.hintText : "";

    // Bindable play button label
    [UIField.Sync(nameof(playButtonTextLink))]
    public string playButtonText;
    string playButtonTextLink => Data != null ? Data.playButtonText : "";
}

[Serializable]
public class HomeView
{
    public string hintText = "Trang trại đang chờ bạn 🌱";
    public string playButtonText = "PLAY";
    public UserData userData;
}
