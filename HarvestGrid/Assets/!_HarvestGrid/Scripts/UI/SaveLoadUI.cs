using LgTyLib.Modules.DataPersistence;
using UnityEngine;


public class SaveLoadUI : MonoBehaviour
{
    [SerializeField]
    public SaveSlotId saveSlotId;
    public void Save()
    {
        DataPersistenceManager.Instance.SaveGame(saveSlotId);
    }

    public void Load()
    {
        DataPersistenceManager.Instance.LoadGame(saveSlotId);
    }
}