using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CivilGame.UI
{
    public class GuideDialogueUI : MonoBehaviour
    {
        public static GuideDialogueUI Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject _dialogueRoot;
        [SerializeField] private Image _characterPortrait;
        [SerializeField] private TextMeshProUGUI _speakerNameText;
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private TextMeshProUGUI _continuePromptText;
        [SerializeField] private Button _clickToAdvanceButton;

        [Header("Settings")]
        [SerializeField] private float _typewriterSpeed = 0.025f;
        [SerializeField] private AudioClip _speechBlipSound;

        private Coroutine _typewriterCoroutine;
        private Action _onLineFinishedCallback;
        private string _fullCurrentMessage = "";
        private bool _isTyping = false;

        public bool IsOpen => _dialogueRoot != null && _dialogueRoot.activeSelf;
        public bool IsTyping => _isTyping;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_clickToAdvanceButton != null)
            {
                _clickToAdvanceButton.onClick.AddListener(OnAdvanceClicked);
            }

            if (_dialogueRoot != null)
            {
                _dialogueRoot.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_clickToAdvanceButton != null)
            {
                _clickToAdvanceButton.onClick.RemoveListener(OnAdvanceClicked);
            }
        }

        public void ShowDialogue(string speaker, string message, Action onComplete = null)
        {
            if (_dialogueRoot != null)
            {
                _dialogueRoot.SetActive(true);
            }

            if (_characterPortrait != null)
            {
                _characterPortrait.gameObject.SetActive(true);
            }

            if (_speakerNameText != null)
            {
                _speakerNameText.text = speaker;
            }

            _fullCurrentMessage = message;
            _onLineFinishedCallback = onComplete;

            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
            }

            _typewriterCoroutine = StartCoroutine(TypewriterRoutine(message));
        }

        private IEnumerator TypewriterRoutine(string targetMessage)
        {
            _isTyping = true;
            if (_messageText != null) _messageText.text = "";
            if (_continuePromptText != null) _continuePromptText.gameObject.SetActive(false);

            int totalLength = targetMessage.Length;
            int visibleChars = 0;

            while (visibleChars < totalLength)
            {
                // Handle rich text tags instantly
                if (targetMessage[visibleChars] == '<')
                {
                    int closeIndex = targetMessage.IndexOf('>', visibleChars);
                    if (closeIndex != -1)
                    {
                        visibleChars = closeIndex + 1;
                    }
                    else
                    {
                        visibleChars++;
                    }
                }
                else
                {
                    visibleChars++;
                }

                if (_messageText != null)
                {
                    _messageText.text = targetMessage.Substring(0, visibleChars);
                }

                yield return new WaitForSecondsRealtime(_typewriterSpeed);
            }

            CompleteTyping();
        }

        private void CompleteTyping()
        {
            _isTyping = false;
            if (_messageText != null) _messageText.text = _fullCurrentMessage;
            if (_continuePromptText != null) _continuePromptText.gameObject.SetActive(true);
            _typewriterCoroutine = null;
        }

        public void OnAdvanceClicked()
        {
            if (_isTyping)
            {
                // Instant reveal if clicked while typing
                if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
                CompleteTyping();
            }
            else
            {
                // Advance to callback
                var callback = _onLineFinishedCallback;
                _onLineFinishedCallback = null;
                callback?.Invoke();
            }
        }

        public void HideDialogue()
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }

            _isTyping = false;
            if (_dialogueRoot != null)
            {
                _dialogueRoot.SetActive(false);
            }
        }
    }
}
