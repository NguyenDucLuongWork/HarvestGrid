using UIGen;
using UnityEngine;
using UnityEngine.UIElements;

public class UIHomePanel : BindedPanel
{
    [SerializeField]
    public HomeViewSO so;

    protected override object DataSource => so != null ? so.homeView : null;

    [UIAction("play")]
    public void OnPlayClicked()
    {
        Debug.Log("[UIHomePanel] PLAY button clicked! Starting farming session...");

        if (LevelsManager.HasInstance)
        {
            LevelsManager.Instance.LoadNextLevel();
        }
    }

    [UIAction("save")]
    public void OnSaveClicked()
    {
        Debug.Log("[UIHomePanel] Quick Save clicked!");
    }

    [UIAction("loadg")]
    public void OnLoadClicked()
    {
        Debug.Log("[UIHomePanel] Quick Load clicked!");
    }
}
