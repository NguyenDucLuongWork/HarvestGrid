using LgTyLib.Modules.AutoBindedPanel;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class FarmPanel : BindedPanel
{
    // ── Inspector ───────────────────────────────────────────────────────────
    [SerializeField] public FarmViewSO farmViewSO;

    protected override object DataSource => throw new System.NotImplementedException();

    [ContextMenu("Sync")]
    public void Sync()
    {
        farmViewSO.Sync();
    }
}
