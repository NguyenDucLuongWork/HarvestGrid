using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class UISwipeDetector : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Config")]
    [SerializeField] private float minimumSwipeDistance = 50f;

    [Header("Events")]
    public UnityEvent onSlideUp;
    public UnityEvent onSlideDown;

    private Vector2 pointerDownPosition;

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDownPosition = eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Vector2 pointerUpPosition = eventData.position;
        Vector2 delta = pointerUpPosition - pointerDownPosition;

        // Ignore small movements / clicks
        if (Mathf.Abs(delta.y) < minimumSwipeDistance)
            return;

        // Make sure it is primarily a vertical swipe
        if (Mathf.Abs(delta.y) <= Mathf.Abs(delta.x))
            return;

        if (delta.y > 0)
        {
            onSlideUp?.Invoke();
        }
        else
        {
            onSlideDown?.Invoke();
        }
    }
}
