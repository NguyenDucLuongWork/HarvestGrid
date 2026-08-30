using UnityEngine;

namespace LgTyLib.Modules.Settings
{
    public class SettingsSaveHandler
    {
        // -- String
        public void SetString(string groupKey, string key, string value)
            => PlayerPrefs.SetString(BuildKey(groupKey, key), value);

        public string GetString(string groupKey, string key, string defaultValue = "")
            => PlayerPrefs.GetString(BuildKey(groupKey, key), defaultValue);

        // -- Int
        public void SetInt(string groupKey, string key, int value)
            => PlayerPrefs.SetInt(BuildKey(groupKey, key), value);

        public int GetInt(string groupKey, string key, int defaultValue = 0)
            => PlayerPrefs.GetInt(BuildKey(groupKey, key), defaultValue);

        // -- Float
        public void SetFloat(string groupKey, string key, float value)
            => PlayerPrefs.SetFloat(BuildKey(groupKey, key), value);

        public float GetFloat(string groupKey, string key, float defaultValue = 0f)
            => PlayerPrefs.GetFloat(BuildKey(groupKey, key), defaultValue);

        // -- Bool
        public void SetBool(string groupKey, string key, bool value)
            => PlayerPrefs.SetInt(BuildKey(groupKey, key), value ? 1 : 0);

        public bool GetBool(string groupKey, string key, bool defaultValue = false)
            => PlayerPrefs.GetInt(BuildKey(groupKey, key), defaultValue ? 1 : 0) == 1;

        // -- Core
        public void Save() => PlayerPrefs.Save();

        public bool HasKey(string groupKey, string key)
            => PlayerPrefs.HasKey(BuildKey(groupKey, key));

        public void DeleteKey(string groupKey, string key)
            => PlayerPrefs.DeleteKey(BuildKey(groupKey, key));

        private string BuildKey(string groupKey, string key)
            => $"{groupKey}_{key}";
    }
}