using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LgTyLib.Core;

namespace LgTyLib.Modules.Settings
{
    public class SettingsManager : BaseSingleton<SettingsManager>
    {
        private List<ISettingGroup> _groups = new List<ISettingGroup>();
        private SettingsSaveHandler _saveHandler = new SettingsSaveHandler();

        protected override void Awake()
        {
            base.Awake();
            RefreshGroups();
        }

        public void RefreshGroups()
        {
            _groups = new List<ISettingGroup>(
                FindObjectsByType<MonoBehaviour>()
                    .OfType<ISettingGroup>()
            );

            if (_groups.Count == 0)
                Debug.LogWarning("[SettingsManager] No ISettingGroup implementors found in scene.");
        }

        public void LoadAll()
        {
            foreach (var group in _groups)
                group.Load(_saveHandler);
        }

        public void SaveAll()
        {
            foreach (var group in _groups)
                group.Save(_saveHandler);

            _saveHandler.Save();
        }

        public void ResetAll()
        {
            foreach (var group in _groups)
                group.ResetToDefault(_saveHandler);

            _saveHandler.Save();
        }

        public void ResetGroup(string groupKey)
        {
            var group = _groups.Find(g => g.GroupKey == groupKey);
            if (group == null)
            {
                Debug.LogWarning($"[SettingsManager] No group found with key: {groupKey}");
                return;
            }
            group.ResetToDefault(_saveHandler);
            _saveHandler.Save();
        }
    }
}