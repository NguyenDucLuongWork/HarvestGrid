using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HarvestGrid.UI.UserProfile
{
    public class UserProfileButton : MonoBehaviour
    {
        [SerializeField] private Image avatarImage;
        [SerializeField] private TextMeshProUGUI usernameText;
        [SerializeField] private Button profileButton;
        [SerializeField] private UserProfilePopup profilePopup;

        private void Start()
        {
            if (profileButton != null)
            {
                profileButton.onClick.AddListener(OnProfileButtonClicked);
            }

            UpdateUI();
        }

        private void UpdateUI()
        {
            if (usernameText != null)
            {
                usernameText.text = string.IsNullOrEmpty(AuthSession.Username)
                    ? "Guest"
                    : AuthSession.Username;
            }
        }

        private void OnProfileButtonClicked()
        {
            if (profilePopup != null)
            {
                // Toggle popup visibility
                bool isActive = profilePopup.gameObject.activeSelf;
                profilePopup.gameObject.SetActive(!isActive);
            }
            else
            {
                Debug.LogWarning("[UserProfileButton] UserProfilePopup reference is missing!");
            }
        }
    }
}
