using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class ScaleDown : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler
{
    [SerializeField] private float scale = 0.9f;
    [SerializeField] private float duration = 0.1f;

    private Vector3 originalScale;
    private Tween scaleTween;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void OnDisable()
    {
        ResetScale();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        scaleTween?.Kill();

        scaleTween = transform
            .DOScale(originalScale * scale, duration)
            .SetEase(Ease.OutQuad);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ResetScale();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetScale();
    }

    private void ResetScale()
    {
        scaleTween?.Kill();

        scaleTween = transform
            .DOScale(originalScale, duration)
            .SetEase(Ease.OutBack);
    }
}