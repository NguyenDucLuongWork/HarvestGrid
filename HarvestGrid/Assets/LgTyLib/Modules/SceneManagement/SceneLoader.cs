using LgTyLib.Core;
using LgTyLib.Modules.SceneManagement;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LgTyLib.SceneManagement
{
    /// <summary>
    /// Core scene management singleton.
    /// Handles async loading, progress reporting, pre/post hooks, and transition states.
    ///
    /// Usage:
    ///   await SceneLoader.Instance.LoadSceneAsync(SceneLoadRequest.Default("GameScene"));
    ///   await SceneLoader.Instance.LoadSceneAsync(mySceneAsset);          // Editor only
    ///
    /// Hooks:
    ///   SceneLoader.Instance.RegisterPreLoadHook(async () => await ShowLoadingScreen());
    ///   SceneLoader.Instance.RegisterPostLoadHook(async () => await HideLoadingScreen());
    /// </summary>
    public class SceneLoader : BaseSingleton<SceneLoader>
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Progress SO")]
        [Tooltip("Assign your SceneLoadProgressSO here. Bind your UI Toolkit slider to it.")]
        [SerializeField] private SceneLoadProgressSO _progressSO;

        [Header("Settings")]
        [Tooltip("Minimum fake progress duration in seconds. Prevents a flash of 0→100.")]
        [SerializeField, Min(0f)] private float _minLoadDuration = 0.5f;

        // ── State ─────────────────────────────────────────────────────────────────
        private AsyncOperation _pendingOperation;
        private bool _isLoading = false;

        // ── Hooks ─────────────────────────────────────────────────────────────────
        // Pre-load: runs BEFORE the AsyncOperation starts (e.g. fade in loading screen).
        private readonly List<Func<Awaitable>> _preLoadHooks = new();

        // Post-load: runs AFTER scene is activated (e.g. fade out loading screen).
        private readonly List<Func<Awaitable>> _postLoadHooks = new();

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when any transition state changes.</summary>
        public event Action<SceneTransitionState> OnTransitionStateChanged;

        /// <summary>Fired when a load starts. Passes the SceneLoadRequest.</summary>
        public event Action<SceneLoadRequest> OnLoadStarted;

        /// <summary>Fired when a load completes successfully.</summary>
        public event Action<SceneLoadRequest> OnLoadCompleted;

        /// <summary>Fired if an error occurs during loading.</summary>
        public event Action<string> OnLoadError;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Unity Event-compatible overload. Assign this directly in the Inspector.
        /// Loads in Single mode, no hold.
        /// </summary>
        public void LoadScene(string sceneName)
        {
            LoadSceneAsync(SceneLoadRequest.Default(sceneName)).GetAwaiter();
        }

        /// <summary>
        /// Unity Event-compatible overload using a build index.
        /// Loads in Single mode, no hold.
        /// </summary>
        public void LoadScene(int sceneBuildIndex)
        {
            var request = new SceneLoadRequest
            {
                SceneName = SceneUtility.GetScenePathByBuildIndex(sceneBuildIndex),
                LoadMode = LoadSceneMode.Single,
                HoldBeforeActivation = false,
            };
            LoadSceneAsync(request).GetAwaiter();
        }

        /// <summary>
        /// Unity Event-compatible overload. Loads additively — useful for HUD, overlays.
        /// </summary>
        public void LoadSceneAdditive(string sceneName)
        {
            var request = new SceneLoadRequest
            {
                SceneName = sceneName,
                LoadMode = LoadSceneMode.Additive,
                HoldBeforeActivation = false,
            };
            LoadSceneAsync(request).GetAwaiter();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Unity Event-compatible SceneAsset overload. Assign in the Inspector.
        /// Loads in Single mode, no hold.
        /// Compiled out of builds — safe to use freely in editor-side MonoBehaviours.
        /// </summary>
        public void LoadScene(SceneAsset sceneAsset)
        {
            LoadSceneAsync(SceneLoadRequest.Default(GetSceneName(sceneAsset))).GetAwaiter();
        }

        /// <summary>
        /// Unity Event-compatible SceneAsset overload. Loads additively.
        /// Compiled out of builds — safe to use freely in editor-side MonoBehaviours.
        /// </summary>
        public void LoadSceneAdditive(SceneAsset sceneAsset)
        {
            var request = new SceneLoadRequest
            {
                SceneName = GetSceneName(sceneAsset),
                LoadMode = LoadSceneMode.Additive,
                HoldBeforeActivation = false,
            };
            LoadSceneAsync(request).GetAwaiter();
        }

        /// <summary>
        /// Awaitable SceneAsset overload with full control.
        /// Resolves the asset to its scene name and delegates to LoadSceneAsync(SceneLoadRequest).
        /// Compiled out of builds — safe to use freely in editor-side MonoBehaviours.
        /// </summary>
        /// <param name="sceneAsset">The SceneAsset to load.</param>
        /// <param name="loadMode">Single (default) or Additive.</param>
        /// <param name="holdBeforeActivation">
        ///   If true, the scene loads to 90% then waits for ActivatePendingScene().
        /// </param>
        public async Awaitable LoadSceneAsync(
            SceneAsset sceneAsset,
            LoadSceneMode loadMode = LoadSceneMode.Single,
            bool holdBeforeActivation = false)
        {
            if (sceneAsset == null)
            {
                ReportError("LoadSceneAsync(SceneAsset) called with a null SceneAsset.");
                return;
            }

            var request = new SceneLoadRequest
            {
                SceneName = GetSceneName(sceneAsset),
                LoadMode = loadMode,
                HoldBeforeActivation = holdBeforeActivation,
            };

            await LoadSceneAsync(request);
        }

        // ── SceneAsset helpers ────────────────────────────────────────────────────

        /// <summary>
        /// Resolves a SceneAsset to the scene name expected by SceneManager.
        /// Uses the asset path so the name stays accurate even after renames.
        /// Example: "Assets/Scenes/GameScene.unity" → "GameScene"
        /// </summary>
        private static string GetSceneName(SceneAsset sceneAsset)
        {
            if (sceneAsset == null) return string.Empty;

            // AssetDatabase gives us the full project-relative path; strip path + extension.
            string path = AssetDatabase.GetAssetPath(sceneAsset);
            return System.IO.Path.GetFileNameWithoutExtension(path);
        }
#endif

        /// <summary>
        /// Load a scene asynchronously with full control. Awaitable — await this in code.
        /// Runs pre-load hooks → loads → (optional hold) → activates → runs post-load hooks.
        /// </summary>
        public async Awaitable LoadSceneAsync(SceneLoadRequest request)
        {
            if (_isLoading)
            {
                Debug.LogWarning($"[SceneLoader] Load requested for '{request.SceneName}' but a load is already in progress. Request ignored.");
                return;
            }

            if (string.IsNullOrEmpty(request.SceneName))
            {
                ReportError("SceneLoadRequest.SceneName is null or empty.");
                return;
            }

            _isLoading = true;
            _progressSO?.Reset();

            SetState(SceneTransitionState.PreLoad, request);
            OnLoadStarted?.Invoke(request);

            // ── Pre-load hooks ────────────────────────────────────────────────
            await RunHooks(_preLoadHooks, "pre-load");

            // ── Begin async load ──────────────────────────────────────────────
            SetState(SceneTransitionState.Loading, request);

            AsyncOperation op;
            try
            {
                op = SceneManager.LoadSceneAsync(request.SceneName, request.LoadMode);
            }
            catch (Exception e)
            {
                ReportError($"Failed to start loading scene '{request.SceneName}': {e.Message}");
                _isLoading = false;
                return;
            }

            if (op == null)
            {
                ReportError($"SceneManager returned null AsyncOperation for scene '{request.SceneName}'. Check the scene is in Build Settings.");
                _isLoading = false;
                return;
            }

            // Hold at 90% if requested
            op.allowSceneActivation = !request.HoldBeforeActivation;
            _pendingOperation = request.HoldBeforeActivation ? op : null;

            // ── Track progress ────────────────────────────────────────────────
            float elapsed = 0f;
            while (!op.isDone)
            {
                elapsed += Time.unscaledDeltaTime;

                // Unity reports 0–0.9 during load, then jumps to 1.0 on activation.
                float rawProgress = op.progress / 0.9f; // normalise to 0–1

                // Blend with minimum duration so progress doesn't flash
                float timedProgress = _minLoadDuration > 0f
                    ? Mathf.Min(rawProgress, elapsed / _minLoadDuration)
                    : rawProgress;

                if (_progressSO != null)
                    _progressSO.LoadProgress = timedProgress;

                // Held at 90% — pause tracking and wait for manual activation
                if (request.HoldBeforeActivation && op.progress >= 0.9f)
                {
                    if (_progressSO != null)
                        _progressSO.LoadProgress = 1f;

                    SetState(SceneTransitionState.WaitingForActivation, request);
                    break;
                }

                await Awaitable.NextFrameAsync();
            }

            // If not held, we're now activating
            if (!request.HoldBeforeActivation)
            {
                if (_progressSO != null)
                    _progressSO.LoadProgress = 1f;

                SetState(SceneTransitionState.Activating, request);
                // Wait one frame for scene to fully activate
                await Awaitable.NextFrameAsync();
            }

            // If held, caller must call ActivatePendingScene() — we return here and
            // let them await that separately.
            if (request.HoldBeforeActivation)
            {
                _isLoading = false;
                return;
            }

            // ── Scene is live — reset progress before post-load hooks run ────
            ResetProgress();

            // ── Post-load hooks ───────────────────────────────────────────────
            SetState(SceneTransitionState.PostLoad, request);
            await RunHooks(_postLoadHooks, "post-load");

            SetState(SceneTransitionState.Complete, request);
            OnLoadCompleted?.Invoke(request);

            _isLoading = false;
            _pendingOperation = null;
            if (_progressSO != null) _progressSO.TransitionState = SceneTransitionState.Idle;
        }

        /// <summary>
        /// Activate the scene held by HoldBeforeActivation.
        /// Call this after the user presses a key / your loading screen is ready.
        /// Awaitable — awaiting it will wait for post-load hooks to complete.
        /// </summary>
        public async Awaitable ActivatePendingScene()
        {
            if (_pendingOperation == null)
            {
                Debug.LogWarning("[SceneLoader] ActivatePendingScene called but no scene is pending activation.");
                return;
            }

            SetState(SceneTransitionState.Activating);
            _pendingOperation.allowSceneActivation = true;

            while (!_pendingOperation.isDone)
                await Awaitable.NextFrameAsync();

            await Awaitable.NextFrameAsync();

            ResetProgress();

            SetState(SceneTransitionState.PostLoad);
            await RunHooks(_postLoadHooks, "post-load");

            SetState(SceneTransitionState.Complete);
            OnLoadCompleted?.Invoke(default);

            _isLoading = false;
            _pendingOperation = null;
            if (_progressSO != null) _progressSO.TransitionState = SceneTransitionState.Idle;
        }

        /// <summary>Whether a scene load is currently in progress.</summary>
        public bool IsLoading => _isLoading;

        /// <summary>Whether a loaded scene is waiting for manual activation.</summary>
        public bool HasPendingActivation => _pendingOperation != null;

        /// <summary>Current transition state.</summary>
        public SceneTransitionState CurrentState => _progressSO != null
            ? _progressSO.TransitionState
            : SceneTransitionState.Idle;

        // ── Hook Registration ─────────────────────────────────────────────────────

        /// <summary>
        /// Register an async hook that runs before the scene load begins.
        /// Hooks run in registration order.
        /// Example: show loading screen, fade out current scene.
        /// </summary>
        public void RegisterPreLoadHook(Func<Awaitable> hook)
        {
            if (hook != null) _preLoadHooks.Add(hook);
        }

        /// <summary>
        /// Register an async hook that runs after the scene is active.
        /// Example: hide loading screen, fade in new scene.
        /// </summary>
        public void RegisterPostLoadHook(Func<Awaitable> hook)
        {
            if (hook != null) _postLoadHooks.Add(hook);
        }

        /// <summary>Remove a previously registered pre-load hook.</summary>
        public void UnregisterPreLoadHook(Func<Awaitable> hook) => _preLoadHooks.Remove(hook);

        /// <summary>Remove a previously registered post-load hook.</summary>
        public void UnregisterPostLoadHook(Func<Awaitable> hook) => _postLoadHooks.Remove(hook);

        // ── Internal ──────────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();

            // base.Awake() may have destroyed this as a duplicate — bail early if so
            if (this == null) return;

            if (_progressSO != null)
                _progressSO.CurrentSceneName = SceneManager.GetActiveScene().name;

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_progressSO != null)
                _progressSO.CurrentSceneName = scene.name;
        }

        private void SetState(SceneTransitionState state, SceneLoadRequest? request = null)
        {
            if (_progressSO != null)
            {
                _progressSO.TransitionState = state;
                if (request.HasValue)
                    _progressSO.TargetSceneName = request.Value.SceneName;
            }
            OnTransitionStateChanged?.Invoke(state);
        }

        private async Awaitable RunHooks(List<Func<Awaitable>> hooks, string hookType)
        {
            foreach (var hook in hooks)
            {
                try
                {
                    await hook();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SceneLoader] Exception in {hookType} hook: {e.Message}\n{e.StackTrace}");
                }
            }
        }

        private void ResetProgress()
        {
            if (_progressSO == null) return;
            _progressSO.LoadProgress = 0f;
            _progressSO.TargetSceneName = string.Empty;
        }

        private void ReportError(string message)
        {
            Debug.LogError($"[SceneLoader] {message}");
            OnLoadError?.Invoke(message);
            SetState(SceneTransitionState.Idle);
        }
    }
}