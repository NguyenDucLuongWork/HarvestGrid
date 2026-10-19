using System;
using UnityEngine;
using LgTyLib.Modules.DataPersistence;

namespace HarvestGrid.Managers.Auth
{
    public class LocalAuthService : IAuthService
    {
        public void Login(string username, string password, Action<AuthResult> onSuccess, Action<string> onError)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                onError?.Invoke("Username is required.");
                return;
            }

            // In local auth, we treat the username as the identifier for UserData
            AuthManager.Instance.SetPendingLogin(username);

            // 1. Load UserData
            DataPersistenceManager.Instance.LoadUserData(username);

            if (string.IsNullOrEmpty(DataPersistenceManager.Instance.UserData.username))
            {
                // New user
                Debug.Log($"[LocalAuth] Creating new local UserData: {username}. Saving data...");
                DataPersistenceManager.Instance.UserData.username = username;
                DataPersistenceManager.Instance.SaveUserData(username);
            }
            else
            {
                Debug.Log($"[LocalAuth] Found existing UserData: {username}. Loading data...");
            }

            // 2. Load GameData
            // Using "Players" as playthroughId and username as slotName
            SaveSlotId slot = new SaveSlotId("Players", username);
            if (DataPersistenceManager.Instance.SaveExists(slot))
            {
                Debug.Log($"[LocalAuth] Found existing gameplay data for: {username}. Loading...");
                DataPersistenceManager.Instance.LoadGame(slot);
            }
            else
            {
                Debug.Log($"[LocalAuth] Creating new gameplay data for: {username}. Saving...");
                DataPersistenceManager.Instance.SaveGame(slot);
            }

            string userId = HarvestGrid.UI.AuthSession.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                onError?.Invoke("Failed to load or create player data.");
                return;
            }

            onSuccess?.Invoke(new AuthResult 
            { 
                PlayerId = userId,
                Username = HarvestGrid.UI.AuthSession.Username,
                SessionToken = HarvestGrid.UI.AuthSession.SessionToken
            });
        }

        public void Logout()
        {
            // Handled by AuthManager
        }
    }
}
