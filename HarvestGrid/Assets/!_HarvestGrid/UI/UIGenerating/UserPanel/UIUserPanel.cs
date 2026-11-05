using UIGen;
using UnityEngine;
using UnityEngine.UIElements;

public class UIUserPanel : BindedPanel
{
    [SerializeField]
    public UserViewSO so;

    protected override object DataSource => so != null ? so.userView : null;

    [UIAction("save")]
    public void OnSaveClicked()
    {
        if (so != null && so.userView != null)
        {
            Debug.Log($"[UIUserPanel] Saved farmer profile! Name: '{so.userView.username}'");
            Refresh();
        }
    }

    [UIAction("logout")]
    public void OnLogoutClicked()
    {
        Debug.Log("[UIUserPanel] Logout clicked. Redirecting to login or closing session...");
    }

    protected override void OnBound()
    {
        base.OnBound();

        // Optional synchronization with UserManager if available in runtime
        if (UserManager.Instance != null && UserManager.Instance.userData != null && so != null)
        {
            so.SetData(UserManager.Instance.userData);
            Refresh();
        }
    }
}
