using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CivilGame.UI
{
    [System.Serializable]
    public class DialogueLineData
    {
        [Tooltip("Optional custom portrait for this replica. If null, the default Lucy sprite will be used.")]
        public Sprite portrait;

        [Tooltip("Voiceover audio clip that plays when this replica appears.")]
        public AudioClip voiceClip;
    }

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

        [Header("Audio & Portrait Settings")]
        [Tooltip("Default sprite used when custom portrait is null.")]
        [SerializeField] private Sprite _defaultPortrait;
        [Tooltip("AudioSource used to play line voice acting.")]
        [SerializeField] private AudioSource _voiceAudioSource;

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

            if (_defaultPortrait == null && _characterPortrait != null)
            {
                _defaultPortrait = _characterPortrait.sprite;
            }

            if (_voiceAudioSource == null)
            {
                _voiceAudioSource = GetComponent<AudioSource>();
                if (_voiceAudioSource == null)
                {
                    _voiceAudioSource = gameObject.AddComponent<AudioSource>();
                    _voiceAudioSource.playOnAwake = false;
                }
            }

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
            ShowDialogue(speaker, message, onComplete, null, null, true);
        }

        public void ShowDialogue(string speaker, string message, Action onComplete, DialogueLineData lineData, bool showUiPortrait = true)
        {
            Sprite p = lineData != null ? lineData.portrait : null;
            AudioClip v = lineData != null ? lineData.voiceClip : null;
            ShowDialogue(speaker, message, onComplete, p, v, showUiPortrait);
        }

        public void ShowDialogue(string speaker, string message, Action onComplete, Sprite customPortrait, AudioClip voiceClip, bool showUiPortrait = true)
        {
            if (_dialogueRoot != null)
            {
                _dialogueRoot.SetActive(true);
            }

            if (_characterPortrait != null)
            {
                _characterPortrait.gameObject.SetActive(showUiPortrait);
                if (showUiPortrait)
                {
                    if (customPortrait != null)
                    {
                        _characterPortrait.sprite = customPortrait;
                    }
                    else if (_defaultPortrait != null)
                    {
                        _characterPortrait.sprite = _defaultPortrait;
                    }
                }
            }

            if (_voiceAudioSource != null)
            {
                _voiceAudioSource.Stop();
                if (voiceClip != null)
                {
                    _voiceAudioSource.clip = voiceClip;
                    _voiceAudioSource.Play();
                }
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

            if (_voiceAudioSource != null && _voiceAudioSource.isPlaying)
            {
                _voiceAudioSource.Stop();
            }

            _isTyping = false;
            if (_dialogueRoot != null)
            {
                _dialogueRoot.SetActive(false);
            }
        }
    }
}
