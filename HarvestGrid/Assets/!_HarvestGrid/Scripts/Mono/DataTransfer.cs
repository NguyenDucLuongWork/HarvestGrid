using LgTyLib.Core;
using System;
using System.Collections.Generic;
using System.Text;

public class DataTransfer : BaseSingleton<DataTransfer>
{
    public bool toLoad;

    private void OnEnable()
    {
        DontDestroyOnLoad(this.gameObject);
    }
}