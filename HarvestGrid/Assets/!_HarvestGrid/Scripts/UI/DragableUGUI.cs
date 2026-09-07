using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class DragableUGUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Tooltip("If null, will use the root Canvas automatically.")]
    [SerializeField] private Canvas canvas;

    private RectTransform rectTransform;
    [SerializeField]
    private bool isDragging;
    [SerializeField]
    private bool pointerIsDown;

    public UnityEvent OnDragStarted;
    public UnityEvent OnDraggingEnded;
    public UnityEvent OnClick;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();
    }

    private void OnEnable()
    {
        InputManager.Instance.OnDragBegin += HandleDragBegin;
        InputManager.Instance.OnDrag += HandleDrag;
        InputManager.Instance.OnDragEnd += HandleDragEnd;
    }

    private void OnDisable()
    {
        InputManager.Instance.OnDragBegin -= HandleDragBegin;
        InputManager.Instance.OnDrag -= HandleDrag;
        InputManager.Instance.OnDragEnd -= HandleDragEnd;
        isDragging = false;
        pointerIsDown = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        //if (Inventory.Instance != null && Inventory.Instance.IsLocked) return;
        pointerIsDown = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!pointerIsDown) return;

        if (!isDragging)
        {
            OnClick?.Invoke();
        }

        pointerIsDown = false;
    }

    private void HandleDragBegin(Vector2 position)
    {
        if (!pointerIsDown) return;

        isDragging = true;
        OnDragStarted?.Invoke();
    }

    private void HandleDrag(Vector2 position, Vector2 delta)
    {
        if (!isDragging) return;
        rectTransform.anchoredPosition += delta / canvas.scaleFactor;
    }

    private void HandleDragEnd(Vector2 position)
    {
        if (!isDragging) return;
        isDragging = false;
        pointerIsDown = false;
        OnDraggingEnded?.Invoke();
    }
}