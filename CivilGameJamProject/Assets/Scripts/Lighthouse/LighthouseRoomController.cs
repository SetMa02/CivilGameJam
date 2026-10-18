using System.Collections;
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
        [Tooltip("Multi-layer parallax window root for Left Window (LeftWindow)")]
        [SerializeField] private GameObject _leftWindowParallaxRoot;
        [Tooltip("Multi-layer parallax window root for Right Window (RightWindow)")]
        [SerializeField] private GameObject _rightWindowParallaxRoot;

        [Header("Navigation Buttons")]
        [SerializeField] private Button _navLeftButton;
        [SerializeField] private Button _navRightButton;

        [Header("Audio")]
        [SerializeField] private AudioClip _stepsClip;
        [SerializeField] private AudioSource _audioSource;

        [Header("Room Transition Animation")]
        [SerializeField] private float _transitionDuration = 0.65f;

        private LighthouseView _currentView = LighthouseView.Hall;
        private bool _isInitialized = false;
        private Coroutine _viewTransitionCoroutine;
        private bool _isSwitchingView = false;

        public bool IsSwitchingView => _isSwitchingView;
        public LighthouseView CurrentView => _currentView;
        public event System.Action<LighthouseView> OnViewChanged;

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
            if (_isSwitchingView) return;

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
            if (_isSwitchingView) return;

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
            if (_currentView == view && _isInitialized) return;

            if (playSound && _isInitialized)
            {
                PlayStepsSound();
            }

            if (_viewTransitionCoroutine != null)
            {
                StopCoroutine(_viewTransitionCoroutine);
            }

            if (_isInitialized)
            {
                _viewTransitionCoroutine = StartCoroutine(TransitionToViewRoutine(_currentView, view));
            }
            else
            {
                ApplyViewInstant(view);
            }
        }

        private IEnumerator TransitionToViewRoutine(LighthouseView oldView, LighthouseView newView)
        {
            _isSwitchingView = true;
            _currentView = newView;

            var cam = Camera.main;
            var parallaxCtrl = CivilGame.CameraEffects.WindowParallaxController.Instance != null 
                ? CivilGame.CameraEffects.WindowParallaxController.Instance 
                : FindAnyObjectByType<CivilGame.CameraEffects.WindowParallaxController>();

            if (parallaxCtrl != null)
            {
                parallaxCtrl.IsLockedBySequence = true;
                parallaxCtrl.SetParallaxActive(false);
            }

            // Направление поворота головы смотрителя (-1 влево, +1 вправо)
            float panDirection = 0f;
            if (oldView == LighthouseView.Hall && newView == LighthouseView.RightWindow) panDirection = -1f;
            else if (oldView == LighthouseView.RightWindow && newView == LighthouseView.Hall) panDirection = 1f;
            else if (oldView == LighthouseView.Hall && newView == LighthouseView.LeftWindow) panDirection = 1f;
            else if (oldView == LighthouseView.LeftWindow && newView == LighthouseView.Hall) panDirection = -1f;

            Vector3 startPos = cam != null ? cam.transform.position : new Vector3(0f, 0f, -11.44f);
            Quaternion startRot = cam != null ? cam.transform.rotation : Quaternion.identity;

            float halfDuration = _transitionDuration * 0.5f;
            float elapsed = 0f;

            // Мягкий поворот головы и смещение взгляда
            Vector3 midPanPos = new Vector3(panDirection * 1.8f, -0.08f, -10.9f);
            Quaternion midPanRot = Quaternion.Euler(0f, -panDirection * 8.5f, -panDirection * 1.2f);

            // Фаза 1: Камера плавно и кинематографично поворачивается в сторону шага
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                // Плавная кривая SmoothStep
                float ease = t * t * (3f - 2f * t);

                if (cam != null)
                {
                    cam.transform.position = Vector3.Lerp(startPos, midPanPos, ease);
                    cam.transform.rotation = Quaternion.Slerp(startRot, midPanRot, ease);
                }
                yield return null;
            }

            // Переключаем комнату в момент максимального поворота
            ApplyViewInstant(newView);

            // Фаза 2: Камера плавно возвращается и центрируется на новом виде
            elapsed = 0f;
            Vector3 centerPos = new Vector3(0f, 0f, -11.44f);
            Quaternion centerRot = Quaternion.identity;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                float ease = t * t * (3f - 2f * t);

                if (cam != null)
                {
                    cam.transform.position = Vector3.Lerp(midPanPos, centerPos, ease);
                    cam.transform.rotation = Quaternion.Slerp(midPanRot, centerRot, ease);
                }
                yield return null;
            }

            if (cam != null)
            {
                cam.transform.position = centerPos;
                cam.transform.rotation = centerRot;
            }

            if (parallaxCtrl != null)
            {
                parallaxCtrl.IsLockedBySequence = false;
                if (newView == LighthouseView.RightWindow || newView == LighthouseView.LeftWindow)
                {
                    parallaxCtrl.SetParallaxActive(true);
                }
            }

            _isSwitchingView = false;
            _viewTransitionCoroutine = null;
        }

        private void ApplyViewInstant(LighthouseView view)
        {
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
                    if (_leftWindowParallaxRoot != null) _leftWindowParallaxRoot.SetActive(false);
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
                    if (_leftWindowParallaxRoot != null)
                    {
                        if (_backgroundRenderer != null) _backgroundRenderer.gameObject.SetActive(false);
                        _leftWindowParallaxRoot.SetActive(true);
                    }
                    else if (_backgroundRenderer != null && _leftWindowSprite != null)
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

                    // Enable parallax for Left Window
                    var pCtrlLeft = CivilGame.CameraEffects.WindowParallaxController.Instance != null 
                        ? CivilGame.CameraEffects.WindowParallaxController.Instance 
                        : FindAnyObjectByType<CivilGame.CameraEffects.WindowParallaxController>();
                    if (pCtrlLeft != null)
                    {
                        pCtrlLeft.SetParallaxActive(true);
                    }
                    break;

                case LighthouseView.RightWindow:
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

                    if (_leftWindowParallaxRoot != null) _leftWindowParallaxRoot.SetActive(false);
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

            OnViewChanged?.Invoke(_currentView);
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

