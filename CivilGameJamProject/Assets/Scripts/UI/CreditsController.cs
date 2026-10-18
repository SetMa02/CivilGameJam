using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

namespace CivilGame.UI
{
    [System.Serializable]
    public class CreditEntry
    {
        [Tooltip("Должность / Роль (например: Lead Programmer, 2D Artist, Sound Designer)")]
        public string role;

        [Tooltip("Имя / Никнейм создателя")]
        public string name;

        public CreditEntry() {}
        public CreditEntry(string r, string n)
        {
            role = r;
            name = n;
        }
    }

    public class CreditsController : MonoBehaviour
    {
        [Header("Scene Navigation")]
        [Tooltip("Название сцены главного меню")]
        [SerializeField] private string _mainMenuSceneName = "MainMenu";

        [Header("Speed & Control")]
        [Tooltip("Базовая скорость движения титров снизу вверх (пикселей в секунду)")]
        [SerializeField] private float _scrollSpeed = 65f;

        [Tooltip("Множитель ускорения при удержании Пробела / ЛКМ")]
        [SerializeField] private float _fastForwardMultiplier = 3.5f;

        [Tooltip("Позволить игроку нажать Escape для мгновенного появления кнопки меню")]
        [SerializeField] private bool _allowSkipWithEscape = true;

        [Header("UI References")]
        [Tooltip("Контейнер всего едущего блока титров")]
        [SerializeField] private RectTransform _creditsContent;

        [Tooltip("Текстовый компонент списка титров")]
        [SerializeField] private TextMeshProUGUI _creditsListText;

        [Tooltip("Текстовый компонент благодарности за игру")]
        [SerializeField] private TextMeshProUGUI _thanksText;

        [Tooltip("Текст на кнопке возврата в главное меню")]
        [SerializeField] private TextMeshProUGUI _mainMenuButtonText;

        [Tooltip("Финальный блок: Thanks for Playing + Кнопка Main Menu")]
        [SerializeField] private CanvasGroup _finalPanelCanvasGroup;

        [Tooltip("Кнопка возврата в главное меню")]
        [SerializeField] private Button _mainMenuButton;

        [Tooltip("Заголовок игры в начале титров")]
        [SerializeField] private string _gameTitle = "LIGHTHOUSE WATCHER";

        [Header("Credits List")]
        [Tooltip("Список должностей и имён (легко добавлять/редактировать в Инспекторе)")]
        [SerializeField] private List<CreditEntry> _credits = new List<CreditEntry>()
        {
            new CreditEntry("Game Design & Lead", "Your Name / Team"),
            new CreditEntry("Programming & Mechanics", "Lead Programmer"),
            new CreditEntry("Additional Programming", "Gameplay Programmer"),
            new CreditEntry("2D Art & Concept", "Lead Artist"),
            new CreditEntry("Character Design & UI Art", "2D Artist"),
            new CreditEntry("Animation & VFX", "Animator"),
            new CreditEntry("Sound Design & SFX", "Audio Designer"),
            new CreditEntry("Music & Ambiance", "Composer"),
            new CreditEntry("Writing & Narrative", "Writer / Scenario"),
            new CreditEntry("QA & Playtesting", "Testers Team"),
            new CreditEntry("Special Thanks", "Game Jam Organizers & Players")
        };

        private bool _isFinished = false;
        private float _contentStartPosY;
        private float _contentTargetEndY;

        private void Awake()
        {
            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.AddListener(ReturnToMainMenu);
            }

            if (_finalPanelCanvasGroup != null)
            {
                _finalPanelCanvasGroup.alpha = 0f;
                _finalPanelCanvasGroup.interactable = false;
                _finalPanelCanvasGroup.blocksRaycasts = false;
            }

            if (_thanksText != null)
            {
                string title = CivilGame.Localization.LocalizationManager.Get("credits.thanks_title");
                string desc = CivilGame.Localization.LocalizationManager.Get("credits.thanks_desc");
                _thanksText.text = $"<size=34><color=#fbbf24><b>{title}</b></color></size>\n<size=19><color=#94a3b8>{"You fulfilled your duty"}</color></size>";
            }

            if (_mainMenuButtonText != null)
            {
                _mainMenuButtonText.text = CivilGame.Localization.LocalizationManager.Get("credits.main_menu");
            }

            BuildCreditsText();
        }

        private void Start()
        {
            Time.timeScale = 1f;

            StartCoroutine(CreditsScrollRoutine());
        }

        private void OnDestroy()
        {
            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
            }
        }

        private void Update()
        {
            if (_allowSkipWithEscape && !_isFinished)
            {
                if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    StopAllCoroutines();
                    InstantShowFinalPanel();
                }
            }
        }

        public void BuildCreditsText()
        {
            if (_creditsListText == null) return;

            var sb = new System.Text.StringBuilder();

            // Заголовок игры
            sb.AppendLine($"<size=38><color=#fbbf24><b>{_gameTitle}</b></color></size>");
            sb.AppendLine("<size=18><color=#94a3b8>Civil Game Jam 2026</color></size>");
            sb.AppendLine("\n\n");

            // Имена и роли
            foreach (var entry in _credits)
            {
                sb.AppendLine($"<size=18><color=#94a3b8>{entry.role.ToUpper()}</color></size>");
                sb.AppendLine($"<size=26><color=#f8fafc><b>{entry.name}</b></color></size>");
                sb.AppendLine("\n");
            }

            _creditsListText.text = sb.ToString();
        }

        private IEnumerator CreditsScrollRoutine()
        {
            yield return null; // ждем обновления RectTransform
            Canvas.ForceUpdateCanvases();

            // Получаем фактическую высоту вьюпорта в UI-единицах Canvas
            RectTransform viewportRt = _creditsContent != null ? _creditsContent.parent as RectTransform : null;
            float viewportHeight = viewportRt != null ? viewportRt.rect.height : 1080f;

            // Вычисляем фактическую высоту текста
            float contentHeight = _creditsListText != null ? _creditsListText.preferredHeight : 1500f;

            // Устанавливаем размер контейнера под размер текста
            if (_creditsContent != null)
            {
                _creditsContent.sizeDelta = new Vector2(_creditsContent.sizeDelta.x, contentHeight);

                // Pivot в (0.5, 0), Anchor в (0.5, 0):
                // При y = 0 низ текста находится у нижней границы вьюпорта.
                // Чтобы верх текста (первая строка) был СТРОГО под нижней границей экрана:
                // смещаем на всю высоту текста вниз: y = -contentHeight (плюс небольшой отступ)
                _creditsContent.anchoredPosition = new Vector2(0f, -contentHeight - 50f);
            }

            // Конечная точка: текст должен полностью уехать за верхнюю границу вьюпорта
            // Низ текста должен подняться выше верхней границы вьюпорта: y = viewportHeight + 100f
            _contentTargetEndY = viewportHeight + 150f;

            float currentY = _creditsContent != null ? _creditsContent.anchoredPosition.y : (-contentHeight - 50f);

            while (currentY < _contentTargetEndY)
            {
                float speed = _scrollSpeed;

                // Ускорение при удержании Пробела или клика ЛКМ
                bool isFast = false;
                if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed) isFast = true;
                if (Mouse.current != null && Mouse.current.leftButton.isPressed) isFast = true;

                if (isFast) speed *= _fastForwardMultiplier;

                currentY += speed * Time.deltaTime;

                if (_creditsContent != null)
                {
                    _creditsContent.anchoredPosition = new Vector2(0f, currentY);
                }

                yield return null;
            }

            // Плавное появление финального блока
            yield return StartCoroutine(FadeInFinalPanelRoutine());
        }

        private IEnumerator FadeInFinalPanelRoutine()
        {
            _isFinished = true;

            if (_finalPanelCanvasGroup != null)
            {
                _finalPanelCanvasGroup.blocksRaycasts = true;
                _finalPanelCanvasGroup.interactable = true;

                float duration = 1.2f;
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    _finalPanelCanvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
                    yield return null;
                }
                _finalPanelCanvasGroup.alpha = 1f;
            }
        }

        private void InstantShowFinalPanel()
        {
            _isFinished = true;
            if (_creditsContent != null)
            {
                _creditsContent.anchoredPosition = new Vector2(0f, _contentTargetEndY);
            }

            if (_finalPanelCanvasGroup != null)
            {
                _finalPanelCanvasGroup.alpha = 1f;
                _finalPanelCanvasGroup.interactable = true;
                _finalPanelCanvasGroup.blocksRaycasts = true;
            }
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(_mainMenuSceneName);
        }
    }
}
