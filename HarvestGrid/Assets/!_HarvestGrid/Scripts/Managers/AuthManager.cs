using LgTyLib.Core;
using HarvestGrid.UI; // For AuthSession
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

using PlayFab;
using PlayFab.ClientModels;

namespace HarvestGrid.Managers
{
    public class AuthManager : BaseSingleton<AuthManager>
    {
        [Header("Config")]
        [SerializeField] private string loginSceneName = "LoginScene";

        public event Action OnLoggedOut;

        public void LoginPlayFab(string email, string password, Action<LoginResult> onSuccess, Action<PlayFabError> onError)
        {
            var request = new LoginWithEmailAddressRequest
            {
                Email = email,
                Password = password,
                InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
                {
                    GetPlayerProfile = true
                }
            };

            PlayFabClientAPI.LoginWithEmailAddress(request, 
                result => 
                {
                    // Cache session details
                    AuthSession.Set(result.PlayFabId, email, result.SessionTicket);
                    onSuccess?.Invoke(result);
                }, 
                error => 
                {
                    // Special behavior: If account doesn't exist, we auto-register for ease of testing
                    // in development. In production, you might want a separate Register screen.
                    if (error.Error == PlayFabErrorCode.AccountNotFound)
                    {
                        Debug.Log("[AuthManager] Account not found. Attempting to auto-register...");
                        RegisterPlayFab(email, password, onSuccess, onError);
                    }
                    else
                    {
                        onError?.Invoke(error);
                    }
                }
            );
        }

        private void RegisterPlayFab(string email, string password, Action<LoginResult> onSuccess, Action<PlayFabError> onError)
        {
            var request = new RegisterPlayFabUserRequest
            {
                Email = email,
                Password = password,
                RequireBothUsernameAndEmail = false
            };

            PlayFabClientAPI.RegisterPlayFabUser(request,
                result =>
                {
                    Debug.Log("[AuthManager] Auto-register success! Logging in...");
                    // Try to log in immediately after register
                    LoginPlayFab(email, password, onSuccess, onError);
                },
                error =>
                {
                    onError?.Invoke(error);
                }
            );
        }

        public void Logout()
        {
            // Clear static session data
            AuthSession.Clear();
            
            // Clear PlayFab auth tokens locally
            PlayFabClientAPI.ForgetAllCredentials();

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
    }
}
