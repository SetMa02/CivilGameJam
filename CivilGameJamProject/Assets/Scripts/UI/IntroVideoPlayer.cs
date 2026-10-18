using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.InputSystem;

namespace CivilGame.UI
{
    [RequireComponent(typeof(VideoPlayer))]
    public class IntroVideoPlayer : MonoBehaviour
    {
        [Header("Scene Transition")]
        [Tooltip("The scene to load after the intro finishes")]
        [SerializeField] private string _nextSceneName = "MainMenu";

        [Header("Settings")]
        [Tooltip("Allow skipping the video by pressing Space, Escape, Enter, or clicking the mouse")]
        [SerializeField] private bool _allowSkip = true;

        [Tooltip("Audio source to output video audio through")]
        [SerializeField] private AudioSource _audioSource;

        private VideoPlayer _videoPlayer;
        private bool _isTransitioning = false;

        private void Awake()
        {
            _videoPlayer = GetComponent<VideoPlayer>();

            if (_audioSource != null)
            {
                _videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                _videoPlayer.EnableAudioTrack(0, true);
                _videoPlayer.SetTargetAudioSource(0, _audioSource);
            }

            _videoPlayer.loopPointReached += OnVideoFinished;
            _videoPlayer.errorReceived += OnVideoError;
        }

        private void OnDestroy()
        {
            if (_videoPlayer != null)
            {
                _videoPlayer.loopPointReached -= OnVideoFinished;
                _videoPlayer.errorReceived -= OnVideoError;
            }
        }

        private void Update()
        {
            if (!_allowSkip || _isTransitioning) return;

            // Check if player pressed Escape, Space, Enter or clicked mouse to skip
            bool skipPressed = false;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.spaceKey.wasPressedThisFrame ||
                    Keyboard.current.escapeKey.wasPressedThisFrame ||
                    Keyboard.current.enterKey.wasPressedThisFrame)
                {
                    skipPressed = true;
                }
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                skipPressed = true;
            }

            if (skipPressed)
            {
                ProceedToNextScene();
            }
        }

        private void OnVideoFinished(VideoPlayer source)
        {
            ProceedToNextScene();
        }

        private void OnVideoError(VideoPlayer source, string message)
        {
            Debug.LogWarning($"[IntroVideoPlayer] Video error: {message}. Skipping to {_nextSceneName}.");
            ProceedToNextScene();
        }

        public void ProceedToNextScene()
        {
            if (_isTransitioning) return;
            _isTransitioning = true;

            Debug.Log($"[IntroVideoPlayer] Intro completed or skipped. Loading {_nextSceneName}...");
            SceneManager.LoadScene(_nextSceneName);
        }
    }
}
