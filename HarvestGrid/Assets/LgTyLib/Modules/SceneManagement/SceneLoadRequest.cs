using UnityEngine.SceneManagement;

namespace LgTyLib.Modules.SceneManagement
{
    /// <summary>
    /// Options passed to SceneLoader.LoadSceneAsync.
    /// </summary>
    public struct SceneLoadRequest
    {
        /// <summary>Scene name or path to load.</summary>
        public string SceneName;

        /// <summary>Single (replace) or Additive.</summary>
        public LoadSceneMode LoadMode;

        /// <summary>
        /// If true, the scene will be fully loaded but held at 90% until
        /// you call SceneLoader.ActivatePendingScene() manually.
        /// Useful for "Press any key to continue" patterns.
        /// </summary>
        public bool HoldBeforeActivation;

        /// <summary>Optional tag forwarded through transition events (e.g. "gameplay", "menu").</summary>
        public string Tag;

        public static SceneLoadRequest Default(string sceneName) => new SceneLoadRequest
        {
            SceneName = sceneName,
            LoadMode = LoadSceneMode.Single,
            HoldBeforeActivation = false,
            Tag = string.Empty
        };
    }
}