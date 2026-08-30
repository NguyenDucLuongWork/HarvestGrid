namespace LgTyLib.Modules.SceneManagement
{
    public enum SceneTransitionState
    {
        /// <summary>No transition in progress.</summary>
        Idle,

        /// <summary>Loading screen / hooks are being shown before the load starts.</summary>
        PreLoad,

        /// <summary>Scene is actively being loaded by Unity.</summary>
        Loading,

        /// <summary>Scene is loaded but activation is being held (allowSceneActivation = false).</summary>
        WaitingForActivation,

        /// <summary>Scene is being activated and the old scene is being unloaded.</summary>
        Activating,

        /// <summary>Post-load hooks are running (e.g. hide loading screen).</summary>
        PostLoad,

        /// <summary>Transition fully complete.</summary>
        Complete
    }
}