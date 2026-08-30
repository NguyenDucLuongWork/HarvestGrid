# LgTyLib — Scene Management Module

## Files

| File | Purpose |
|---|---|
| `SceneLoader.cs` | Core singleton. Async load, hooks, state machine. |
| `SceneLoadProgressSO.cs` | ScriptableObject with `LoadProgress (float 0–1)`. Bind your slider here. |
| `SceneTransitionState.cs` | Enum: `Idle → PreLoad → Loading → WaitingForActivation → Activating → PostLoad → Complete` |
| `SceneLoadRequest.cs` | Struct with scene name, load mode, hold flag, tag. |

---

## Setup

1. Create a `SceneLoadProgressSO` asset:
   `Assets → Create → LgTyLib → Scene Management → Load Progress`

2. Add **SceneLoader** to a GameObject in your persistent/bootstrap scene.
   Assign the SO to the `Progress SO` field.

3. Bind your UI Toolkit slider to `SceneLoadProgressSO.LoadProgress`
   (use a `[SerializeField]` reference to the SO in your binding script).

---

## Basic Usage

```csharp
// Simple load
await SceneLoader.Instance.LoadSceneAsync(SceneLoadRequest.Default("GameScene"));

// Additive load
await SceneLoader.Instance.LoadSceneAsync(new SceneLoadRequest
{
    SceneName  = "HUD",
    LoadMode   = LoadSceneMode.Additive,
    HoldBeforeActivation = false
});

// Hold at 100% until player presses a key
await SceneLoader.Instance.LoadSceneAsync(new SceneLoadRequest
{
    SceneName            = "GameScene",
    HoldBeforeActivation = true
});
// ... show "Press any key" prompt ...
await SceneLoader.Instance.ActivatePendingScene();
```

---

## Hooks

Hooks are `async` delegates that run before / after the load. Use them for
fades, animations, or any async setup — they don't block the main thread.

```csharp
// In your loading screen controller
SceneLoader.Instance.RegisterPreLoadHook(ShowLoadingScreen);
SceneLoader.Instance.RegisterPostLoadHook(HideLoadingScreen);

// Hook signature
private async Awaitable ShowLoadingScreen()
{
    // play fade-in animation, then return
    await _animator.PlayAsync("FadeIn");
}

// Clean up when your controller is destroyed
private void OnDestroy()
{
    SceneLoader.Instance.UnregisterPreLoadHook(ShowLoadingScreen);
    SceneLoader.Instance.UnregisterPostLoadHook(HideLoadingScreen);
}
```

---

## Events

```csharp
SceneLoader.Instance.OnTransitionStateChanged += state => Debug.Log(state);
SceneLoader.Instance.OnLoadStarted   += req  => Debug.Log($"Loading {req.SceneName}");
SceneLoader.Instance.OnLoadCompleted += req  => Debug.Log("Done");
SceneLoader.Instance.OnLoadError     += msg  => Debug.LogError(msg);
```

---

## Progress SO — UI Toolkit Binding

```csharp
// Example binding script (attach to your loading screen UI document)
public class LoadingScreenBinder : MonoBehaviour
{
    [SerializeField] private SceneLoadProgressSO _progressSO;
    [SerializeField] private UIDocument _document;

    private Slider _slider;

    private void OnEnable()
    {
        _slider = _document.rootVisualElement.Q<Slider>("LoadSlider");
        _progressSO.OnProgressChanged += OnProgress;
        _progressSO.OnTransitionStateChanged += OnState;
    }

    private void OnDisable()
    {
        _progressSO.OnProgressChanged -= OnProgress;
        _progressSO.OnTransitionStateChanged -= OnState;
    }

    private void OnProgress(float v) => _slider.value = v;

    private void OnState(SceneTransitionState s)
    {
        // Show/hide based on state
        bool visible = s != SceneTransitionState.Idle && s != SceneTransitionState.Complete;
        _document.rootVisualElement.style.display = visible
            ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
```

---

## Transition State Flow

```
Idle
 └─► PreLoad        (pre-load hooks run)
      └─► Loading       (AsyncOperation active, progress 0→1)
           └─► [WaitingForActivation]   ← only if HoldBeforeActivation = true
                └─► Activating
                     └─► PostLoad      (post-load hooks run)
                          └─► Complete
                               └─► Idle
```