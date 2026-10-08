using LgTyLib.Core;
using LgTyLib.Modules.ObjectPooling;
using LgTyLib.Modules.Settings;
using UnityEngine;
using UnityEngine.Audio;

namespace LgTyLib.Modules.Audio
{
    public class AudioManager : BaseSingleton<AudioManager>, ISettingGroup
    {
        [Header("Config")]
        [field: SerializeField]
        public AudioSettingsDataSO audioSettingsDataSO { get; private set; }
        [field: SerializeField]
        public AudioLibSO audioLibSO { get; private set; }

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


        public void PlaySoundFXClip(AudioClip audioClip, Vector3 position, float volume, bool loop = false)
        {
            if (audioClip == null)
            {
                Debug.LogWarning("Null audio clip");
                return;
            }
            AudioSource audioSource = Instantiate(soundObject, position, Quaternion.identity);
            audioSource.clip = audioClip;
            audioSource.volume = volume;
            audioSource.loop = loop;
            audioSource.Play(); 

            float clipLength = audioSource.clip.length;

            if (loop)
            {
                return;
            }
            Destroy(audioSource.gameObject, clipLength);
        }

        public void PlaySoundFXClipWithPool(AudioClip audioClip, Vector3 position, float volume)
        {
            if(audioClip == null)
            {
                Debug.LogWarning("Null audio clip");
                return;
            }
            AudioSource audioSource = ObjectPoolManager.Instance.SpawnObject<AudioSource>(
                soundObject,
                position,
                PoolGroup.SoundFX
            );

            audioSource.clip = audioClip;
            audioSource.volume = volume;
            audioSource.Play();

            ObjectPoolManager.Instance.ReturnObjectToPool(
                audioSource.gameObject,
                audioClip.length
                );

        }

        public void UpdateMasterVolume(float masterAudioScale)
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

        public float GetMusicVolume() => audioSettingsDataSO != null ? audioSettingsDataSO.audioSettingsData.musicAudioScale : 0.5f;
        public float GetSFXVolume() => audioSettingsDataSO != null ? audioSettingsDataSO.audioSettingsData.sfxAudioScale : 0.5f;

        public float AudioScaleRange01ToVolume(float slider)
        {
            if (slider <= 0.001f) return -80f; // Mute
            
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