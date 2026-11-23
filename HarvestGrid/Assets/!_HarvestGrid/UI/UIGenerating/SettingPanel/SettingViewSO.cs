using System;
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
    public float sens = 0.5f;

    public float masterAudioScale = 0.5f;

    public float musicAudioScale = 0.5f;

    public float sfxAudioScale = 0.5f;
}
