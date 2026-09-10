using LgTyLib.Core;
using LgTyLib.Modules.DataPersistence;
using UnityEngine;

public class GameplayScene : BaseSingleton<GameplayScene>, IDataPersistence
{
    [SerializeField]
    private GameplaySceneDataSO gameplaySceneData;
    [SerializeField]
    private GameObject farmPlaceHolder;

    private LevelSO levelSO;
    public LevelSO LevelSO => levelSO;

    public void LoadGame(GameData gameData)
    {
        //
    }

    public void SaveGame(ref GameData gameData)
    {
        gameData.level = levelSO.levelID;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Awake()
    {
        base.Awake();
        levelSO = gameplaySceneData.level;
        Instantiate(gameplaySceneData.level.farmMono, farmPlaceHolder.transform);
    }

    void Start()
    {
        //StoringSpaceMono.Instance.SetData(levelSO.storingSpaceSO.storingSpace);
        if (gameplaySceneData.toLoad)
        {
            GameManager.Instance.Load();
            gameplaySceneData.toLoad = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
