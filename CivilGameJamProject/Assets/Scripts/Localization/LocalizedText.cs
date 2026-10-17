using UnityEngine;
using TMPro;

namespace CivilGame.Localization
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizedText : MonoBehaviour
    {
        [Tooltip("Key in LocalizationManager dictionary")]
        [SerializeField] private string _translationKey;

        private TextMeshProUGUI _textMesh;

        public string TranslationKey
        {
            get => _translationKey;
            set
            {
                _translationKey = value;
                UpdateText();
            }
        }

        private void Awake()
        {
            _textMesh = GetComponent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
            UpdateText();
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void Start()
        {
            UpdateText();
        }

        private void HandleLanguageChanged(GameLanguage newLang)
        {
            UpdateText();
        }

        public void UpdateText()
        {
            if (_textMesh == null) _textMesh = GetComponent<TextMeshProUGUI>();
            if (_textMesh != null && !string.IsNullOrEmpty(_translationKey))
            {
                _textMesh.text = LocalizationManager.Get(_translationKey);
            }
        }
    }
}
