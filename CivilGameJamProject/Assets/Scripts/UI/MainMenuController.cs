using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CivilGame.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scene Settings")]
        [Tooltip("Name of the scene to load when clicking New Game")]
        [SerializeField] private string _gameSceneName = "GameScene";

        [Header("Buttons")]
        [SerializeField] private UnityEngine.UI.Button _newGameButton;
        [SerializeField] private UnityEngine.UI.Button _settingsButton;
        [SerializeField] private UnityEngine.UI.Button _exitButton;

        [Header("Settings Panel")]
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private UnityEngine.UI.Button _closeSettingsButton;
        [SerializeField] private UnityEngine.UI.Slider _volumeSlider;
        [SerializeField] private UnityEngine.UI.Toggle _fullscreenToggle;

        private void Awake()
        {
            if (_newGameButton != null)
            {
                _newGameButton.onClick.AddListener(OnNewGameClicked);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.AddListener(OnSettingsClicked);
            }

            if (_exitButton != null)
            {
                _exitButton.onClick.AddListener(OnExitClicked);
            }

            if (_closeSettingsButton != null)
            {
                _closeSettingsButton.onClick.AddListener(OnCloseSettingsClicked);
            }

            if (_volumeSlider != null)
            {
                _volumeSlider.value = AudioListener.volume;
                _volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }

            if (_fullscreenToggle != null)
            {
                _fullscreenToggle.isOn = Screen.fullScreen;
                _fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggled);
            }

            if (_settingsPanel != null)
            {
                _settingsPanel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_newGameButton != null)
            {
                _newGameButton.onClick.RemoveListener(OnNewGameClicked);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveListener(OnSettingsClicked);
            }

            if (_exitButton != null)
            {
                _exitButton.onClick.RemoveListener(OnExitClicked);
            }

            if (_closeSettingsButton != null)
            {
                _closeSettingsButton.onClick.RemoveListener(OnCloseSettingsClicked);
            }

            if (_volumeSlider != null)
            {
                _volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
            }

            if (_fullscreenToggle != null)
            {
                _fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenToggled);
            }
        }

        public void OnNewGameClicked()
        {
            Debug.Log($"[MainMenu] Starting New Game, loading scene: {_gameSceneName}");
            SceneManager.LoadScene(_gameSceneName);
        }

        public void OnSettingsClicked()
        {
            Debug.Log("[MainMenu] Settings clicked");
            if (_settingsPanel != null)
            {
                _settingsPanel.SetActive(true);
            }
        }

        public void OnCloseSettingsClicked()
        {
            if (_settingsPanel != null)
            {
                _settingsPanel.SetActive(false);
            }
        }

        private void OnVolumeChanged(float value)
        {
            AudioListener.volume = value;
        }

        private void OnFullscreenToggled(bool isFullscreen)
        {
            Screen.fullScreen = isFullscreen;
        }

        public void OnExitClicked()
        {
            Debug.Log("[MainMenu] Exiting game...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
