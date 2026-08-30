namespace LgTyLib.Modules.DataPersistence
{
    /// <summary>
    /// Implement this interface on any MonoBehaviour that needs to participate
    /// in the save/load system.
    /// </summary>
    public interface IDataPersistence
    {
        void LoadGame(GameData gameData);
        void SaveGame(ref GameData gameData);
    }
}