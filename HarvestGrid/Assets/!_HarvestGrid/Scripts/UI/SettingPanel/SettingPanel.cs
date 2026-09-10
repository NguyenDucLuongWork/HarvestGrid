using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LgTyLib.Modules.Audio;
using LgTyLib.Modules.Settings;

namespace HarvestGrid.UI.Settings
{
    public class SettingPanel : MonoBehaviour
    {
        [Header("Audio Settings")]
        [SerializeField] private Slider musicSlider;
        [SerializeField] private TextMeshProUGUI musicPercentageText;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private TextMeshProUGUI sfxPercentageText;

        [Header("Graphics Settings")]
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private TMP_Dropdown qualityDropdown;
        
        [Header("General UI")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button resetButton;

        private void Start()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(ClosePanel);

            if (resetButton != null)
                resetButton.onClick.AddListener(OnResetClicked);
        }

        private void OnEnable()
        {
            // Sync UI state to runtime values securely when panel opens
            if (HarvestGrid.Settings.GraphicsSettingsManager.Instance != null)
            {
                if (fullscreenToggle != null)
                    fullscreenToggle.isOn = HarvestGrid.Settings.GraphicsSettingsManager.Instance.IsFullscreen;

                if (qualityDropdown != null)
                    qualityDropdown.value = HarvestGrid.Settings.GraphicsSettingsManager.Instance.QualityLevel;
            }

            if (AudioManager.Instance != null)
            {
                if (musicSlider != null)
                {
                    musicSlider.value = AudioManager.Instance.GetMusicVolume();
                    UpdatePercentageText(musicPercentageText, musicSlider.value);
                }

                if (sfxSlider != null)
                {
                    sfxSlider.value = AudioManager.Instance.GetSFXVolume();
                    UpdatePercentageText(sfxPercentageText, sfxSlider.value);
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

            if (musicSlider != null)
            {
                musicSlider.onValueChanged.RemoveAllListeners();
                musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.RemoveAllListeners();
                sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }
        }

        private void OnDisable()
        {
            // Cleanup memory/events
            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.RemoveAllListeners();
            if (qualityDropdown != null)
                qualityDropdown.onValueChanged.RemoveAllListeners();
            if (musicSlider != null)
                musicSlider.onValueChanged.RemoveAllListeners();
            if (sfxSlider != null)
                sfxSlider.onValueChanged.RemoveAllListeners();
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

        private void OnMusicVolumeChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.UpdateMusicVolume(value);
                UpdatePercentageText(musicPercentageText, value);
                SettingsManager.Instance.SaveAll(); // Tự động lưu
            }
        }

        private void OnSFXVolumeChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.UpdateSFXVolume(value);
                UpdatePercentageText(sfxPercentageText, value);
                SettingsManager.Instance.SaveAll(); // Tự động lưu
            }
        }

        private void UpdatePercentageText(TextMeshProUGUI textElement, float value)
        {
            if (textElement != null)
            {
                int percentage = Mathf.RoundToInt(value * 100f);
                textElement.text = $"{percentage}%";
            }
        }

        private void OnResetClicked()
        {
            // Reset everything via SettingsManager
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.ResetAll();

                // Re-sync UI manually since ResetAll() doesn't fire events directly back to this script
                // We just call OnDisable and OnEnable logic again to refresh UI
                OnDisable();
                OnEnable();
            }
        }

        public void ClosePanel()
        {
            gameObject.SetActive(false);
        }
    }
}
