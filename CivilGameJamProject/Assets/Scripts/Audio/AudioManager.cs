using UnityEngine;

namespace CivilGame.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Source")]
        [SerializeField] private AudioSource _sfxSource;

        [Header("SFX Clips")]
        [SerializeField] private AudioClip _stepsClip;
        [SerializeField] private AudioClip _pageFoldClip;
        [SerializeField] private AudioClip _bookCloseClip;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_sfxSource == null)
            {
                _sfxSource = GetComponent<AudioSource>();
                if (_sfxSource == null)
                {
                    _sfxSource = gameObject.AddComponent<AudioSource>();
                }
            }
            _sfxSource.playOnAwake = false;
        }

        public void PlaySteps(float volume = 0.85f)
        {
            PlaySFX(_stepsClip, volume, 0.94f, 1.06f);
        }

        public void PlayPageFold(float volume = 0.9f)
        {
            PlaySFX(_pageFoldClip, volume, 0.96f, 1.04f);
        }

        public void PlayBookClose(float volume = 0.9f)
        {
            PlaySFX(_bookCloseClip, volume, 0.97f, 1.03f);
        }

        public void PlaySFX(AudioClip clip, float volume = 1f, float minPitch = 0.95f, float maxPitch = 1.05f)
        {
            if (clip == null || _sfxSource == null) return;

            float originalPitch = _sfxSource.pitch;
            _sfxSource.pitch = Random.Range(minPitch, maxPitch);
            _sfxSource.PlayOneShot(clip, volume);
            _sfxSource.pitch = originalPitch;
        }
    }
}
