using UIGen;
using UnityEngine;

public class UIMetricsPanel : BindedPanel
{
    [SerializeField]
    public UserMetricsViewSO so;

    protected override object DataSource => so != null ? so.userMetrics : null;

    protected override void OnBound()
    {
        base.OnBound();
        if (UserManager.Instance != null && UserManager.Instance.userData != null && so != null)
        {
            so.SetData(UserManager.Instance.userData);
            Refresh();
        }
    }
}
