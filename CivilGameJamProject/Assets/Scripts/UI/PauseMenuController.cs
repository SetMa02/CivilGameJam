using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using CivilGame.Book;
using CivilGame.Phone;

namespace CivilGame.UI
{
    public class PauseMenuController : MonoBehaviour
    {
        public static PauseMenuController Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject _pauseModal;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _mainMenuButton;

        [Header("Scene Settings")]
        [SerializeField] private string _mainMenuSceneName = "MainMenu";

        private bool _isPaused = false;
        public bool IsPaused => _isPaused;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_pauseButton != null)
            {
                _pauseButton.onClick.AddListener(PauseGame);
            }

            if (_resumeButton != null)
            {
                _resumeButton.onClick.AddListener(ResumeGame);
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.AddListener(RestartGame);
            }

            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.AddListener(ExitToMainMenu);
            }

            if (_pauseModal != null)
            {
                _pauseModal.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_pauseButton != null) _pauseButton.onClick.RemoveListener(PauseGame);
            if (_resumeButton != null) _resumeButton.onClick.RemoveListener(ResumeGame);
            if (_restartButton != null) _restartButton.onClick.RemoveListener(RestartGame);
            if (_mainMenuButton != null) _mainMenuButton.onClick.RemoveListener(ExitToMainMenu);

            // Always restore time scale when leaving scene
            Time.timeScale = 1f;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                HandleEscapePressed();
            }
        }

        private void HandleEscapePressed()
        {
            // If pause menu is open, pressing Escape resumes
            if (_isPaused)
            {
                ResumeGame();
                return;
            }

            // If book is open, close book
            var book = FindAnyObjectByType<BookUIController>();
            if (book != null && book.IsOpen)
            {
                book.CloseBook(true);
                return;
            }

            // If phone is open, close phone
            var phone = FindAnyObjectByType<PhoneUIController>();
            if (phone != null && phone.IsOpen)
            {
                phone.ClosePhone();
                return;
            }

            // Otherwise, pause game
            PauseGame();
        }

        public void PauseGame()
        {
            _isPaused = true;
            Time.timeScale = 0f;

            if (_pauseModal != null)
            {
                _pauseModal.SetActive(true);
            }
        }

        public void ResumeGame()
        {
            _isPaused = false;
            Time.timeScale = 1f;

            if (_pauseModal != null)
            {
                _pauseModal.SetActive(false);
            }
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            PlayerPrefs.SetInt(CivilGame.DayCycle.DayOneTutorialDirector.TutorialPrefKey, 1);
            PlayerPrefs.Save();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void ExitToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(_mainMenuSceneName);
        }
    }
}
