using System;

namespace LgTyLib.Modules.Audio
{
    [Serializable]
    public class AudioSettingsData
    {

        public AudioSettingsData() {
            masterAudioScale = 0.5f;
            musicAudioScale = 0.5f;
            sfxAudioScale = 0.5f;
        }

        [NonSerialized]
        public const string masterAudioScaleSavingName = "MasetAudioScale";
        public float masterAudioScale;

        [NonSerialized]
        public const string musicAudioScaleSavingName = "MusicAudioScale";
        public float musicAudioScale;

        [NonSerialized]
        public const string sfxAudioScaleSavingName = "SFXAudioScale";
        public float sfxAudioScale;
    }
}
