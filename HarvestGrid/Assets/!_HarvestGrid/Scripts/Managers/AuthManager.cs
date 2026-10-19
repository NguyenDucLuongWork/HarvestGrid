using LgTyLib.Core;
using HarvestGrid.UI; // For AuthSession
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using LgTyLib.Modules.DataPersistence;
using HarvestGrid.Managers.Auth;

namespace HarvestGrid.Managers
{
    public class AuthManager : BaseSingleton<AuthManager>, IUserDataPersistence
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
                LgTyLib.SceneManagement.SceneLoader.Instance.LoadScene(loginSceneName);
            }
            else
            {
                Debug.LogWarning("[AuthManager] LoginSceneName is not configured!");
            }
        }

        // --- IUserDataPersistence Implementation ---

        public void LoadUserData(UserData data)
        {
            // Called when DataPersistenceManager.LoadUserData is executed
            AuthSession.Set(data.userId, data.username, "local_token_" + data.userId);
            Debug.Log($"[AuthManager] Loaded Session for {data.username}");
        }

        public void SaveUserData(ref UserData data)
        {
            // Called when DataPersistenceManager.SaveUserData is executed
            if (string.IsNullOrEmpty(data.userId))
            {
                // This is a new player!
                data.userId = Guid.NewGuid().ToString();
                data.username = pendingUsername;
                Debug.Log($"[AuthManager] Generated new ID {data.userId} for new player {data.username}");
            }
            else
            {
                if (!string.IsNullOrEmpty(AuthSession.Username))
                {
                    data.username = AuthSession.Username;
                }
            }

            // Sync session if saving a new player
            if (string.IsNullOrEmpty(AuthSession.UserId))
            {
                AuthSession.Set(data.userId, data.username, "local_token_" + data.userId);
            }
        }
    }
}
