using LgTyLib.Core;
using LgTyLib.Modules.DataPersistence;
using UnityEngine;


public class GameManager : BaseSingleton<GameManager>
{
    [SerializeField]
    public SaveSlotId saveSlotId;

    public GameplaySceneDataSO gameplaySceneDataSO;

    public void Save()
    {
        DataPersistenceManager.Instance.SaveGame(saveSlotId);
    }

    public void Load()
    {
        DataPersistenceManager.Instance.LoadGame(saveSlotId);
    }
}