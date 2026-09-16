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
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI coinsText;

        [Header("Buttons")]
        [SerializeField] private Button logoutButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button backgroundClickArea; // For click-outside

        // Cache GameData fields here because OnEnable might run after LoadGame
        private int currentLevel = 1;
        private int currentCoins = 0;

        private void Start()
        {
            if (logoutButton != null)
                logoutButton.onClick.AddListener(OnLogoutClicked);

            if (closeButton != null)
                closeButton.onClick.AddListener(ClosePopup);

            if (backgroundClickArea != null)
                backgroundClickArea.onClick.AddListener(ClosePopup);
        }

        private void OnDestroy()
        {
            if (logoutButton != null)
                logoutButton.onClick.RemoveListener(OnLogoutClicked);

            if (closeButton != null)
                closeButton.onClick.RemoveListener(ClosePopup);

            if (backgroundClickArea != null)
                backgroundClickArea.onClick.RemoveListener(ClosePopup);
        }

        private void OnEnable()
        {
            // Populate dynamic text fields every time the popup opens
            if (usernameText != null)
                usernameText.text = string.IsNullOrEmpty(AuthSession.Username)
                    ? "Guest Farmer"
                    : AuthSession.Username;

            if (userIdText != null)
                userIdText.text = string.IsNullOrEmpty(AuthSession.UserId) ? "ID: N/A" : $"ID: {AuthSession.UserId}";

            UpdateStatsUI();
        }

        // --- IDataPersistence Implementation ---
        public void LoadGame(GameData gameData)
        {
            // Cache values from GameData
            currentLevel = gameData.level;
            currentCoins = 0; // Coins not in GameData currently

            UpdateStatsUI();
        }

        public void SaveGame(ref GameData gameData)
        {
            // Read-only UI, nothing to save
        }

        private void UpdateStatsUI()
        {
            if (levelText != null)
                levelText.text = currentLevel.ToString();

            if (coinsText != null)
            {
                coinsText.text = currentCoins.ToString();
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
