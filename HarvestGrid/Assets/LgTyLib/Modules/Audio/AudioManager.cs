using LgTyLib.Core;
using LgTyLib.Modules.ObjectPooling;
using LgTyLib.Modules.Settings;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

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

        [Header("BGM")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioClip bgm01;
        [SerializeField] private AudioClip bgm02;

        [Header("UI SFX")]
        [SerializeField] private AudioClip uiClickSound;
        [SerializeField] private AudioClip uiOpenSound;
        [SerializeField] private AudioClip uiCloseSound;
        [SerializeField] private AudioClip uiConfirmSound;

        [Header("Item SFX")]
        [SerializeField] private AudioClip sickleSound;
        [SerializeField] private AudioClip wateringSound;
        [SerializeField] private AudioClip fertilizerSound;

        private Coroutine bgmFadeCoroutine;

        public string GroupKey => "AudioSettings";

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (uiClickSound == null) uiClickSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/!_HarvestGrid/Audio/UI/UI_Click.wav");
            if (uiOpenSound == null) uiOpenSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/!_HarvestGrid/Audio/UI/UI_Open.wav");
            if (uiCloseSound == null) uiCloseSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/!_HarvestGrid/Audio/UI/UI_Close.wav");
            // uiConfirmSound can be mapped if user adds one later

            if (sickleSound == null) sickleSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/!_HarvestGrid/Audio/Items/SFX_Sickle.wav");
            if (wateringSound == null) wateringSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/!_HarvestGrid/Audio/Items/SFX_Watering.wav");
            if (fertilizerSound == null) fertilizerSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/!_HarvestGrid/Audio/Items/SFX_Fertilizer.wav");

            if (bgm01 == null) bgm01 = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/!_HarvestGrid/Audio/BGM/BGM_Valley.wav");
            if (bgm02 == null) bgm02 = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/!_HarvestGrid/Audio/BGM/BGM_Gaming.wav");
        }
#endif

        private void Start()
        {
            if (bgm01 != null)
            {
                PlayBGM(bgm01);
            }
        }

        private void Update()
        {
            DetectGlobalButtonClick();
        }

        private void DetectGlobalButtonClick()
        {
            if (EventSystem.current == null) return;
            
            // Check for mouse click or touch
            bool isClick = Input.GetMouseButtonDown(0);
            if (!isClick && Input.touchCount > 0)
            {
                isClick = Input.GetTouch(0).phase == TouchPhase.Began;
            }

            if (isClick)
            {
                PointerEventData pointerData = new PointerEventData(EventSystem.current)
                {
                    position = Input.mousePosition
                };

                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);

                foreach (var result in results)
                {
                    // Check if we hit a Button or something with IPointerClickHandler (like Toggle)
                    var button = result.gameObject.GetComponentInParent<Button>();
                    var toggle = result.gameObject.GetComponentInParent<Toggle>();
                    
                    if ((button != null && button.interactable) || (toggle != null && toggle.interactable))
                    {
                        PlayUIClick();
                        break; 
                    }
                }
            }
        }

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

        #region BGM Methods
        public void PlayBGM(AudioClip bgmClip, bool loop = true, float fadeDuration = 1f)
        {
            if (musicSource == null || bgmClip == null) return;
            
            if (bgmFadeCoroutine != null)
                StopCoroutine(bgmFadeCoroutine);

            if (musicSource.isPlaying)
            {
                bgmFadeCoroutine = StartCoroutine(FadeBGM(bgmClip, loop, fadeDuration));
            }
            else
            {
                musicSource.clip = bgmClip;
                musicSource.loop = loop;
                musicSource.volume = 1f; // Mixer handles the actual volume level
                musicSource.Play();
            }
        }

        public void StopBGM()
        {
            if (musicSource != null) musicSource.Stop();
        }

        private System.Collections.IEnumerator FadeBGM(AudioClip newClip, bool loop, float fadeDuration)
        {
            float startVolume = musicSource.volume;
            float timer = 0f;

            // Fade out
            while (timer < fadeDuration / 2)
            {
                timer += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, timer / (fadeDuration / 2));
                yield return null;
            }

            musicSource.Stop();
            musicSource.clip = newClip;
            musicSource.loop = loop;
            musicSource.Play();

            timer = 0f;
            // Fade in
            while (timer < fadeDuration / 2)
            {
                timer += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(0f, startVolume, timer / (fadeDuration / 2));
                yield return null;
            }
            musicSource.volume = startVolume;
            bgmFadeCoroutine = null;
        }
        #endregion

        #region UI SFX Methods
        public void PlayUIClick() => PlaySFX(uiClickSound);
        public void PlayUIOpen() => PlaySFX(uiOpenSound);
        public void PlayUIClose() => PlaySFX(uiCloseSound);
        public void PlayUIConfirm() => PlaySFX(uiConfirmSound);

        private void PlaySFX(AudioClip clip)
        {
            if (clip != null)
                PlaySoundFXClipWithPool(clip, transform, 1f);
        }
        #endregion

        #region Item SFX Methods
        public void PlaySickleSound() => PlaySFX(sickleSound);
        public void PlayWateringSound() => PlaySFX(wateringSound);
        public void PlayFertilizerSound() => PlaySFX(fertilizerSound);
        #endregion

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