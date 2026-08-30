using LgTyLib.Core;
using LgTyLib.Modules.ObjectPooling;
using LgTyLib.Modules.Settings;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;

namespace LgTyLib.Modules.Audio
{
    public class AudioManager : BaseSingleton<AudioManager>, ISettingGroup
    {
        [Header("Config")]
        [SerializeField]
        private AudioSettingsDataSO audioSettingsDataSO;
        
        [SerializeField]
        private AudioMixer audioMixer;
        [SerializeField]
        private string masterVolumeField = "MasterVolume";
        [SerializeField]
        private string musicVolumeField = "MusicVolume";
        [SerializeField]
        private string sfxVolumeField = "SfxVolume";

        [SerializeField]
        private AudioSource soundObject;

        public string GroupKey => "AudioSettings";

        public void PlaySoundFXClip(AudioClip audioClip, Transform spawnTransform, float volume)
        {
            AudioSource audioSource = Instantiate(soundObject, spawnTransform.position, Quaternion.identity);
            audioSource.clip = audioClip;
            audioSource.volume = volume;

            audioSource.Play(); 

            float clipLength = audioSource.clip.length;

            Destroy(audioSource.gameObject, clipLength);
        }

        public void PlaySoundFXClipWithPool(AudioClip audioClip, Transform spawnTransform, float volume)
        {
            AudioSource audioSource = ObjectPoolManager.Instance.SpawnObject<AudioSource>(
                    soundObject,
                    transform.position,
                    ObjectPoolManager.PoolType.SoundFX
                );
            audioSource.clip = audioClip;
            audioSource.volume = volume;

            audioSource.Play();

            float clipLength = audioSource.clip.length;

            ObjectPoolManager.Instance.ReturnObjectToPool(
                audioSource.gameObject, 
                audioClip.length,
                ObjectPoolManager.PoolType.SoundFX
                );
        }

        public void UpdateMasterVolume( float masterAudioScale)
        {
            audioMixer.SetFloat(masterVolumeField, AudioScaleRange01ToVolume(masterAudioScale));
            audioSettingsDataSO.audioSettingsData.masterAudioScale = masterAudioScale;
        }

        public void UpdateMusicVolume(float musicAudioScale)
        {
            audioMixer.SetFloat(musicVolumeField, AudioScaleRange01ToVolume(musicAudioScale));
            audioSettingsDataSO.audioSettingsData.musicAudioScale = musicAudioScale;
        }

        public void UpdateSFXVolume(float sfxAudioScale)
        {
            audioMixer.SetFloat(sfxVolumeField, AudioScaleRange01ToVolume(sfxAudioScale));
            audioSettingsDataSO.audioSettingsData.sfxAudioScale = sfxAudioScale;
        }

        public float AudioScaleRange01ToVolume(float slider)
        {
            float scale = slider * 2f; // 0.5 -> 1
            scale = Mathf.Max(scale, 0.0001f);

            return Mathf.Log10(scale) * 20f;
        }

        public void ApplyAudioSettings()
        {
            audioMixer.SetFloat(masterVolumeField, 
                AudioScaleRange01ToVolume(audioSettingsDataSO.audioSettingsData.masterAudioScale));
            audioMixer.SetFloat(musicVolumeField, 
                AudioScaleRange01ToVolume(audioSettingsDataSO.audioSettingsData.musicAudioScale));
            audioMixer.SetFloat(sfxVolumeField, 
                AudioScaleRange01ToVolume(audioSettingsDataSO.audioSettingsData.sfxAudioScale));

        }

        public void Load(SettingsSaveHandler handler)
        {
            audioSettingsDataSO.audioSettingsData.masterAudioScale = handler.GetFloat(
                    GroupKey,
                    AudioSettingsData.masterAudioScaleSavingName,
                    0.5f
                );

            audioSettingsDataSO.audioSettingsData.musicAudioScale = handler.GetFloat(
                GroupKey,
                AudioSettingsData.musicAudioScaleSavingName,
                0.5f
            );

            audioSettingsDataSO.audioSettingsData.sfxAudioScale = handler.GetFloat(
                GroupKey,
                AudioSettingsData.sfxAudioScaleSavingName,
                0.5f
            );

            ApplyAudioSettings();
        }

        public void Save(SettingsSaveHandler handler)
        {
            handler.SetFloat(
                    GroupKey,
                    AudioSettingsData.masterAudioScaleSavingName,
                    audioSettingsDataSO.audioSettingsData.masterAudioScale
                );

            handler.SetFloat(
                GroupKey,
                AudioSettingsData.musicAudioScaleSavingName,
                audioSettingsDataSO.audioSettingsData.musicAudioScale
            );

            handler.SetFloat(
                GroupKey,
                AudioSettingsData.sfxAudioScaleSavingName,
                audioSettingsDataSO.audioSettingsData.sfxAudioScale
            );
        }

        public void ResetToDefaultSetting()
        {
            audioSettingsDataSO.audioSettingsData = new AudioSettingsData();
            ApplyAudioSettings();
        }

        public void ResetToDefault(SettingsSaveHandler handler)
        {
            ResetToDefaultSetting();
        }
    }
}