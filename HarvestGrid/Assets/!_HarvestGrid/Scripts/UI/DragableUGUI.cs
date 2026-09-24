using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class DragableUGUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Tooltip("If null, will use the root Canvas automatically.")]
    [SerializeField] private Canvas canvas;

    [SerializeField]
    private bool interactable = true;

    public bool Interactable
    {
        get => interactable;
        set
        {
            interactable = value;

            if (!interactable)
            {
                isDragging = false;
                pointerIsDown = false;
            }
        }
    }

    private RectTransform rectTransform;

    [SerializeField]
    private bool isDragging;

    [SerializeField]
    private bool pointerIsDown;

    public UnityEvent OnDragStarted;
    public UnityEvent<Vector2, Vector2> OnDragging;
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
        if (!interactable)
            return;

        pointerIsDown = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!interactable || !pointerIsDown)
            return;

        if (!isDragging)
        {
            OnClick?.Invoke();
        }

        pointerIsDown = false;
    }

    private void HandleDragBegin(Vector2 position)
    {
        if (!interactable || !pointerIsDown)
            return;

        isDragging = true;
        OnDragStarted?.Invoke();
    }

    private void HandleDrag(Vector2 position, Vector2 delta)
    {
        if (!interactable || !isDragging)
            return;

        Vector2 scaledDelta = delta / canvas.scaleFactor;

        rectTransform.anchoredPosition += scaledDelta;

        OnDragging?.Invoke(position, scaledDelta);
    }

    private void HandleDragEnd(Vector2 position)
    {
        if (!interactable || !isDragging)
            return;

        isDragging = false;
        pointerIsDown = false;

        OnDraggingEnded?.Invoke();
    }
}