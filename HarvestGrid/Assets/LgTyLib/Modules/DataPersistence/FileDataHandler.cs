using System;
using System.IO;
using UnityEngine;

namespace LgTyLib.Modules.DataPersistence
{
    public class FileDataHandler
    {
        private readonly string rootPath;

        public FileDataHandler(string rootPath)
        {
            this.rootPath = rootPath;
        }

        private string GetFullPath(SaveSlotId slot)
        {
            string slotFile = slot.slotName.EndsWith(".json")
                ? slot.slotName
                : slot.slotName + ".json";

            return Path.Combine(rootPath, slot.playthroughId, slotFile);
        }

        public bool SaveExists(SaveSlotId slot) => File.Exists(GetFullPath(slot));

        public GameData Load(SaveSlotId slot)
        {
            string fullPath = GetFullPath(slot);

            if (!File.Exists(fullPath))
                return null;

            try
            {
                string dataToLoad;
                using (var stream = new FileStream(fullPath, FileMode.Open))
                using (var reader = new StreamReader(stream))
                    dataToLoad = reader.ReadToEnd();

                return JsonUtility.FromJson<GameData>(dataToLoad);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading from {fullPath}\n{e}");
                return null;
            }
        }

        public void Save(SaveSlotId slot, GameData gameData)
        {
            string fullPath = GetFullPath(slot);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

                string dataToStore = JsonUtility.ToJson(gameData, prettyPrint: true);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                using (var writer = new StreamWriter(stream))
                    writer.Write(dataToStore);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving to {fullPath}\n{e}");
            }
        }

        public void Delete(SaveSlotId slot)
        {
            string fullPath = GetFullPath(slot);

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

        /// <summary>Returns all slot names within a playthrough folder.</summary>
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

        /// <summary>Returns all playthrough folder names.</summary>
        public string[] GetAllPlaythroughs()
        {
            if (!Directory.Exists(rootPath))
                return Array.Empty<string>();

            var dirs = Directory.GetDirectories(rootPath);

            for (int i = 0; i < dirs.Length; i++)
                dirs[i] = Path.GetFileName(dirs[i]);

            return dirs;
        }

        /// <summary>Deletes an entire playthrough folder and all its saves.</summary>
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
    }
}