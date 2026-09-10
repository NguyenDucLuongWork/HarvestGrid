using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LgTyLib.Modules.DataPersistence;
using HarvestGrid.Managers;
using System;

namespace HarvestGrid.UI.UserProfile
{
    public class UserProfilePopup : MonoBehaviour, IDataPersistence
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI usernameText;
        [SerializeField] private TextMeshProUGUI userIdText;
        [SerializeField] private TextMeshProUGUI scoreText; 
        [SerializeField] private TextMeshProUGUI totalPlayTimeText;
        
        [Header("Buttons")]
        [SerializeField] private Button logoutButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button backgroundClickArea; // For click-outside

        // Cache GameData fields here because OnEnable might run after LoadGame
        private float currentScore = 0f;
        private float currentPlayTime = 0f;

        private void Start()
        {
            if (logoutButton != null)
                logoutButton.onClick.AddListener(OnLogoutClicked);
                
            if (closeButton != null)
                closeButton.onClick.AddListener(ClosePopup);
                
            if (backgroundClickArea != null)
                backgroundClickArea.onClick.AddListener(ClosePopup);
        }

        private void OnEnable()
        {
            // Populate dynamic text fields every time the popup opens
            if (usernameText != null)
                usernameText.text = string.IsNullOrEmpty(AuthSession.Email) ? "Guest" : AuthSession.Email;
                
            if (userIdText != null)
                userIdText.text = string.IsNullOrEmpty(AuthSession.UserId) ? "ID: N/A" : $"ID: {AuthSession.UserId}";

            UpdateStatsUI();
        }

        // --- IDataPersistence Implementation ---
        public void LoadGame(GameData gameData)
        {
            // Cache values from GameData
            currentScore = gameData.score;
            currentPlayTime = gameData.totalPlayTime;

            UpdateStatsUI();
        }

        public void SaveGame(ref GameData gameData)
        {
            // Read-only UI, nothing to save
        }

        private void UpdateStatsUI()
        {
            if (scoreText != null)
                scoreText.text = $"Score: {currentScore}";
                
            if (totalPlayTimeText != null)
            {
                TimeSpan time = TimeSpan.FromSeconds(currentPlayTime);
                totalPlayTimeText.text = $"Playtime: {(int)time.TotalHours}h {time.Minutes}m";
            }
        }

        private void OnLogoutClicked()
        {
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.Logout();
            }
            else
            {
                Debug.LogError("[UserProfilePopup] AuthManager Instance not found! Ensure AuthManager is in the scene.");
            }
        }

        public void ClosePopup()
        {
            gameObject.SetActive(false);
        }
    }
}
