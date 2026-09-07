using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.EventSystems;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
using LgTyLib.Core;

public class InputManager : BaseSingleton<InputManager>
{
    [Header("Click / Tap")]
    [Tooltip("Max screen pixels moved before a press is no longer a click.")]
    [SerializeField] private float clickMoveThreshold = 10f;

    [Header("Drag & Drop")]
    [Tooltip("Pixels of movement required before drag starts.")]
    [SerializeField] private float dragStartThreshold = 15f;

    [Header("Tap & Hold")]
    [Tooltip("Seconds held before OnHoldBegin fires.")]
    [SerializeField] private float holdDuration = 0.5f;

    public event Action<Vector2> OnClick;

    public event Action<Vector2> OnDragBegin;

    public event Action<Vector2 /*position*/, Vector2 /*delta*/> OnDrag;

    public event Action<Vector2> OnDragEnd;

    public event Action<Vector2> OnHoldBegin;

    public event Action<Vector2> OnHoldEnd;

    public event Action<Vector2> OnPointerPosition;
    public Vector2 CurrentPointerPosition { get; private set; }

    private enum PressState { Idle, Pressing, Holding, Dragging }
    private PressState state = PressState.Idle;

    private Vector2 pressStartPosition;
    private Vector2 previousPosition;
    private float pressStartTime;
    private bool pressOnUI;

    private int activeTouchFingerId = -1;

    private InputAction pointerPress;

    protected override void Awake()
    {
        base.Awake();

        pointerPress = new InputAction("PointerPress", InputActionType.Button, "<Mouse>/leftButton");

        pointerPress.performed += OnPressPerformed;
        pointerPress.canceled += OnPressCanceled;
    }

    private void OnEnable()
    {
        pointerPress.Enable();
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        pointerPress.Disable();
        EnhancedTouchSupport.Disable();
    }

    protected override void OnDestroy()
    {
        pointerPress.performed -= OnPressPerformed;
        pointerPress.canceled -= OnPressCanceled;
        base.OnDestroy();
    }

    private void Update()
    {
        if (IsMobilePlatform())
        {
            HandleTouchInput();
            TrackTouchPosition();
        }
        else
        {
            HandleMouseHeldUpdate();
            TrackMousePosition();
        }
    }

    private static Vector2 GetMousePosition()
    {
        return Mouse.current?.position.ReadValue() ?? Vector2.zero;
    }

    private void TrackMousePosition()
    {
        Vector2 pos = GetMousePosition();
        CurrentPointerPosition = pos;
        OnPointerPosition?.Invoke(pos);
    }

    private void TrackTouchPosition()
    {
        if (Touch.activeTouches.Count == 0) return;
        Vector2 pos = Touch.activeTouches[0].screenPosition;
        CurrentPointerPosition = pos;
        OnPointerPosition?.Invoke(pos);
    }

    private void OnPressPerformed(InputAction.CallbackContext ctx)
    {
        if (IsMobilePlatform()) return;

        BeginPress(GetMousePosition(), -1);
    }

    private void OnPressCanceled(InputAction.CallbackContext ctx)
    {
        if (IsMobilePlatform()) return;

        EndPress(GetMousePosition());
    }

    private void HandleMouseHeldUpdate()
    {
        if (state == PressState.Idle) return;

        UpdatePress(GetMousePosition());
    }

    private void HandleTouchInput()
    {
        if (Touch.activeTouches.Count != 1)
        {
            if (state != PressState.Idle)
                CancelPress();
            return;
        }

        Touch touch = Touch.activeTouches[0];

        switch (touch.phase)
        {
            case TouchPhase.Began:
                BeginPress(touch.screenPosition, touch.finger.index);
                break;

            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                UpdatePress(touch.screenPosition);
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                EndPress(touch.screenPosition);
                break;
        }
    }

    private void BeginPress(Vector2 pos, int fingerId)
    {
        pressStartPosition = pos;
        previousPosition = pos;
        pressStartTime = Time.unscaledTime;
        activeTouchFingerId = fingerId;
        pressOnUI = IsPointerOverUI(pos, fingerId);
        state = PressState.Pressing;

        StopAllCoroutines();
        StartCoroutine(HoldRoutine(pos));
    }

    private void UpdatePress(Vector2 pos)
    {
        switch (state)
        {
            case PressState.Pressing:
                {
                    float moved = Vector2.Distance(pos, pressStartPosition);
                    if (moved >= dragStartThreshold)
                    {
                        StopAllCoroutines();
                        state = PressState.Dragging;
                        OnDragBegin?.Invoke(pressStartPosition);
                        OnDrag?.Invoke(pos, pos - previousPosition);
                        previousPosition = pos;
                    }
                    break;
                }

            case PressState.Dragging:
                {
                    Vector2 delta = pos - previousPosition;
                    if (delta.sqrMagnitude > 0f)
                        OnDrag?.Invoke(pos, delta);
                    previousPosition = pos;
                    break;
                }

            case PressState.Holding:
                {
                    float movedFromStart = Vector2.Distance(pos, pressStartPosition);
                    if (movedFromStart >= dragStartThreshold)
                    {
                        state = PressState.Dragging;
                        OnDragBegin?.Invoke(pressStartPosition);
                        OnDrag?.Invoke(pos, pos - previousPosition);
                        previousPosition = pos;
                    }
                    break;
                }
        }
    }

    private void EndPress(Vector2 pos)
    {
        StopAllCoroutines();

        switch (state)
        {
            case PressState.Pressing:
                {
                    float moved = Vector2.Distance(pos, pressStartPosition);
                    bool wasClick = moved < clickMoveThreshold;
                    if (wasClick && !pressOnUI)
                        OnClick?.Invoke(pos);
                    break;
                }

            case PressState.Dragging:
                OnDragEnd?.Invoke(pos);
                break;

            case PressState.Holding:
                OnHoldEnd?.Invoke(pos);
                break;
        }

        ResetPressState();
    }

    private void CancelPress()
    {
        StopAllCoroutines();

        if (state == PressState.Dragging)
            OnDragEnd?.Invoke(previousPosition);
        else if (state == PressState.Holding)
            OnHoldEnd?.Invoke(previousPosition);

        ResetPressState();
    }

    private void ResetPressState()
    {
        state = PressState.Idle;
        previousPosition = Vector2.zero;
        activeTouchFingerId = -1;
    }
    private IEnumerator HoldRoutine(Vector2 startPos)
    {
        yield return new WaitForSecondsRealtime(holdDuration);

        if (state == PressState.Pressing)
        {
            state = PressState.Holding;
            OnHoldBegin?.Invoke(startPos);
            Debug.Log("Holding");
        }
    }

    private static bool IsPointerOverUI(Vector2 screenPos, int fingerId = -1)
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject(fingerId);
    }

    private static bool IsMobilePlatform()
    {
#if UNITY_EDITOR
        return UnityEngine.Device.Application.isMobilePlatform;
#else
        return Application.isMobilePlatform;
#endif
    }
}