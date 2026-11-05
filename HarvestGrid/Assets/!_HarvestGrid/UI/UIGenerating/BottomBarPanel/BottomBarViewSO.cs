using System;
using UIGen;
using UnityEngine;

[CreateAssetMenu(fileName = "BottomBarViewSO", 
    menuName = "Scriptable Objects/View/BottomBarViewSO")]
public class BottomBarViewSO : ScriptableObject
{
    [SerializeField]
    public BottomBarView bottomBar = new BottomBarView();

    public void SetActiveTab(string tabName)
    {
        bottomBar.currentTab = tabName;
    }
}

[Serializable]
public class BottomBarView
{
    [UIField(ControlType.Label, Label = "Active Tab")]
    public string currentTab = "Home";
}
