using DG.Tweening;
using LgTyLib.Core;
using LgTyLib.Modules.DataPersistence;
using System.Collections;
using UnityEngine;

public class GameplayScene : BaseSingleton<GameplayScene>, IDataPersistence
{
    [SerializeField]
    private GameplaySceneDataSO gameplaySceneData;

    [SerializeField]
    private GameObject farmPlaceHolder;

    [SerializeField]
    private GameObject bag;

    [Header("Bag Animation")]
    [SerializeField]
    private float bagHiddenY = -1200f;

    [SerializeField]
    private float bagShownY = 0f;

    [Header("Farm Animation")]
    [SerializeField]
    private float farmStartY = 0f;

    [SerializeField]
    private float farmEndY = 800f;

    [Header("Animation")]
    [SerializeField]
    private float slideDuration = 0.35f;

    [SerializeField]
    private Ease slideEase = Ease.OutCubic;

    private LevelSO levelSO;

    private RectTransform bagRect;
    private RectTransform farmRect;

    public LevelSO LevelSO => levelSO;

    public void LoadGame(GameData gameData)
    {
        //
    }

    public void SaveGame(ref GameData gameData)
    {
        gameData.level = levelSO.levelID;
    }

    protected override void Awake()
    {
        base.Awake();
        Debug.Log("Enterd GameplayScene");
        levelSO = gameplaySceneData.level;
        Instantiate(gameplaySceneData.level.farmMono, farmPlaceHolder.transform);

        bagRect = bag.GetComponent<RectTransform>();
        farmRect = farmPlaceHolder.GetComponent<RectTransform>();

        // Initial positions
        SetBagY(bagHiddenY);
        SetFarmY(farmStartY);
    }

    private void Start()
    {
        StoringSpaceMono.Instance.SetData(
            levelSO.storingSpaceSO.storingSpace.Clone()
        );

        StartCoroutine(LoadNextFrame());
    }

    private IEnumerator LoadNextFrame()
    {
        yield return null; // wait 1 frame

        if (GameManager.Instance.gameplaySceneDataSO.toLoad)
        {
            Debug.Log("Loading...");
            GameManager.Instance.Load();
            DataTransfer.Instance.toLoad = false;
        }
    }



    public void ShowBag()
    {
        bagRect.DOKill();
        farmRect.DOKill();

        // Bag: -1200 -> 0
        bagRect
            .DOAnchorPosY(bagShownY, slideDuration)
            .SetEase(slideEase);

        // Farm: 0 -> 800
        farmRect
            .DOAnchorPosY(farmEndY, slideDuration)
            .SetEase(slideEase);
    }

    public void HideBag()
    {
        bagRect.DOKill();
        farmRect.DOKill();

        // Bag: 0 -> -1200
        bagRect
            .DOAnchorPosY(bagHiddenY, slideDuration)
            .SetEase(slideEase);

        // Farm: 800 -> 0
        farmRect
            .DOAnchorPosY(farmStartY, slideDuration)
            .SetEase(slideEase);
    }

    private void SetBagY(float y)
    {
        Vector2 position = bagRect.anchoredPosition;
        position.y = y;
        bagRect.anchoredPosition = position;
    }

    private void SetFarmY(float y)
    {
        Vector2 position = farmRect.anchoredPosition;
        position.y = y;
        farmRect.anchoredPosition = position;
    }
}