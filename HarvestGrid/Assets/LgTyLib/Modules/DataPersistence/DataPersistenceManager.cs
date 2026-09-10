using LgTyLib.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LgTyLib.Modules.DataPersistence
{
    public class DataPersistenceManager : BaseSingleton<DataPersistenceManager>
    {
        [Header("File Storage Config")]
        [SerializeField] private string savesFolderName = "Saves";

        private FileDataHandler fileDataHandler;
        private GameData gameData;
        private List<IDataPersistence> dataPersistenceList;
        private SaveSlotId? activeSlot;
        private float sessionStartTime;

        // ── Events ───────────────────────────────────────────────────
        public static event Action<SaveSlotId> OnGameLoaded;
        public static event Action<SaveSlotId> OnGameSaved;
        public static event Action<SaveSlotId> OnSaveDeleted;
        public static event Action<string> OnPlaythroughDeleted;

        // ── Lifecycle ────────────────────────────────────────────────
        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            string rootPath = System.IO.Path.Combine(Application.persistentDataPath, savesFolderName);
            fileDataHandler = new FileDataHandler(rootPath);
            dataPersistenceList = FindAllDataPersistences();
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

            Debug.Log($"Saved to {slot} successfully.");
            OnGameSaved?.Invoke(slot);
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

        // ── Internal ─────────────────────────────────────────────────

        private List<IDataPersistence> FindAllDataPersistences()
        {
            return FindObjectsByType<MonoBehaviour>()
                .OfType<IDataPersistence>()
                .ToList();
        }

        [Obsolete]
        public void FindAllDataSaver()
        {
            dataPersistenceList = FindAllDataPersistences();

        }
    }
}