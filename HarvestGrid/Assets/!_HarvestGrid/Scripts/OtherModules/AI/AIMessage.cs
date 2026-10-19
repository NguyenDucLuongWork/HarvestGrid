
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AIMessage : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI message;
    [SerializeField] private Image relatedStuffIcon;
    [SerializeField] private RectTransform messageContainer;

    [Header("Animation")]
    [SerializeField, Min(0f)] private float fadeDuration = 0.3f;
    [SerializeField, Min(0f)] private float visibleDuration = 3f;

    private CanvasGroup canvasGroup;
    private Sequence messageSequence;

    private void Awake()
    {
        canvasGroup = messageContainer.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = messageContainer.gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        messageContainer.gameObject.SetActive(false);
    }

    public void GetHelp()
    {
        AIHelper.Instance.Analyse();

        SetContent(
            AIHelper.Instance.Recommendation.scenario.description,
            null
        );
    }

    public void SetContent(string content, Sprite image)
    {
        message.text = content;

        if (image != null)
        {
            relatedStuffIcon.gameObject.SetActive(true);
            relatedStuffIcon.sprite = image;
        }
        else
        {
            relatedStuffIcon.gameObject.SetActive(false);
        }

        ShowMessage();
    }

    private void ShowMessage()
    {
        // Cancel the previous animation and timer.
        messageSequence?.Kill();

        messageContainer.gameObject.SetActive(true);
        canvasGroup.alpha = 0f;

        messageSequence = DOTween.Sequence()
            .Append(canvasGroup.DOFade(1f, fadeDuration))
            .AppendInterval(visibleDuration)
            .Append(canvasGroup.DOFade(0f, fadeDuration))
            .OnComplete(() =>
            {
                messageContainer.gameObject.SetActive(false);
            });
    }

    private void OnDestroy()
    {
        messageSequence?.Kill();
    }
}
