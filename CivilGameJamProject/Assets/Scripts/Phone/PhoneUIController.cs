using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CivilGame.Threats;
using CivilGame.DayCycle;

namespace CivilGame.Phone
{
    public class PhoneUIController : MonoBehaviour
    {
        public static PhoneUIController Instance { get; private set; }

        [Header("Main Panels")]
        [SerializeField] private GameObject _rootPanel;
        [SerializeField] private GameObject _mainMenuPanel;
        [SerializeField] private GameObject _squadSelectPanel;
        [SerializeField] private GameObject _zoneSelectPanel;

        [Header("Main Buttons")]
        [SerializeField] private Button _reportButton;
        [SerializeField] private Button _shopButton;
        [SerializeField] private Button _outsideCloseButton;
        [SerializeField] private Button _squadBackButton;
        [SerializeField] private Button _zoneBackButton;

        [Header("Squad Buttons")]
        [SerializeField] private Button _irrigatorsBtn;
        [SerializeField] private Button _lumberjacksBtn;
        [SerializeField] private Button _firefightersBtn;
        [SerializeField] private Button _rescueHeliBtn;
        [SerializeField] private Button _climbersBtn;
        [SerializeField] private Button _diversBtn;

        [Header("Zone Buttons")]
        [SerializeField] private Button _mountainsBtn;
        [SerializeField] private Button _seaBtn;
        [SerializeField] private Button _forestBtn;
        [SerializeField] private Button _fieldBtn;

        [Header("Status Feedback")]
        [SerializeField] private TextMeshProUGUI _feedbackText;
        [SerializeField] private TextMeshProUGUI _selectedSquadPromptText;

        private SquadType _selectedSquad;
        private string _selectedSquadName;
        private Coroutine _feedbackCoroutine;

        public bool IsOpen => _rootPanel != null && _rootPanel.activeSelf;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Wire main buttons
            if (_outsideCloseButton != null) _outsideCloseButton.onClick.AddListener(ClosePhone);
            if (_reportButton != null) _reportButton.onClick.AddListener(OnReportClicked);
            if (_shopButton != null) _shopButton.onClick.AddListener(OnShopClicked);
            if (_squadBackButton != null) _squadBackButton.onClick.AddListener(ShowMainMenu);
            if (_zoneBackButton != null) _zoneBackButton.onClick.AddListener(ShowSquadSelection);

            // Wire squad buttons
            if (_irrigatorsBtn != null) _irrigatorsBtn.onClick.AddListener(() => OnSquadChosen(SquadType.Irrigators, "Irrigators"));
            if (_lumberjacksBtn != null) _lumberjacksBtn.onClick.AddListener(() => OnSquadChosen(SquadType.Lumberjacks, "Lumberjacks"));
            if (_firefightersBtn != null) _firefightersBtn.onClick.AddListener(() => OnSquadChosen(SquadType.Firefighters, "Firefighters"));
            if (_rescueHeliBtn != null) _rescueHeliBtn.onClick.AddListener(() => OnSquadChosen(SquadType.RescueHelicopter, "Rescue Helicopter"));
            if (_climbersBtn != null) _climbersBtn.onClick.AddListener(() => OnSquadChosen(SquadType.MountainClimbers, "Mountain Climbers"));
            if (_diversBtn != null) _diversBtn.onClick.AddListener(() => OnSquadChosen(SquadType.Divers, "Divers"));

            // Wire zone buttons
            if (_mountainsBtn != null) _mountainsBtn.onClick.AddListener(() => OnZoneChosen(ZoneType.Mountains));
            if (_seaBtn != null) _seaBtn.onClick.AddListener(() => OnZoneChosen(ZoneType.Sea));
            if (_forestBtn != null) _forestBtn.onClick.AddListener(() => OnZoneChosen(ZoneType.Forest));
            if (_fieldBtn != null) _fieldBtn.onClick.AddListener(() => OnZoneChosen(ZoneType.Field));
        }

        private void Start()
        {
            ClosePhone();
        }

        public void OpenPhone()
        {
            if (_rootPanel != null) _rootPanel.SetActive(true);
            ShowMainMenu();
            ClearFeedback();
        }

        public void ClosePhone()
        {
            if (_rootPanel != null) _rootPanel.SetActive(false);
            ClearFeedback();
        }

        private void ShowMainMenu()
        {
            if (_mainMenuPanel != null) _mainMenuPanel.SetActive(true);
            if (_squadSelectPanel != null) _squadSelectPanel.SetActive(false);
            if (_zoneSelectPanel != null) _zoneSelectPanel.SetActive(false);

            UpdateReportButtonState();
        }

        private void UpdateReportButtonState()
        {
            if (_reportButton == null) return;

            bool alreadyDispatched = DayCycleManager.Instance != null && DayCycleManager.Instance.HasDispatchedToday;
            _reportButton.interactable = !alreadyDispatched;

            if (alreadyDispatched)
            {
                ShowFeedback("<color=#94a3b8>[Лимит вызовов исчерпан: 1 отряд в день. Ожидайте завтрашний день.]</color>");
            }
        }

        private void OnReportClicked()
        {
            if (DayCycleManager.Instance != null && DayCycleManager.Instance.HasDispatchedToday)
            {
                ShowFeedback("<color=#f87171>Штаб сообщает: на сегодня отряд уже вызван! Ожидайте следующий день.</color>");
                return;
            }

            ShowSquadSelection();
        }

        private void OnShopClicked()
        {
            ShowFeedback("<color=#94a3b8>[Магазин закрыт на техобслуживание]</color>");
        }

        private void ShowSquadSelection()
        {
            if (_mainMenuPanel != null) _mainMenuPanel.SetActive(false);
            if (_squadSelectPanel != null) _squadSelectPanel.SetActive(true);
            if (_zoneSelectPanel != null) _zoneSelectPanel.SetActive(false);
        }

        private void OnSquadChosen(SquadType squad, string squadName)
        {
            _selectedSquad = squad;
            _selectedSquadName = squadName;

            ShowZoneSelection();
        }

        private void ShowZoneSelection()
        {
            if (_mainMenuPanel != null) _mainMenuPanel.SetActive(false);
            if (_squadSelectPanel != null) _squadSelectPanel.SetActive(false);
            if (_zoneSelectPanel != null) _zoneSelectPanel.SetActive(true);

            if (_selectedSquadPromptText != null)
            {
                _selectedSquadPromptText.text = $"Squad: <b><color=#38bdf8>{_selectedSquadName}</color></b>\nSelect destination zone:";
            }
        }

        private void OnZoneChosen(ZoneType zone)
        {
            if (DayCycleManager.Instance == null) return;

            bool success = DayCycleManager.Instance.DispatchSquad(zone, _selectedSquad, out string message);

            ShowMainMenu();

            if (success)
            {
                ShowFeedback($"<color=#38bdf8>{message}</color>");
            }
            else
            {
                ShowFeedback($"<color=#f87171>{message}</color>");
            }
        }

        private void ShowFeedback(string message)
        {
            if (_feedbackText != null)
            {
                _feedbackText.text = message;
                _feedbackText.gameObject.SetActive(true);
            }

            if (_feedbackCoroutine != null) StopCoroutine(_feedbackCoroutine);
            _feedbackCoroutine = StartCoroutine(ClearFeedbackRoutine());
        }

        private IEnumerator ClearFeedbackRoutine()
        {
            yield return new WaitForSeconds(3.5f);
            ClearFeedback();
        }

        private void ClearFeedback()
        {
            if (_feedbackText != null)
            {
                _feedbackText.text = "";
            }
        }
    }
}
