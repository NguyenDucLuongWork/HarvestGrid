using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HarvestGrid.UI.Settings
{
    public class SettingPanel : MonoBehaviour
    {
        [Header("Graphics Settings")]
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private TMP_Dropdown qualityDropdown;
        
        [Header("General UI")]
        [SerializeField] private Button closeButton;

        private void Start()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(ClosePanel);
            }
        }

        private void OnEnable()
        {
            // Sync UI state to runtime values securely when panel opens
            if (HarvestGrid.Settings.GraphicsSettingsManager.Instance != null)
            {
                if (fullscreenToggle != null)
                {
                    fullscreenToggle.isOn = HarvestGrid.Settings.GraphicsSettingsManager.Instance.IsFullscreen;
                }

                if (qualityDropdown != null)
                {
                    qualityDropdown.value = HarvestGrid.Settings.GraphicsSettingsManager.Instance.QualityLevel;
                }
            }

            // Register UI events
            if (fullscreenToggle != null)
            {
                fullscreenToggle.onValueChanged.RemoveAllListeners();
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            }

            if (qualityDropdown != null)
            {
                qualityDropdown.onValueChanged.RemoveAllListeners();
                qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
            }
        }

        private void OnDisable()
        {
            // Cleanup memory/events
            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.RemoveAllListeners();
            if (qualityDropdown != null)
                qualityDropdown.onValueChanged.RemoveAllListeners();
        }

        private void OnFullscreenChanged(bool isOn)
        {
            if (HarvestGrid.Settings.GraphicsSettingsManager.Instance != null)
            {
                HarvestGrid.Settings.GraphicsSettingsManager.Instance.SetFullscreen(isOn);
            }
        }

        private void OnQualityChanged(int value)
        {
            if (HarvestGrid.Settings.GraphicsSettingsManager.Instance != null)
            {
                HarvestGrid.Settings.GraphicsSettingsManager.Instance.SetQualityLevel(value);
            }
        }

        public void ClosePanel()
        {
            gameObject.SetActive(false);
        }
    }
}
