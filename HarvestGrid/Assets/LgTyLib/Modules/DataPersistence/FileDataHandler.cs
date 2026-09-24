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

        public bool UserDataExists() => File.Exists(GetUserDataPath());

        public UserData LoadUserData() => LoadFile<UserData>(GetUserDataPath());

        public void SaveUserData(UserData userData) => SaveFile(GetUserDataPath(), userData);
    }
}