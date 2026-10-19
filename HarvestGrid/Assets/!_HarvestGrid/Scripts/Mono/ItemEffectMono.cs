using DG.Tweening;
using LgTyLib.Modules.Audio;
using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ItemEffectMono : MonoBehaviour
{
    [SerializeField] private ItemUseContext useContext;

    [Header("Animation")]
    [SerializeField] private float rotateDegree = 15f;
    [SerializeField] private float moveHeight = 40f;
    [SerializeField] private float duration = 0.8f;

    [Header("Appear")]
    [SerializeField] private float appearDuration = 0.2f;
    [SerializeField] private float startScale = 0.5f;

    [Header("Fade")]
    [SerializeField] private float fadeOutDuration = 0.2f;

    private Image image;
    private RectTransform rect;
    private Vector2 startPosition;
    private float remainingTime;
    private Tween currentTween;
    private Action<GameObject> onFinished;
    private bool isPlaying;
    public float RemainingTime => remainingTime;
    public ItemUseContext UseContext => useContext;
    private float TotalDuration =>
        appearDuration + duration + duration * 0.5f + fadeOutDuration;

    private void Awake()
    {
        image = GetComponent<Image>();
        rect = image.rectTransform;
        gameObject.SetActive(false);
    }

    public void Play(ItemUseContext useContext, Action<GameObject> onFinished)
        => Play(useContext, onFinished, -1f);

    public void Play(ItemUseContext useContext, Action<GameObject> onFinished, float remainingTime)
    {
        if (useContext == null || useContext.Item == null)
            return;

        // Playing Soung
        AudioManager.Instance.PlaySoundFXClipWithPool(
                useContext.Use.sfxClip,
                this.transform.position,
                AudioManager.Instance.audioSettingsDataSO.audioSettingsData.sfxAudioScale
            );

        // Previous effect still running: finish it properly so its slot is released
        if (isPlaying)
            Finish();

        this.useContext = useContext;
        this.onFinished = onFinished;
        isPlaying = true;

        image.sprite = useContext.Item.Icon;
        rect.position = useContext.TargetSlot.GetFarmSlotMono().transform.position;
        startPosition = rect.anchoredPosition;

        SetEffected(useContext, true);

        // < 0 = play from start, otherwise resume with that much time left
        float remaining = remainingTime < 0f ? TotalDuration : Mathf.Min(remainingTime, TotalDuration);

        if (remaining <= 0f)
        {
            Finish();
            return;
        }

        StartAnimation(remaining);
    }

    private void StartAnimation(float remaining)
    {
        this.remainingTime = remaining;
        ResetVisual();
        gameObject.SetActive(true);

        float total = TotalDuration;
        currentTween = CreateAnimation();

        // Attach callbacks BEFORE Goto so completion is never missed
        currentTween.OnUpdate(() =>
            this.remainingTime = Mathf.Max(0f, total - currentTween.Elapsed()));
        currentTween.OnComplete(Finish);

        float elapsed = total - remaining;
        if (elapsed > 0f)
            currentTween.Goto(elapsed, true);
    }

    private Sequence CreateAnimation()
    {
        Sequence s = DOTween.Sequence();

        s.Append(image.DOFade(1f, appearDuration).SetEase(Ease.Linear));
        s.Join(rect.DOScale(1f, appearDuration).SetEase(Ease.OutBack));

        s.Append(rect.DOAnchorPosY(startPosition.y + moveHeight, duration).SetEase(Ease.InOutSine));
        s.Join(rect.DOLocalRotate(new Vector3(0f, 0f, rotateDegree), duration).SetEase(Ease.InOutSine));

        s.Append(rect.DOAnchorPosY(startPosition.y, duration * 0.5f).SetEase(Ease.OutBounce));
        s.Append(image.DOFade(0f, fadeOutDuration));

        return s;
    }

    private void ResetVisual()
    {
        currentTween?.Kill();
        currentTween = null;

        rect.anchoredPosition = startPosition;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one * startScale;

        Color c = image.color;
        c.a = 0f;
        image.color = c;
    }

    private void Finish()
    {
        if (!isPlaying) return;      // guard against double calls
        isPlaying = false;

        var ctx = useContext;
        var callback = onFinished;
        onFinished = null;
        remainingTime = 0f;

        currentTween?.Kill();
        currentTween = null;

        try
        {
            ctx.Use?.Apply(ctx);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            SetEffected(ctx, false);   // always runs, even if Apply throws
            gameObject.SetActive(false);
        }

        callback?.Invoke(gameObject);
    }

    private static void SetEffected(ItemUseContext ctx, bool value)
    {
        if (ctx?.TargetSlot == null) return;
        ctx.TargetSlot.beingEffected = value;
    }

    private void OnDisable()
    {
        // Disabled externally mid-animation: don't leave the slot locked
        if (!isPlaying) return;
        isPlaying = false;
        currentTween?.Kill();
        currentTween = null;
        SetEffected(useContext, false);
    }

    private void OnDestroy()
    {
        currentTween?.Kill();
        SetEffected(useContext, false);
        onFinished = null;
    }
}