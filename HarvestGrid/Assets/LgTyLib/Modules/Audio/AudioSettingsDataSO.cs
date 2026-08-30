using UnityEngine;

namespace LgTyLib.Modules.Audio
{

    [CreateAssetMenu(fileName = "AudioSettingsDataSO", menuName = "LgTyLib/Audio/AudioSettingsDataSO")]
    public class AudioSettingsDataSO : ScriptableObject
    {
        public AudioSettingsData audioSettingsData;
    }
}
