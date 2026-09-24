using DG.Tweening;
using UnityEngine;

public class BreathEffect : MonoBehaviour
{
    [SerializeField] private float scaleAmount = 1.05f;
    [SerializeField] private float duration = 0.8f;

    private Vector3 originalScale;
    private Tween breathTween;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        Play();
    }

    private void OnDisable()
    {
        Kill();
    }

    public void Play()
    {
        Kill();

        transform.localScale = originalScale;

        breathTween = transform
            .DOScale(originalScale * scaleAmount, duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject)
            ;
    }

    public void Kill()
    {
        breathTween?.Kill();
        breathTween = null;

        transform.localScale = originalScale;
    }
}