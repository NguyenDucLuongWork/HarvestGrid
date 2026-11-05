using System;
using UIGen;
using UnityEngine;
using LgTyLib.Modules.Audio;

[CreateAssetMenu(fileName = "SettingViewSO", 
    menuName = "Scriptable Objects/View/SettingViewSO")]
public class SettingViewSO : ScriptableObject
{
    [SerializeField]
    public SettingView setting = new SettingView();

    public void LoadFromSystem()
    {
        if (AudioManager.HasInstance)
        {
            setting.musicAudioScale = AudioManager.Instance.GetMusicVolume();
            setting.sfxAudioScale = AudioManager.Instance.GetSFXVolume();
        }
    }

    public void ApplyToSystem()
    {
        if (AudioManager.HasInstance)
        {
            AudioManager.Instance.UpdateMasterVolume(setting.masterAudioScale);
            AudioManager.Instance.UpdateMusicVolume(setting.musicAudioScale);
            AudioManager.Instance.UpdateSFXVolume(setting.sfxAudioScale);
        }
    }

    public void ResetDefaults()
    {
        setting.sens = 0.5f;
        setting.masterAudioScale = 0.5f;
        setting.musicAudioScale = 0.5f;
        setting.sfxAudioScale = 0.5f;
        ApplyToSystem();
    }
}

[Serializable]
public class SettingView
{
    [UIField(ControlType.Slider, Min = 0f, Max = 1f, Label = "👆 Độ nhạy")]
    public float sens = 0.5f;

    [UIField(ControlType.Slider, Min = 0f, Max = 1f, Label = "🔊 Master")]
    public float masterAudioScale = 0.5f;

    [UIField(ControlType.Slider, Min = 0f, Max = 1f, Label = "🎵 Music")]
    public float musicAudioScale = 0.5f;

    [UIField(ControlType.Slider, Min = 0f, Max = 1f, Label = "🔔 SFX")]
    public float sfxAudioScale = 0.5f;
}
