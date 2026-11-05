using LgTyLib.Core;
using LgTyLib.Modules.DataPersistence;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : BaseSingleton<GameManager>
{
    [SerializeField]
    public SaveSlotId saveSlotId;

    public GameplaySceneDataSO gameplaySceneDataSO;

    private void Start()
    {
        StartCoroutine(SetupNextFrame());
    }

    private IEnumerator SetupNextFrame()
    {
        yield return null;
        Setup();
        
    }

    public void Setup()
    {
        UserManager.Instance.Load();

        LevelsManager.Instance.SyncLoadView();
    }

    public void Save()
    {
        DataPersistenceManager.Instance.SaveGame(saveSlotId);
    }

    public void Load()
    {
        DataPersistenceManager.Instance.LoadGame(saveSlotId);
    }

    public CurrentScene GetCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        switch (sceneName)
        {
            case "HomeScene":
                return CurrentScene.Home;

            case "GameplayScene":
                return CurrentScene.Gameplay;

            case "LoginScene":
                return CurrentScene.Login;

            default:
                Debug.LogWarning($"Unknown scene: {sceneName}");
                return CurrentScene.Unknown;
        }
    }

}

public enum CurrentScene
{
    Unknown,
    Login,
    Home,
    Gameplay
}