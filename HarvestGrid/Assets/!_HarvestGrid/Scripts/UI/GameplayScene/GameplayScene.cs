using DG.Tweening;
using LgTyLib.Core;
using LgTyLib.Modules.DataPersistence;
using System.Collections;
using UnityEngine;

public partial class GameplayScene : BaseSingleton<GameplayScene>, IDataPersistence
{
    [SerializeField]
    private GameplaySceneDataSO gameplaySceneData;
    [Header("OnShowBagUI")]
    [SerializeField]
    private GameObject farmPlaceHolder;

    [SerializeField]
    private GameObject bag;

    [SerializeField]
    private GameObject bag2;

    [Header("Bag Animation")]
    [SerializeField]
    private float bagHiddenY = -1200f;

    [SerializeField]
    private float bagShownY = 0f;

    [SerializeField]
    private float bag2HiddenY = 1200f;

    [SerializeField]
    private float bag2ShownY = 0f;

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
    private RectTransform bag2Rect;
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

        var farm = Instantiate(
            gameplaySceneData.level.farmMono,
            farmPlaceHolder.transform
        );

        farm.gameObject.transform.SetSiblingIndex( 0 );

        bagRect = bag.GetComponent<RectTransform>();
        bag2Rect = bag2.GetComponent<RectTransform>();
        farmRect = farmPlaceHolder.GetComponent<RectTransform>();

        // Initial positions
        SetBagY(bagHiddenY);
        SetBag2Y(bag2HiddenY);
        SetFarmY(farmStartY);
    }

    private void Start()
    {
        StoringSpaceMono.Instance.SetData(
            levelSO.storingSpaceSO.storingSpace.Clone()
        );

        StartCoroutine(LoadNextFrame());
        InventoryMono.Instance.AddMoney(levelSO.money);
    }

    private IEnumerator LoadNextFrame()
    {
        yield return null;

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
        bag2Rect.DOKill();
        farmRect.DOKill();

        // Bag 1: -1200 -> 0
        bagRect
            .DOAnchorPosY(bagShownY, slideDuration)
            .SetEase(slideEase);

        // Bag 2: +1200 -> 0
        bag2Rect
            .DOAnchorPosY(bag2ShownY, slideDuration)
            .SetEase(slideEase);

        // Farm: 0 -> 800
        farmRect
            .DOAnchorPosY(farmEndY, slideDuration)
            .SetEase(slideEase);
    }

    public void HideBag()
    {
        bagRect.DOKill();
        bag2Rect.DOKill();
        farmRect.DOKill();

        // Bag 1: 0 -> -1200
        bagRect
            .DOAnchorPosY(bagHiddenY, slideDuration)
            .SetEase(slideEase);

        // Bag 2: 0 -> +1200
        bag2Rect
            .DOAnchorPosY(bag2HiddenY, slideDuration)
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

    private void SetBag2Y(float y)
    {
        Vector2 position = bag2Rect.anchoredPosition;
        position.y = y;
        bag2Rect.anchoredPosition = position;
    }

    private void SetFarmY(float y)
    {
        Vector2 position = farmRect.anchoredPosition;
        position.y = y;
        farmRect.anchoredPosition = position;
    }

    public void AddMoney(int amount)
    {
        InventoryMono.Instance.AddMoney(amount);
    }
}