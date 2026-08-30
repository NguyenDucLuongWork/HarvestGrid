
namespace LgTyLib.Modules.Settings
{
    public interface ISettingGroup
    {
        string GroupKey { get; }
        void Load(SettingsSaveHandler handler);
        void Save(SettingsSaveHandler handler);
        void ResetToDefault(SettingsSaveHandler handler);
    }
}
