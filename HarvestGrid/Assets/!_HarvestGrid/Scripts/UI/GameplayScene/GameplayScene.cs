
using DG.Tweening;
using LgTyLib.Core;
using LgTyLib.Modules.Audio;
using LgTyLib.Modules.DataPersistence;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public partial class GameplayScene : BaseSingleton<GameplayScene>, IDataPersistence
{
    [SerializeField]
    private GameplaySceneDataSO gameplaySceneData;

    [Header("Time")]
    [SerializeField]
    private float timeBudget = 300f;

    [SerializeField]
    private float remainingTime;

    public float TimeBudget => timeBudget;
    public float RemainingTime => remainingTime;

    [SerializeField]
    private Slider timeSlider;

    [Header("UI")]
    [SerializeField]
    private GameObject farmPlaceHolder;

    [SerializeField]
    private GameObject inventoryBar;

    [SerializeField]
    private GameObject storingSpace;

    [SerializeField]
    private GameObject shop;

    [Header("Inventory Bar Animation")]
    [SerializeField]
    private float inventoryBarHiddenY = 0f;

    [SerializeField]
    private float inventoryBarShownY = 907f;

    [Header("Storing Space Animation")]
    [SerializeField]
    private float storingSpaceHiddenY = -1600f;

    [SerializeField]
    private float storingSpaceShownY = 0f;

    [Header("Shop Animation")]
    [SerializeField]
    private float shopHiddenY = -100f;

    [SerializeField]
    private float shopShownY = 392f;

    [Header("Farm Animation")]
    [SerializeField]
    private float farmStartY = 0f;

    [SerializeField]
    private float farmEndY = 725f;

    [Header("Animation")]
    [SerializeField]
    private float slideDuration = 0.35f;

    [SerializeField]
    private Ease slideEase = Ease.OutCubic;

    private LevelSO levelSO;

    private RectTransform inventoryBarRect;
    private RectTransform storingSpaceRect;
    private RectTransform shopRect;
    private RectTransform farmRect;

    private bool isShopShown;

    public LevelSO LevelSO => levelSO;

    public void LoadGame(GameData gameData)
    {
        //
    }

    public void SaveGame(ref GameData gameData)
    {
        gameData.levelID = levelSO.levelID;
    }

    protected override void Awake()
    {
        base.Awake();

        Debug.Log("Entered GameplayScene");

        levelSO = gameplaySceneData.level;

        timeBudget = levelSO.timeBudget;
        remainingTime = timeBudget;

        var farm = Instantiate(
            levelSO.farmMono,
            farmPlaceHolder.transform
        );

        farm.transform.SetSiblingIndex(0);

        inventoryBarRect = inventoryBar.GetComponent<RectTransform>();
        storingSpaceRect = storingSpace.GetComponent<RectTransform>();
        shopRect = shop.GetComponent<RectTransform>();
        farmRect = farmPlaceHolder.GetComponent<RectTransform>();

        SetY(inventoryBarRect, inventoryBarHiddenY);
        SetY(storingSpaceRect, storingSpaceHiddenY);
        SetY(shopRect, shopHiddenY);
        SetY(farmRect, farmStartY);

        isShopShown = false;
    }

    private void Start()
    {
        StoringSpaceMono.Instance.SetData(
            levelSO.storingSpaceSO.storingSpace.Clone()
        );

        StartCoroutine(LoadNextFrame());

        InventoryMono.Instance.AddMoney(levelSO.money);

        AudioManager.Instance.PlaySoundFXClip(
            AudioManager.Instance.audioLibSO.bgm1,
            Vector3.zero,
            AudioManager.Instance.audioSettingsDataSO
                .audioSettingsData.musicAudioScale,
            true
        );
    }

    public void ConsumeTime(float amount)
    {
        if (amount <= 0f)
            return;

        remainingTime = Mathf.Max(0f, remainingTime - amount);

        timeSlider.value = timeBudget > 0f
            ? remainingTime / timeBudget
            : 0f;

        if (remainingTime <= 0f)
            LoseGame();
    }

    public void ResetTime()
    {
        remainingTime = timeBudget;
    }

    public bool IsTimeUp()
    {
        return remainingTime <= 0f;
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
        inventoryBarRect.DOKill();
        storingSpaceRect.DOKill();
        shopRect.DOKill();
        farmRect.DOKill();

        inventoryBarRect
            .DOAnchorPosY(inventoryBarShownY, slideDuration)
            .SetEase(slideEase);

        storingSpaceRect
            .DOAnchorPosY(storingSpaceShownY, slideDuration)
            .SetEase(slideEase);

        farmRect
            .DOAnchorPosY(farmEndY, slideDuration)
            .SetEase(slideEase);
    }

    public void HideBag()
    {
        // Close the shop first if it is open.
        if (isShopShown)
        {
            HideShop();
            return;
        }

        HideInventoryUI();
    }

private float inventoryBarShopOffset =>
    Mathf.Abs(shopShownY - shopHiddenY);

    public void ShowShop()
    {
        inventoryBarRect.DOKill();
        shopRect.DOKill();

        inventoryBarRect
            .DOAnchorPosY(
                inventoryBarShownY + inventoryBarShopOffset,
                slideDuration
            )
            .SetEase(slideEase);

        shopRect
            .DOAnchorPosY(shopShownY, slideDuration)
            .SetEase(slideEase);

        isShopShown = true;
        shop.gameObject.transform.SetSiblingIndex(100);
    }

    public void HideShop()
    {
        if (!isShopShown)
            return;

        inventoryBarRect.DOKill();
        shopRect.DOKill();

        inventoryBarRect
            .DOAnchorPosY(inventoryBarShownY, slideDuration)
            .SetEase(slideEase);

        shopRect
            .DOAnchorPosY(shopHiddenY, slideDuration)
            .SetEase(slideEase)
            .OnComplete(() =>
            {
                isShopShown = false;
            });
        shop.gameObject.transform.SetSiblingIndex(0);

    }


    private void HideInventoryUI()
    {
        inventoryBarRect.DOKill();
        storingSpaceRect.DOKill();
        farmRect.DOKill();

        inventoryBarRect
            .DOAnchorPosY(inventoryBarHiddenY, slideDuration)
            .SetEase(slideEase);

        storingSpaceRect
            .DOAnchorPosY(storingSpaceHiddenY, slideDuration)
            .SetEase(slideEase);

        farmRect
            .DOAnchorPosY(farmStartY, slideDuration)
            .SetEase(slideEase);
    }

    private void SetY(RectTransform rect, float y)
    {
        Vector2 position = rect.anchoredPosition;
        position.y = y;
        rect.anchoredPosition = position;
    }

    public void AddMoney(int amount)
    {
        InventoryMono.Instance.AddMoney(amount);
    }

}
