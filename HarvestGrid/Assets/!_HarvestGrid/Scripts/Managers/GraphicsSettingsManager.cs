using LgTyLib.Core;
using LgTyLib.Modules.Settings;
using UnityEngine;

namespace HarvestGrid.Settings
{
    public class GraphicsSettingsManager : BaseSingleton<GraphicsSettingsManager>, ISettingGroup
    {
        public string GroupKey => "Graphics";

        public bool IsFullscreen { get; private set; }
        public int QualityLevel { get; private set; }

        public void SetFullscreen(bool isFullscreen)
        {
            IsFullscreen = isFullscreen;
            Screen.fullScreen = isFullscreen;
            
            // Auto-save setting locally when changed
            SettingsManager.Instance.SaveAll();
        }

        public void SetQualityLevel(int level)
        {
            QualityLevel = level;
            QualitySettings.SetQualityLevel(level);
            
            // Auto-save setting locally when changed
            SettingsManager.Instance.SaveAll();
        }

        public void Load(SettingsSaveHandler handler)
        {
            IsFullscreen = handler.GetBool(GroupKey, "Fullscreen", true);
            QualityLevel = handler.GetInt(GroupKey, "Quality", QualitySettings.names.Length - 1);
            
            ApplySettings();
        }

        public void Save(SettingsSaveHandler handler)
        {
            handler.SetBool(GroupKey, "Fullscreen", IsFullscreen);
            handler.SetInt(GroupKey, "Quality", QualityLevel);
        }

        public void ResetToDefault(SettingsSaveHandler handler)
        {
            IsFullscreen = true;
            QualityLevel = QualitySettings.names.Length - 1; // Default to highest quality
            
            ApplySettings();
        }

        private void ApplySettings()
        {
            Screen.fullScreen = IsFullscreen;
            QualitySettings.SetQualityLevel(QualityLevel);
        }
    }
}
