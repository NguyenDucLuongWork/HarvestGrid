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

            // Using "Players" as playthroughId and username as slotName
            SaveSlotId slot = new SaveSlotId("Players", username);

            // Inform AuthManager about the pending login so it can provide data during SaveGame/LoadGame
            AuthManager.Instance.SetPendingLogin(username);

            if (DataPersistenceManager.Instance.SaveExists(slot))
            {
                // Player exists, load their data
                Debug.Log($"[LocalAuth] Found existing player: {username}. Loading data...");
                DataPersistenceManager.Instance.LoadGame(slot);
            }
            else
            {
                // Player does not exist, create new local data
                Debug.Log($"[LocalAuth] Creating new local player: {username}. Saving data...");
                DataPersistenceManager.Instance.SaveGame(slot); // This triggers AuthManager.SaveGame which generates ID
            }

            // At this point, DataPersistenceManager runs synchronously for local files.
            // AuthManager's IDataPersistence methods (LoadGame/SaveGame) have already been called.
            // So AuthSession is already populated.
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
