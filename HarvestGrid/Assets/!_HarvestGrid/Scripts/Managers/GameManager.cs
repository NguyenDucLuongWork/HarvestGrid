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

    [SerializeField]
    private GamePage currentPage = GamePage.Unknown;

    public GamePage GetCurrentPage()
    {
        return currentPage;
    }

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

        LoadHomePage();
    }

    public void Save()
    {
        DataPersistenceManager.Instance.SaveGame(saveSlotId);
    }

    public void Load()
    {
        DataPersistenceManager.Instance.LoadGame(saveSlotId);
    }

    [ContextMenu("LoadGameplayPage")]
    public void LoadGameplayPage()
    {
        LoadPage(GamePage.Gameplay);
    }

    [ContextMenu("LoadHomePage")]
    public void LoadHomePage()
    {
        LoadPage(GamePage.Home);
    }

    public void LoadPage(GamePage page)
    {
        if (currentPage == page)
            return;

        switch (page)
        {
            case GamePage.Home:
                GameplayScene.Instance.ToggleGameplay(false);
                UIManager.Instance.mainMenuPage.SetActive(true);
                break;

            case GamePage.Gameplay:
                GameplayScene.Instance.ToggleGameplay(true);
                UIManager.Instance.mainMenuPage.SetActive(false);
                break;

            default:
                Debug.LogWarning($"Cannot load page: {page}");
                return;
        }

        currentPage = page;
    }
}

public enum GamePage
{
    Unknown,
    Login,
    Home,
    Gameplay
}