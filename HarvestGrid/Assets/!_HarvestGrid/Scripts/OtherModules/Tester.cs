using GameJson;
using LgTyLib.Modules.DataPersistence;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Tester : MonoBehaviour, IDataPersistence
{
    [TextArea(5, 20)]
    public string data;

    public PlantPrototype so;

    [ContextMenu("DebugJson (For AI)")]
    public void DebugJsonForAI()
    {
        if (so != null && so.plant != null)
        {
            data = CleanJson.SimplifyObject(so.plant, CleanJson.Options.ForAI());
            Debug.Log(data);
        }
    }

    public void LoadGame(GameData gameData)
    {
        //throw new NotImplementedException();
    }

    public void SaveGame(ref GameData gameData)
    {
        data = CleanJson.SimplifyObject(gameData, CleanJson.Options.ForAI());
        Debug.Log(data);
    }
}
