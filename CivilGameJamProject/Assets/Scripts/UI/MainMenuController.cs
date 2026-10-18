using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using CivilGame.Localization;

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

        [Header("Language Selection")]
        [SerializeField] private UnityEngine.UI.Button _englishButton;
        [SerializeField] private UnityEngine.UI.Button _turkishButton;

        private readonly Color _activeLangColor = new Color(0.20f, 0.42f, 0.72f, 1f);
        private readonly Color _inactiveLangColor = new Color(0.14f, 0.18f, 0.25f, 1f);

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

            if (_englishButton != null)
            {
                _englishButton.onClick.AddListener(OnEnglishClicked);
            }

            if (_turkishButton != null)
            {
                _turkishButton.onClick.AddListener(OnTurkishClicked);
            }

            if (_settingsPanel != null)
            {
                _settingsPanel.SetActive(false);
            }

            UpdateLanguageButtons();
        }

        private void Start()
        {
            UpdateLanguageButtons();
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

            if (_englishButton != null)
            {
                _englishButton.onClick.RemoveListener(OnEnglishClicked);
            }

            if (_turkishButton != null)
            {
                _turkishButton.onClick.RemoveListener(OnTurkishClicked);
            }
        }

        public void OnNewGameClicked()
        {
            PlayerPrefs.SetInt("TutorialCompleted", 0);
            PlayerPrefs.Save();
            Debug.Log($"[MainMenu] Starting New Game (Tutorial reset), loading scene: {_gameSceneName}");
            SceneManager.LoadScene(_gameSceneName);
        }

        public void OnSettingsClicked()
        {
            Debug.Log("[MainMenu] Settings clicked");
            if (_settingsPanel != null)
            {
                _settingsPanel.SetActive(true);
                UpdateLanguageButtons();
            }
        }

        public void OnCloseSettingsClicked()
        {
            if (_settingsPanel != null)
            {
                _settingsPanel.SetActive(false);
            }
        }

        public void OnEnglishClicked()
        {
            LocalizationManager.CurrentLanguage = GameLanguage.English;
            UpdateLanguageButtons();
        }

        public void OnTurkishClicked()
        {
            LocalizationManager.CurrentLanguage = GameLanguage.Turkish;
            UpdateLanguageButtons();
        }

        private void UpdateLanguageButtons()
        {
            var cur = LocalizationManager.CurrentLanguage;

            if (_englishButton != null)
            {
                var img = _englishButton.GetComponent<UnityEngine.UI.Image>();
                if (img != null)
                {
                    img.color = cur == GameLanguage.English ? _activeLangColor : _inactiveLangColor;
                }
            }

            if (_turkishButton != null)
            {
                var img = _turkishButton.GetComponent<UnityEngine.UI.Image>();
                if (img != null)
                {
                    img.color = cur == GameLanguage.Turkish ? _activeLangColor : _inactiveLangColor;
                }
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

