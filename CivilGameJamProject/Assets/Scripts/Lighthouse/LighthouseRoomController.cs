using UnityEngine;
using UnityEngine.UI;
using CivilGame.Audio;

namespace CivilGame.Lighthouse
{
    public enum LighthouseView
    {
        LeftWindow,
        Hall,
        RightWindow
    }

    public class LighthouseRoomController : MonoBehaviour
    {
        [Header("Sprites")]
        [SerializeField] private Sprite _hallSprite;
        [SerializeField] private Sprite _leftWindowSprite;
        [SerializeField] private Sprite _rightWindowSprite;

        [Header("Renderers & Containers")]
        [SerializeField] private SpriteRenderer _backgroundRenderer;
        [Tooltip("Interactive objects only visible in Hall view (Book, Phone)")]
        [SerializeField] private GameObject _hallDeskObjects;
        [Tooltip("Bed primitive in Hall view")]
        [SerializeField] private GameObject _hallBedObject;
        [Tooltip("Zone indicator objects on Left Window (Forest, Field)")]
        [SerializeField] private GameObject _leftWindowObjects;
        [Tooltip("Zone indicator objects on Right Window (Sea, Mountains)")]
        [SerializeField] private GameObject _rightWindowObjects;
        [Tooltip("Multi-layer parallax window root for Right Window (WINDOWRIGHT)")]
        [SerializeField] private GameObject _rightWindowParallaxRoot;

        [Header("Navigation Buttons")]
        [SerializeField] private Button _navLeftButton;
        [SerializeField] private Button _navRightButton;

        [Header("Audio")]
        [SerializeField] private AudioClip _stepsClip;
        [SerializeField] private AudioSource _audioSource;

        private LighthouseView _currentView = LighthouseView.Hall;
        private bool _isInitialized = false;

        private void Awake()
        {
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }

            if (_navLeftButton != null)
            {
                _navLeftButton.onClick.AddListener(OnLeftButtonClicked);
            }

            if (_navRightButton != null)
            {
                _navRightButton.onClick.AddListener(OnRightButtonClicked);
            }
        }

        private void Start()
        {
            SetView(LighthouseView.Hall, false);
            _isInitialized = true;
        }

        private void OnDestroy()
        {
            if (_navLeftButton != null)
            {
                _navLeftButton.onClick.RemoveListener(OnLeftButtonClicked);
            }

            if (_navRightButton != null)
            {
                _navRightButton.onClick.RemoveListener(OnRightButtonClicked);
            }
        }

        public void OnLeftButtonClicked()
        {
            if (_currentView == LighthouseView.Hall)
            {
                SetView(LighthouseView.LeftWindow, true);
            }
            else if (_currentView == LighthouseView.RightWindow)
            {
                SetView(LighthouseView.Hall, true);
            }
        }

        public void OnRightButtonClicked()
        {
            if (_currentView == LighthouseView.Hall)
            {
                SetView(LighthouseView.RightWindow, true);
            }
            else if (_currentView == LighthouseView.LeftWindow)
            {
                SetView(LighthouseView.Hall, true);
            }
        }

        public void SetView(LighthouseView view)
        {
            SetView(view, _isInitialized);
        }

        public void SetView(LighthouseView view, bool playSound)
        {
            if (_currentView != view && playSound)
            {
                PlayStepsSound();
            }

            _currentView = view;

            switch (_currentView)
            {
                case LighthouseView.Hall:
                    if (_backgroundRenderer != null && _hallSprite != null)
                    {
                        _backgroundRenderer.gameObject.SetActive(true);
                        _backgroundRenderer.sprite = _hallSprite;
                        UpdateBackgroundScale(_hallSprite);
                    }
                    if (_rightWindowParallaxRoot != null) _rightWindowParallaxRoot.SetActive(false);
                    if (_hallDeskObjects != null) _hallDeskObjects.SetActive(true);
                    if (_hallBedObject != null) _hallBedObject.SetActive(true);
                    if (_leftWindowObjects != null) _leftWindowObjects.SetActive(false);
                    if (_rightWindowObjects != null) _rightWindowObjects.SetActive(false);
                    if (_navLeftButton != null) _navLeftButton.gameObject.SetActive(true);
                    if (_navRightButton != null) _navRightButton.gameObject.SetActive(true);

                    // Disable parallax in Hall
                    var pCtrlHall = CivilGame.CameraEffects.WindowParallaxController.Instance != null 
                        ? CivilGame.CameraEffects.WindowParallaxController.Instance 
                        : FindAnyObjectByType<CivilGame.CameraEffects.WindowParallaxController>();
                    if (pCtrlHall != null)
                    {
                        pCtrlHall.SetParallaxActive(false);
                    }
                    break;

                case LighthouseView.LeftWindow:
                    if (_backgroundRenderer != null && _leftWindowSprite != null)
                    {
                        _backgroundRenderer.gameObject.SetActive(true);
                        _backgroundRenderer.sprite = _leftWindowSprite;
                        UpdateBackgroundScale(_leftWindowSprite);
                    }
                    if (_rightWindowParallaxRoot != null) _rightWindowParallaxRoot.SetActive(false);
                    if (_hallDeskObjects != null) _hallDeskObjects.SetActive(false);
                    if (_hallBedObject != null) _hallBedObject.SetActive(false);
                    if (_leftWindowObjects != null) _leftWindowObjects.SetActive(true);
                    if (_rightWindowObjects != null) _rightWindowObjects.SetActive(false);
                    // From Left Window, player looks right to return to Hall
                    if (_navLeftButton != null) _navLeftButton.gameObject.SetActive(false);
                    if (_navRightButton != null) _navRightButton.gameObject.SetActive(true);

                    // Disable parallax for flat left window
                    var pCtrlLeft = CivilGame.CameraEffects.WindowParallaxController.Instance != null 
                        ? CivilGame.CameraEffects.WindowParallaxController.Instance 
                        : FindAnyObjectByType<CivilGame.CameraEffects.WindowParallaxController>();
                    if (pCtrlLeft != null)
                    {
                        pCtrlLeft.SetParallaxActive(false);
                    }
                    break;

                case LighthouseView.RightWindow:
                    // When parallax window is available, hide flat background and activate parallax root
                    if (_rightWindowParallaxRoot != null)
                    {
                        if (_backgroundRenderer != null) _backgroundRenderer.gameObject.SetActive(false);
                        _rightWindowParallaxRoot.SetActive(true);
                    }
                    else if (_backgroundRenderer != null && _rightWindowSprite != null)
                    {
                        _backgroundRenderer.gameObject.SetActive(true);
                        _backgroundRenderer.sprite = _rightWindowSprite;
                        UpdateBackgroundScale(_rightWindowSprite);
                    }

                    if (_hallDeskObjects != null) _hallDeskObjects.SetActive(false);
                    if (_hallBedObject != null) _hallBedObject.SetActive(false);
                    if (_leftWindowObjects != null) _leftWindowObjects.SetActive(false);
                    if (_rightWindowObjects != null) _rightWindowObjects.SetActive(true);
                    // From Right Window, player looks left to return to Hall
                    if (_navLeftButton != null) _navLeftButton.gameObject.SetActive(true);
                    if (_navRightButton != null) _navRightButton.gameObject.SetActive(false);

                    // Enable parallax in Right Window
                    var pCtrl = CivilGame.CameraEffects.WindowParallaxController.Instance != null 
                        ? CivilGame.CameraEffects.WindowParallaxController.Instance 
                        : FindAnyObjectByType<CivilGame.CameraEffects.WindowParallaxController>();
                    if (pCtrl != null)
                    {
                        pCtrl.SetParallaxActive(true);
                    }
                    break;
            }
        }

        private void PlayStepsSound()
        {
            if (_audioSource != null && _stepsClip != null)
            {
                float prevPitch = _audioSource.pitch;
                _audioSource.pitch = Random.Range(0.95f, 1.05f);
                _audioSource.PlayOneShot(_stepsClip, 0.85f);
                _audioSource.pitch = prevPitch;
            }
            else if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySteps();
            }
        }

        private void UpdateBackgroundScale(Sprite sprite)
        {
            if (Camera.main == null || sprite == null || _backgroundRenderer == null) return;

            float cameraHeight = Camera.main.orthographicSize * 2f;
            float spriteHeightInUnits = sprite.rect.height / sprite.pixelsPerUnit;
            float scale = cameraHeight / spriteHeightInUnits;

            _backgroundRenderer.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}

