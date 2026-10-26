using LgTyLib.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using HarvestGrid.UI;
using System.Text;

namespace LgTyLib.Modules.DataPersistence
{
    public class DataPersistenceManager : BaseSingleton<DataPersistenceManager>
    {
        [Header("File Storage Config")]
        [SerializeField] private string savesFolderName = "Saves";

        private FileDataHandler fileDataHandler;
        private GameData gameData;
        private UserData userData;
        private List<IDataPersistence> dataPersistenceList;
        private List<IUserDataPersistence> userDataPersistenceList;
        private SaveSlotId? activeSlot;
        private float sessionStartTime;
        private string currentUsername;
        private bool cloudSyncReady = false;
        private const string CloudSavePrefix = "HG_SAVE_";

        // ── Events ───────────────────────────────────────────────────
        public static event Action<SaveSlotId> OnGameLoaded;
        public static event Action<SaveSlotId> OnGameSaved;
        public static event Action<SaveSlotId> OnSaveDeleted;
        public static event Action<string> OnPlaythroughDeleted;
        public static event Action OnUserDataLoaded;
        public static event Action OnUserDataSaved;


        //create key
        private static string EncodeKeyPart(string value)
        {
            return Convert.ToBase64String(
                Encoding.UTF8.GetBytes(value)
            ).TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        }

        private static string DecodeKeyPart(string value)
        {
            value = value.Replace('-', '+').Replace('_', '/');

            switch (value.Length % 4)
            {
                case 2: value += "=="; break;
                case 3: value += "="; break;
            }

            return Encoding.UTF8.GetString(
                Convert.FromBase64String(value)
            );
        }

        private static string GetCloudSaveKey(SaveSlotId slot)
        {
            return CloudSavePrefix
                + EncodeKeyPart(slot.playthroughId)
                + "."
                + EncodeKeyPart(slot.slotName);
        }

        // save cloud and ghi đè local
        public void LoadCloudSaves(Action<bool> onComplete = null)
        {
            cloudSyncReady = false;

            if (!AuthSession.IsAuthenticated)
            {
                Debug.LogError("Cannot sync: player is not authenticated.");
                onComplete?.Invoke(false);
                return;
            }

            PlayFabClientAPI.GetUserData(
                new GetUserDataRequest(),
                result =>
                {
                    try
                    {
                        foreach (var pair in result.Data)
                        {
                            if (!pair.Key.StartsWith(CloudSavePrefix))
                                continue;

                            string encoded = pair.Key.Substring(
                                CloudSavePrefix.Length
                            );

                            string[] parts = encoded.Split('.');

                            if (parts.Length != 2)
                                continue;

                            string playthroughId = DecodeKeyPart(parts[0]);
                            string slotName = DecodeKeyPart(parts[1]);

                            var slot = new SaveSlotId(
                                playthroughId,
                                slotName
                            );

                            GameData cloudData =
                                JsonUtility.FromJson<GameData>(pair.Value.Value);

                            if (cloudData == null)
                            {
                                Debug.LogWarning(
                                    $"Invalid cloud save for {slot}."
                                );
                                continue;
                            }

                            // Cloud save overwrites the matching local slot.
                            fileDataHandler.Save(slot, cloudData);

                            Debug.Log($"Restored cloud save: {slot}");
                        }

                        cloudSyncReady = true;
                        onComplete?.Invoke(true);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Cloud restore failed: {e}");
                        onComplete?.Invoke(false);
                    }
                },
                error =>
                {
                    Debug.LogError(
                        $"Could not load cloud saves: {error.GenerateErrorReport()}"
                    );

                    // Do not upload local data if cloud loading failed.
                    onComplete?.Invoke(false);
                }
            );
        }

        // ── Lifecycle ────────────────────────────────────────────────
        protected override void Awake()
        {
            base.Awake();
        }

        public void OnEnable()
        {
            string rootPath = System.IO.Path.Combine(Application.persistentDataPath, savesFolderName);
            fileDataHandler = new FileDataHandler(rootPath);
            dataPersistenceList = FindAllDataPersistences();
            userDataPersistenceList = FindAllUserDataPersistences();
            sessionStartTime = Time.realtimeSinceStartup;
        }

        // ── Load ─────────────────────────────────────────────────────

        /// <summary>Loads a specific slot into memory and notifies all persistence objects.</summary>
        public void LoadGame(SaveSlotId slot)
        {
            Debug.Log($"Loading {slot}...");

            gameData = fileDataHandler.Load(slot);

            if (gameData == null)
            {
                Debug.LogWarning($"No save found for {slot}.");
                return;
            }

            activeSlot = slot;

            foreach (var dp in dataPersistenceList)
                dp.LoadGame(gameData);

            Debug.Log($"Loaded {slot} successfully.");
            OnGameLoaded?.Invoke(slot);
        }

        private string currentPlayerId;


        // playfab
        public void SetCurrentPlayer(string playFabId, string username)
        {
            if (string.IsNullOrWhiteSpace(playFabId))
            {
                Debug.LogError("Cannot set current player: PlayFabId is empty.");
                return;
            }

            currentPlayerId = playFabId;
            currentUsername = username;
        }

        public void LoadCurrentUserData()
        {
            if (string.IsNullOrWhiteSpace(currentPlayerId))
            {
                Debug.LogError("Cannot load user data: no current player.");
                return;
            }

            userData = fileDataHandler.LoadUserDataForPlayer(
                currentPlayerId,
                currentUsername
            ) ?? new UserData();

            foreach (var udp in userDataPersistenceList)
                udp.LoadUserData(userData);

            Debug.Log($"User data loaded for player {currentPlayerId}.");
            OnUserDataLoaded?.Invoke();
        }

        public void SaveCurrentUserData()
        {
            if (string.IsNullOrWhiteSpace(currentPlayerId))
            {
                Debug.LogError("Cannot save user data: no current player.");
                return;
            }

            userData ??= new UserData();

            foreach (var udp in userDataPersistenceList)
                udp.SaveUserData(ref userData);

            fileDataHandler.SaveUserDataForPlayer(currentPlayerId, userData);

            Debug.Log($"User data saved for player {currentPlayerId}.");
            OnUserDataSaved?.Invoke();
        }


        private void UploadGameSaveToCloud(
            SaveSlotId slot,
            GameData data)
        {
            if (!cloudSyncReady || !AuthSession.IsAuthenticated)
                return;

            string json = JsonUtility.ToJson(data);

            // Player Data has a per-value size limit.
            // Reject oversized data instead of silently truncating it.
            int byteCount = Encoding.UTF8.GetByteCount(json);

            if (byteCount > 10 * 1024)
            {
                Debug.LogError(
                    $"Save {slot} is too large for this Player Data approach: "
                    + $"{byteCount} bytes. Use a suitable cloud file/object storage."
                );
                return;
            }

            string key = GetCloudSaveKey(slot);

            PlayFabClientAPI.UpdateUserData(
                new UpdateUserDataRequest
                {
                    Data = new Dictionary<string, string>
                    {
                        { key, json }
                    }
                },
                result =>
                {
                    Debug.Log($"Cloud save uploaded: {slot}");
                },
                error =>
                {
                    Debug.LogError(
                        $"Cloud upload failed: {error.GenerateErrorReport()}"
                    );
                }
            );
        }

        /// <summary>
        /// Loads the single global user/account data file (settings, profile, cross-save
        /// unlocks, etc). Not tied to any playthrough or slot. If no file exists yet
        /// (first launch), a fresh UserData is created in memory.
        /// </summary>
        public void LoadUserData(string username)
        {
            userData = fileDataHandler.LoadUserData(username) ?? new UserData();

            foreach (var udp in userDataPersistenceList)
                udp.LoadUserData(userData);

            Debug.Log($"User data loaded for {username}.");
            OnUserDataLoaded?.Invoke();
        }

        // ── Save ─────────────────────────────────────────────────────

        /// <summary>
        /// Saves to the given slot and makes it the active slot.
        /// If no game data exists yet in memory (e.g. starting a fresh playthrough),
        /// a new GameData is created automatically.
        /// </summary>
        public void SaveGame(SaveSlotId slot)
        {
            gameData ??= new GameData();

            Debug.Log($"Saving to {slot}...");

            foreach (var dp in dataPersistenceList)
                dp.SaveGame(ref gameData);

            gameData.lastSavedAt = DateTime.Now.ToString("o");
            gameData.totalPlayTime += Time.realtimeSinceStartup - sessionStartTime;
            sessionStartTime = Time.realtimeSinceStartup;

            fileDataHandler.Save(slot, gameData);
            activeSlot = slot;
            UploadGameSaveToCloud(slot, gameData);

            Debug.Log($"Saved to {slot} successfully.");
            OnGameSaved?.Invoke(slot);
        }

        /// <summary>
        /// Saves the global user/account data file. Call this independently of
        /// SaveGame — e.g. right after a settings change or on app pause/quit —
        /// not just when a playthrough is saved.
        /// </summary>
        public void SaveUserData(string username)
        {
            userData ??= new UserData();

            foreach (var udp in userDataPersistenceList)
                udp.SaveUserData(ref userData);

            fileDataHandler.SaveUserData(username, userData);

            Debug.Log($"User data saved for {username}.");
            OnUserDataSaved?.Invoke();
        }

        // ── Delete ───────────────────────────────────────────────────

        public void DeleteSave(SaveSlotId slot)
        {
            fileDataHandler.Delete(slot);

            if (activeSlot.HasValue && activeSlot.Value.Equals(slot))
                activeSlot = null;

            Debug.Log($"Deleted save {slot}.");
            OnSaveDeleted?.Invoke(slot);
        }

        public void DeletePlaythrough(string playthroughId)
        {
            fileDataHandler.DeletePlaythrough(playthroughId);

            if (activeSlot.HasValue && activeSlot.Value.playthroughId == playthroughId)
                activeSlot = null;

            Debug.Log($"Deleted playthrough {playthroughId}.");
            OnPlaythroughDeleted?.Invoke(playthroughId);
        }

        // ── Query ────────────────────────────────────────────────────

        public string[] GetAllSlots(string playthroughId) =>
            fileDataHandler.GetAllSlots(playthroughId);

        public string[] GetAllPlaythroughs() =>
            fileDataHandler.GetAllPlaythroughs();

        public bool SaveExists(SaveSlotId slot) =>
            fileDataHandler.SaveExists(slot);

        public SaveSlotId? ActiveSlot => activeSlot;
        public UserData UserData => userData;

        // ── Internal ─────────────────────────────────────────────────

        private List<IDataPersistence> FindAllDataPersistences()
        {
            return FindObjectsByType<MonoBehaviour>()
                .OfType<IDataPersistence>()
                .ToList();
        }

        private List<IUserDataPersistence> FindAllUserDataPersistences()
        {
            return FindObjectsByType<MonoBehaviour>()
                .OfType<IUserDataPersistence>()
                .ToList();
        }

        [Obsolete]
        public void FindAllDataSaver()
        {
            dataPersistenceList = FindAllDataPersistences();
        }
    }
}