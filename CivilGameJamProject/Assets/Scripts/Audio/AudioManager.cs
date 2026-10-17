using System.Collections;
using UnityEngine;

namespace CivilGame.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioSource _fireAmbientSource;
        [SerializeField] private AudioSource _timedAmbientSource;

        [Header("UI & Movement SFX")]
        [SerializeField] private AudioClip _stepsClip;
        [SerializeField] private AudioClip _pageFoldClip;
        [SerializeField] private AudioClip _bookCloseClip;

        [Header("Threat SFX")]
        [SerializeField] private AudioClip _cicadasClip;
        [SerializeField] private AudioClip _birdsClip;
        [SerializeField] private AudioClip _fireClip;
        [SerializeField] private AudioClip _landslideClip;
        [SerializeField] private AudioClip _earthquakeClip;

        private Coroutine _timedAmbientCoroutine;
        private Coroutine _fireFadeCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureAudioSources();
        }

        private void EnsureAudioSources()
        {
            if (_sfxSource == null)
            {
                _sfxSource = gameObject.AddComponent<AudioSource>();
            }
            _sfxSource.playOnAwake = false;

            if (_fireAmbientSource == null)
            {
                _fireAmbientSource = gameObject.AddComponent<AudioSource>();
            }
            _fireAmbientSource.playOnAwake = false;
            _fireAmbientSource.loop = true;

            if (_timedAmbientSource == null)
            {
                _timedAmbientSource = gameObject.AddComponent<AudioSource>();
            }
            _timedAmbientSource.playOnAwake = false;
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

        public void PlayCicadasTimed(float duration = 4.0f, float volume = 0.85f)
        {
            PlayTimedAmbient(_cicadasClip, duration, volume);
        }

        public void PlayBirdsTimed(float duration = 4.0f, float volume = 0.85f)
        {
            PlayTimedAmbient(_birdsClip, duration, volume);
        }

        private void PlayTimedAmbient(AudioClip clip, float duration, float volume)
        {
            if (clip == null || _timedAmbientSource == null) return;

            if (_timedAmbientCoroutine != null)
            {
                StopCoroutine(_timedAmbientCoroutine);
            }
            _timedAmbientCoroutine = StartCoroutine(TimedAmbientRoutine(clip, duration, volume));
        }

        private IEnumerator TimedAmbientRoutine(AudioClip clip, float duration, float targetVolume)
        {
            _timedAmbientSource.clip = clip;
            _timedAmbientSource.volume = targetVolume;
            _timedAmbientSource.loop = true;
            _timedAmbientSource.Play();

            float fadeOutDuration = 0.8f;
            float stayDuration = Mathf.Max(0f, duration - fadeOutDuration);

            yield return new WaitForSeconds(stayDuration);

            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                _timedAmbientSource.volume = Mathf.Lerp(targetVolume, 0f, elapsed / fadeOutDuration);
                yield return null;
            }

            _timedAmbientSource.Stop();
            _timedAmbientSource.loop = false;
            _timedAmbientCoroutine = null;
        }

        public void SetFireActive(bool active, float targetVolume = 0.8f)
        {
            if (_fireClip == null || _fireAmbientSource == null) return;

            if (_fireFadeCoroutine != null)
            {
                StopCoroutine(_fireFadeCoroutine);
            }
            _fireFadeCoroutine = StartCoroutine(FireFadeRoutine(active, targetVolume));
        }

        private IEnumerator FireFadeRoutine(bool active, float targetVolume)
        {
            float duration = 1.0f;
            float elapsed = 0f;

            if (active)
            {
                if (!_fireAmbientSource.isPlaying)
                {
                    _fireAmbientSource.clip = _fireClip;
                    _fireAmbientSource.volume = 0f;
                    _fireAmbientSource.Play();
                }

                float startVol = _fireAmbientSource.volume;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    _fireAmbientSource.volume = Mathf.Lerp(startVol, targetVolume, elapsed / duration);
                    yield return null;
                }
                _fireAmbientSource.volume = targetVolume;
            }
            else
            {
                if (_fireAmbientSource.isPlaying)
                {
                    float startVol = _fireAmbientSource.volume;
                    while (elapsed < duration)
                    {
                        elapsed += Time.deltaTime;
                        _fireAmbientSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
                        yield return null;
                    }
                    _fireAmbientSource.volume = 0f;
                    _fireAmbientSource.Stop();
                }
            }
            _fireFadeCoroutine = null;
        }

        public void PlayEarthquake(float volume = 0.95f)
        {
            PlaySFX(_earthquakeClip, volume, 0.98f, 1.02f);
        }

        public void PlayLandslide(float volume = 0.95f)
        {
            PlaySFX(_landslideClip, volume, 0.97f, 1.03f);
        }
    }
}
