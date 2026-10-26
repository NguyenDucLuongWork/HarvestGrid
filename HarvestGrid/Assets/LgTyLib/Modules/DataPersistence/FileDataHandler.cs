using System;
using System.IO;
using UnityEngine;

namespace LgTyLib.Modules.DataPersistence
{
    public class FileDataHandler
    {
        private readonly string rootPath;
        private const string UserDataFileName = "user.json";

        public FileDataHandler(string rootPath)
        {
            this.rootPath = rootPath;
        }

        private string GetSlotPath(SaveSlotId slot)
        {
            string slotFile = slot.slotName.EndsWith(".json")
                ? slot.slotName
                : slot.slotName + ".json";

            return Path.Combine(rootPath, slot.playthroughId, slotFile);
        }

        private string GetUserDataPath() => Path.Combine(rootPath, UserDataFileName);

        // ── Generic file I/O (shared by per-slot saves and global user data) ──

        private T LoadFile<T>(string fullPath) where T : class
        {
            if (!File.Exists(fullPath))
                return null;

            try
            {
                string json;
                using (var stream = new FileStream(fullPath, FileMode.Open))
                using (var reader = new StreamReader(stream))
                    json = reader.ReadToEnd();

                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading from {fullPath}\n{e}");
                return null;
            }
        }

        private void SaveFile<T>(string fullPath, T data) where T : class
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

                string json = JsonUtility.ToJson(data, prettyPrint: true);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                using (var writer = new StreamWriter(stream))
                    writer.Write(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving to {fullPath}\n{e}");
            }
        }

        // ── Per-slot gameplay saves ─────────────────────────────────

        public bool SaveExists(SaveSlotId slot) => File.Exists(GetSlotPath(slot));

        public GameData Load(SaveSlotId slot) => LoadFile<GameData>(GetSlotPath(slot));

        public void Save(SaveSlotId slot, GameData gameData) => SaveFile(GetSlotPath(slot), gameData);

        public void Delete(SaveSlotId slot)
        {
            string fullPath = GetSlotPath(slot);

            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"No save file found at: {fullPath}");
                return;
            }

            try
            {
                File.Delete(fullPath);
                Debug.Log($"Deleted save: {fullPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error deleting {fullPath}\n{e}");
            }
        }

        public string[] GetAllSlots(string playthroughId)
        {
            string dir = Path.Combine(rootPath, playthroughId);

            if (!Directory.Exists(dir))
                return Array.Empty<string>();

            var files = Directory.GetFiles(dir, "*.json");

            for (int i = 0; i < files.Length; i++)
                files[i] = Path.GetFileNameWithoutExtension(files[i]);

            return files;
        }

        public string[] GetAllPlaythroughs()
        {
            if (!Directory.Exists(rootPath))
                return Array.Empty<string>();

            var dirs = Directory.GetDirectories(rootPath);

            for (int i = 0; i < dirs.Length; i++)
                dirs[i] = Path.GetFileName(dirs[i]);

            return dirs;
        }

        public void DeletePlaythrough(string playthroughId)
        {
            string dir = Path.Combine(rootPath, playthroughId);

            if (!Directory.Exists(dir))
            {
                Debug.LogWarning($"No playthrough folder found: {dir}");
                return;
            }

            try
            {
                Directory.Delete(dir, recursive: true);
                Debug.Log($"Deleted playthrough: {playthroughId}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error deleting playthrough {dir}\n{e}");
            }
        }

        // ── Global user/account data (one file, independent of any playthrough) ──

        private string GetUserDataPath(string username) => Path.Combine(rootPath, "Users", username + ".json");

        public bool UserDataExists(string username) => File.Exists(GetUserDataPath(username));

        public UserData LoadUserData(string username)
        {
            string path = GetUserDataPath(username);
            if (File.Exists(path))
            {
                return LoadFile<UserData>(path);
            }

            // Migration: Check if there's an old GameData save for this username
            string oldGameDataPath = GetSlotPath(new SaveSlotId("Players", username));
            if (File.Exists(oldGameDataPath))
            {
                try
                {
                    string json = File.ReadAllText(oldGameDataPath);
                    // Extract playerId and username using simple string parsing or JSON node (if available)
                    // Unity's JsonUtility doesn't have a DOM, we can use a temporary struct
                    var oldData = JsonUtility.FromJson<OldGameDataProxy>(json);
                    if (!string.IsNullOrEmpty(oldData.playerId))
                    {
                        Debug.Log($"[Migration] Migrating UserData for {username} from old GameData.");
                        var newUserData = new UserData();
                        newUserData.userId = oldData.playerId;
                        newUserData.username = oldData.username;
                        // Save the migrated UserData immediately
                        SaveUserData(username, newUserData);
                        return newUserData;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error migrating UserData for {username}\n{e}");
                }
            }

            return null;
        }

        public void SaveUserData(string username, UserData userData) => SaveFile(GetUserDataPath(username), userData);
        
/// <summary>
/// Loads account data using PlayFabId.
/// If no PlayFabId file exists, attempts to migrate legacy username data.
/// </summary>
public UserData LoadUserDataForPlayer(string playFabId, string username)
{
    if (string.IsNullOrWhiteSpace(playFabId))
    {
        Debug.LogError("PlayFabId is empty.");
        return null;
    }

    // 1. Prefer data already stored under PlayFabId.
    string playerPath = GetUserDataPath(playFabId);

    if (File.Exists(playerPath))
        return LoadFile<UserData>(playerPath);

    // 2. Migrate existing data saved under username.
    if (!string.IsNullOrWhiteSpace(username))
    {
        string legacyPath = GetUserDataPath(username);

        if (File.Exists(legacyPath))
        {
            UserData legacyData = LoadFile<UserData>(legacyPath);

            if (legacyData != null)
            {
                legacyData.userId = playFabId;
                legacyData.username = username;

                SaveUserDataForPlayer(playFabId, legacyData);

                Debug.Log(
                    $"[Migration] Migrated user data from username to PlayFabId."
                );

                return legacyData;
            }
        }

        // 3. If no username file exists, retain the existing
        // legacy GameData migration logic.
        UserData oldData = LoadUserData(username);

        if (oldData != null)
        {
            oldData.userId = playFabId;
            oldData.username = username;

            SaveUserDataForPlayer(playFabId, oldData);
            return oldData;
        }
    }

    // First login on this device: create fresh account data.
    return null;
}

public void SaveUserDataForPlayer(string playFabId, UserData data)
{
    if (string.IsNullOrWhiteSpace(playFabId))
    {
        Debug.LogError("Cannot save user data: PlayFabId is empty.");
        return;
    }

    if (data == null)
    {
        Debug.LogError("Cannot save user data: data is null.");
        return;
    }

    data.userId = playFabId;
    SaveFile(GetUserDataPath(playFabId), data);
}


        [Serializable]
        private class OldGameDataProxy
        {
            public string playerId;
            public string username;
        }
    }
}