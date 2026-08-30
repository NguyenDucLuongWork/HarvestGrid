using UnityEngine;

namespace LgTyLib.Modules.SceneManagement
{
    /// <summary>
    /// ScriptableObject that holds live scene loading progress.
    /// Bind your UI Toolkit slider directly to LoadProgress.
    /// </summary>
    [CreateAssetMenu(fileName = "SceneLoadProgressSO", menuName = "LgTyLib/Scene Management/SceneLoadProgressSO")]
    public class SceneLoadProgressSO : ScriptableObject
    {
        [Range(0f, 1f)]
        [SerializeField] private float _loadProgress;

        [SerializeField] private string _currentSceneName;
        [SerializeField] private string _targetSceneName;
        [SerializeField] private SceneTransitionState _transitionState;

        /// <summary>0.0 – 1.0. Bind your slider to this.</summary>
        public float LoadProgress
        {
            get => _loadProgress;
            internal set
            {
                _loadProgress = Mathf.Clamp01(value);
                OnProgressChanged?.Invoke(_loadProgress);
            }
        }

        public string CurrentSceneName
        {
            get => _currentSceneName;
            internal set => _currentSceneName = value;
        }

        public string TargetSceneName
        {
            get => _targetSceneName;
            internal set => _targetSceneName = value;
        }

        public SceneTransitionState TransitionState
        {
            get => _transitionState;
            internal set
            {
                if (_transitionState == value) return;
                _transitionState = value;
                OnTransitionStateChanged?.Invoke(_transitionState);
            }
        }

        /// <summary>Fired every time LoadProgress changes.</summary>
        public event System.Action<float> OnProgressChanged;

        /// <summary>Fired every time the transition state changes.</summary>
        public event System.Action<SceneTransitionState> OnTransitionStateChanged;

        internal void Reset()
        {
            _loadProgress = 0f;
            _targetSceneName = string.Empty;
            _transitionState = SceneTransitionState.Idle;
        }
    }
}