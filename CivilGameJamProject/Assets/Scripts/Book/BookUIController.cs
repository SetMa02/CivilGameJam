using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CivilGame.Audio;

namespace CivilGame.Book
{
    public class BookUIController : MonoBehaviour
    {
        [Header("Pages Configuration")]
        [SerializeField] private List<ManualPageData> _pages = new List<ManualPageData>();
        [SerializeField] private int _currentPageIndex = 0;

        [Header("UI Structure")]
        [SerializeField] private GameObject _bookRootPanel;
        [SerializeField] private RectTransform _bookCard;
        [SerializeField] private Button _outsideClickBackdrop;

        [Header("Left Page Elements")]
        [SerializeField] private TextMeshProUGUI _leftTitleText;
        [SerializeField] private Image _leftIllustration;
        [SerializeField] private TextMeshProUGUI _leftContentText;

        [Header("Right Page Elements")]
        [SerializeField] private TextMeshProUGUI _rightTitleText;
        [SerializeField] private Image _rightIllustration;
        [SerializeField] private TextMeshProUGUI _rightContentText;

        [Header("Page Numbers")]
        [SerializeField] private TextMeshProUGUI _leftPageNumberText;
        [SerializeField] private TextMeshProUGUI _rightPageNumberText;

        [Header("Navigation Buttons")]
        [SerializeField] private Button _prevPageButton;
        [SerializeField] private Button _nextPageButton;

        [Header("Audio")]
        [SerializeField] private AudioClip _pageFoldClip;
        [SerializeField] private AudioClip _bookCloseClip;
        [SerializeField] private AudioSource _audioSource;

        public bool IsOpen => _bookRootPanel != null && _bookRootPanel.activeSelf;

        private void Awake()
        {
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }

            if (_outsideClickBackdrop != null)
            {
                _outsideClickBackdrop.onClick.AddListener(() => CloseBook(true));
            }

            if (_prevPageButton != null)
            {
                _prevPageButton.onClick.AddListener(PreviousPage);
            }

            if (_nextPageButton != null)
            {
                _nextPageButton.onClick.AddListener(NextPage);
            }
        }

        private void Start()
        {
            CloseBook(false);
        }

        private void OnDestroy()
        {
            if (_outsideClickBackdrop != null)
            {
                _outsideClickBackdrop.onClick.RemoveAllListeners();
            }

            if (_prevPageButton != null)
            {
                _prevPageButton.onClick.RemoveListener(PreviousPage);
            }

            if (_nextPageButton != null)
            {
                _nextPageButton.onClick.RemoveListener(NextPage);
            }
        }

        public void OpenBook()
        {
            if (_bookRootPanel != null)
            {
                _bookRootPanel.SetActive(true);
            }
            UpdatePageDisplay();
            PlayPageFoldSound();
        }

        public void CloseBook()
        {
            CloseBook(true);
        }

        public void CloseBook(bool playSound)
        {
            bool wasOpen = IsOpen;
            if (_bookRootPanel != null)
            {
                _bookRootPanel.SetActive(false);
            }

            if (wasOpen && playSound)
            {
                PlayBookCloseSound();
            }
        }

        public void NextPage()
        {
            if (_currentPageIndex < _pages.Count - 1)
            {
                _currentPageIndex++;
                UpdatePageDisplay();
                PlayPageFoldSound();
            }
        }

        public void PreviousPage()
        {
            if (_currentPageIndex > 0)
            {
                _currentPageIndex--;
                UpdatePageDisplay();
                PlayPageFoldSound();
            }
        }

        private void PlayPageFoldSound()
        {
            if (_audioSource != null && _pageFoldClip != null)
            {
                float prev = _audioSource.pitch;
                _audioSource.pitch = Random.Range(0.96f, 1.04f);
                _audioSource.PlayOneShot(_pageFoldClip, 0.9f);
                _audioSource.pitch = prev;
            }
            else if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPageFold();
            }
        }

        private void PlayBookCloseSound()
        {
            if (_audioSource != null && _bookCloseClip != null)
            {
                float prev = _audioSource.pitch;
                _audioSource.pitch = Random.Range(0.97f, 1.03f);
                _audioSource.PlayOneShot(_bookCloseClip, 0.9f);
                _audioSource.pitch = prev;
            }
            else if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBookClose();
            }
        }

        private void UpdatePageDisplay()
        {
            if (_pages == null || _pages.Count == 0)
            {
                ClearPages();
                if (_prevPageButton != null) _prevPageButton.gameObject.SetActive(false);
                if (_nextPageButton != null) _nextPageButton.gameObject.SetActive(false);
                return;
            }

            _currentPageIndex = Mathf.Clamp(_currentPageIndex, 0, _pages.Count - 1);
            ManualPageData data = _pages[_currentPageIndex];

            // Left page
            if (_leftTitleText != null) _leftTitleText.text = data.LeftPageTitle ?? "";
            if (_leftContentText != null) _leftContentText.text = data.LeftPageContent ?? "";
            if (_leftIllustration != null)
            {
                bool hasImg = data.LeftPageIllustration != null;
                _leftIllustration.gameObject.SetActive(hasImg);
                if (hasImg) _leftIllustration.sprite = data.LeftPageIllustration;
            }

            // Right page
            if (_rightTitleText != null) _rightTitleText.text = data.RightPageTitle ?? "";
            if (_rightContentText != null) _rightContentText.text = data.RightPageContent ?? "";
            if (_rightIllustration != null)
            {
                bool hasImg = data.RightPageIllustration != null;
                _rightIllustration.gameObject.SetActive(hasImg);
                if (hasImg) _rightIllustration.sprite = data.RightPageIllustration;
            }

            // Page numbers
            int leftPageNum = (_currentPageIndex * 2) + 1;
            int rightPageNum = (_currentPageIndex * 2) + 2;

            if (_leftPageNumberText != null)
            {
                _leftPageNumberText.text = string.IsNullOrEmpty(data.PageNumberText)
                    ? $"{leftPageNum}"
                    : $"{leftPageNum}";
            }

            if (_rightPageNumberText != null)
            {
                _rightPageNumberText.text = $"{rightPageNum}";
            }

            // Navigation buttons visibility
            if (_prevPageButton != null)
            {
                _prevPageButton.gameObject.SetActive(_currentPageIndex > 0);
            }

            if (_nextPageButton != null)
            {
                _nextPageButton.gameObject.SetActive(_currentPageIndex < _pages.Count - 1);
            }
        }

        private void ClearPages()
        {
            if (_leftTitleText != null) _leftTitleText.text = "";
            if (_leftContentText != null) _leftContentText.text = "";
            if (_leftIllustration != null) _leftIllustration.gameObject.SetActive(false);

            if (_rightTitleText != null) _rightTitleText.text = "";
            if (_rightContentText != null) _rightContentText.text = "";
            if (_rightIllustration != null) _rightIllustration.gameObject.SetActive(false);

            if (_leftPageNumberText != null) _leftPageNumberText.text = "";
            if (_rightPageNumberText != null) _rightPageNumberText.text = "";
        }
    }
}

