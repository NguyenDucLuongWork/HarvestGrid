using LgTyLib.Core;
using HarvestGrid.UI; // For AuthSession
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using LgTyLib.Modules.DataPersistence;
using HarvestGrid.Managers.Auth;

namespace HarvestGrid.Managers
{
    public class AuthManager : BaseSingleton<AuthManager>, IDataPersistence
    {
        [Header("Config")]
        [SerializeField] private string loginSceneName = "LoginScene";

        public event Action OnLoggedOut;

        public IAuthService AuthService { get; private set; }

        private string pendingUsername;

        protected override void Awake()
        {
            base.Awake();
            // Initialize with Local Auth. Later this can be swapped to PlayFabAuthService.
            AuthService = new LocalAuthService();
        }

        public void SetPendingLogin(string username)
        {
            pendingUsername = username;
        }

        public void Logout()
        {
            // Clear static session data
            AuthSession.Clear();
            AuthService?.Logout();
            
            // Notify systems to clean up
            OnLoggedOut?.Invoke();
            
            // Navigate back to Login Scene
            if (!string.IsNullOrEmpty(loginSceneName))
            {
                SceneManager.LoadScene(loginSceneName);
            }
            else
            {
                Debug.LogWarning("[AuthManager] LoginSceneName is not configured!");
            }
        }

        // --- IDataPersistence Implementation ---

        public void LoadGame(GameData data)
        {
            // Called when DataPersistenceManager.LoadGame is executed
            AuthSession.Set(data.playerId, data.username, "local_token_" + data.playerId);
            Debug.Log($"[AuthManager] Loaded Session for {data.username}");
        }

        public void SaveGame(ref GameData data)
        {
            // Called when DataPersistenceManager.SaveGame is executed
            if (string.IsNullOrEmpty(data.playerId))
            {
                // This is a new player!
                data.playerId = Guid.NewGuid().ToString();
                data.username = pendingUsername;
                Debug.Log($"[AuthManager] Generated new ID {data.playerId} for new player {data.username}");
            }
            else
            {
                // Existing player saving data, we can update username if we support renaming, 
                // but usually we just keep it.
                if (!string.IsNullOrEmpty(AuthSession.Username))
                {
                    data.username = AuthSession.Username;
                }
            }

            // Sync session if saving a new player
            if (string.IsNullOrEmpty(AuthSession.UserId))
            {
                AuthSession.Set(data.playerId, data.username, "local_token_" + data.playerId);
            }
        }
    }
}
