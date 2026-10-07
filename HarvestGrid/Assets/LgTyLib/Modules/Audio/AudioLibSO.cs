using UnityEngine;

namespace LgTyLib.Modules.Audio
{
    //[CreateAssetMenu(fileName = "AudioLibSO", menuName = "LgTyLib/Audio/AudioLibSO")]
    public class AudioLibSO : ScriptableObject
    {
        // ╔══════════════════════════════════════════════════════╗
        // ║  EDIT THIS ONE                                       ║
        // ║                                                      ║
        // ╚══════════════════════════════════════════════════════╝
        [Header("Music")]
        public AudioClip bgm1;
        public AudioClip bgm2;

    }
}