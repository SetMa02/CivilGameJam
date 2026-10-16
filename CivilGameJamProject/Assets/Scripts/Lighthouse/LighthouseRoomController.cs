using UnityEngine;
using UnityEngine.UI;

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

        [Header("Navigation Buttons")]
        [SerializeField] private Button _navLeftButton;
        [SerializeField] private Button _navRightButton;

        private LighthouseView _currentView = LighthouseView.Hall;

        private void Awake()
        {
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
            SetView(LighthouseView.Hall);
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
                SetView(LighthouseView.LeftWindow);
            }
            else if (_currentView == LighthouseView.RightWindow)
            {
                SetView(LighthouseView.Hall);
            }
        }

        public void OnRightButtonClicked()
        {
            if (_currentView == LighthouseView.Hall)
            {
                SetView(LighthouseView.RightWindow);
            }
            else if (_currentView == LighthouseView.LeftWindow)
            {
                SetView(LighthouseView.Hall);
            }
        }

        public void SetView(LighthouseView view)
        {
            _currentView = view;

            switch (_currentView)
            {
                case LighthouseView.Hall:
                    if (_backgroundRenderer != null && _hallSprite != null)
                    {
                        _backgroundRenderer.sprite = _hallSprite;
                        UpdateBackgroundScale(_hallSprite);
                    }
                    if (_hallDeskObjects != null) _hallDeskObjects.SetActive(true);
                    if (_navLeftButton != null) _navLeftButton.gameObject.SetActive(true);
                    if (_navRightButton != null) _navRightButton.gameObject.SetActive(true);
                    break;

                case LighthouseView.LeftWindow:
                    if (_backgroundRenderer != null && _leftWindowSprite != null)
                    {
                        _backgroundRenderer.sprite = _leftWindowSprite;
                        UpdateBackgroundScale(_leftWindowSprite);
                    }
                    if (_hallDeskObjects != null) _hallDeskObjects.SetActive(false);
                    // From Left Window, player looks right to return to Hall
                    if (_navLeftButton != null) _navLeftButton.gameObject.SetActive(false);
                    if (_navRightButton != null) _navRightButton.gameObject.SetActive(true);
                    break;

                case LighthouseView.RightWindow:
                    if (_backgroundRenderer != null && _rightWindowSprite != null)
                    {
                        _backgroundRenderer.sprite = _rightWindowSprite;
                        UpdateBackgroundScale(_rightWindowSprite);
                    }
                    if (_hallDeskObjects != null) _hallDeskObjects.SetActive(false);
                    // From Right Window, player looks left to return to Hall
                    if (_navLeftButton != null) _navLeftButton.gameObject.SetActive(true);
                    if (_navRightButton != null) _navRightButton.gameObject.SetActive(false);
                    break;
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
