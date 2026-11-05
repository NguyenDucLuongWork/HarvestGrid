using System;
using UIGen;
using UnityEngine;

[CreateAssetMenu(fileName = "HomeViewSO", menuName = "Scriptable Objects/View/HomeViewSO")]
public class HomeViewSO : ScriptableObject
{
    [SerializeField]
    public HomeView homeView = new HomeView();
}

[Serializable]
public class HomeView
{
    [UIField(ControlType.Label, Label = "Hint Text")]
    public string hintText = "Trang trại đang chờ bạn 🌱";

    [UIField(ControlType.Label, Label = "Play Button Text")]
    public string playButtonText = "PLAY";
}
