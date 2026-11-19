using System.Threading.Tasks;
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

    [Header("LLM")]
    [SerializeField] private LLMTester llm;
    [SerializeField] private string loadingText = "Hmm, let me think...";
    [TextArea(3, 8)]
    [SerializeField]
    private string rewritePrompt =
        "Rewrite the following hint in a fun, friendly and natural tone, like a playful game companion talking to the player. " +
        "Keep every important detail and the same meaning. Use at most 2 short sentences. " +
        "Reply with ONLY the rewritten text, no quotes or explanations.\n\nHint: ";

    [Header("Animation")]
    [SerializeField, Min(0f)] private float fadeDuration = 0.3f;
    [SerializeField, Min(0f)] private float visibleDuration = 3f;

    private CanvasGroup canvasGroup;
    private Sequence messageSequence;
    private int requestId; // used to ignore outdated LLM replies

    private void Awake()
    {
        canvasGroup = messageContainer.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = messageContainer.gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        messageContainer.gameObject.SetActive(false);
    }

    public async void GetHelp()
    {
        AIHelper.Instance.Analyse();

        string original = AIHelper.Instance.Recommendation.scenario.description;
        int myRequest = ++requestId;

        // Show a placeholder while the LLM is generating.
        SetContent(loadingText, null, autoHide: false);

        string funny = await Humanize(original);

        // Object destroyed, or a newer request started while waiting.
        if (this == null || myRequest != requestId)
            return;

        Debug.Log("LLM Result:" + funny);
        SetContent(funny, null);
    }

    /// <summary>Rewrites the text with the LLM. Falls back to the original on failure.</summary>
    private async Task<string> Humanize(string original)
    {
        if (llm == null || string.IsNullOrWhiteSpace(original))
            return original;

        string reply = await llm.AskAsync(rewritePrompt + original);

        if (string.IsNullOrWhiteSpace(reply))
            return original;

        return reply.Trim().Trim('"', '\'', '“', '”');
    }

    public void SetContent(string content, Sprite image, bool autoHide = true)
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

        ShowMessage(autoHide);
    }

    private void ShowMessage(bool autoHide)
    {
        // Cancel the previous animation and timer.
        messageSequence?.Kill();

        messageContainer.gameObject.SetActive(true);
        canvasGroup.alpha = 0f;

        messageSequence = DOTween.Sequence().Append(canvasGroup.DOFade(1f, fadeDuration));

        if (autoHide)
        {
            messageSequence
                .AppendInterval(visibleDuration)
                .Append(canvasGroup.DOFade(0f, fadeDuration))
                .OnComplete(() => messageContainer.gameObject.SetActive(false));
        }
    }

    private void OnDestroy()
    {
        messageSequence?.Kill();
    }
}