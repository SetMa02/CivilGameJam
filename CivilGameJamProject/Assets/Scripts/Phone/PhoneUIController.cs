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
        [SerializeField] private GameObject _shopPanel;
        [SerializeField] private GameObject _shopTargetZonePanel;

        [Header("Main Buttons")]
        [SerializeField] private Button _reportButton;
        [SerializeField] private Button _shopButton;
        [SerializeField] private Button _outsideCloseButton;
        [SerializeField] private Button _squadBackButton;
        [SerializeField] private Button _zoneBackButton;
        [SerializeField] private Button _shopBackButton;
        [SerializeField] private Button _shopZoneBackButton;

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

        [Header("Shop Buttons")]
        [SerializeField] private Button _buySecondCallBtn;    // $200
        [SerializeField] private Button _buyRestoreAreaBtn;   // $250
        [SerializeField] private Button _buyPauseGrowthBtn;   // $350
        [SerializeField] private Button _buyRollbackBtn;      // $500

        [Header("Shop Dynamic Zone Container")]
        [SerializeField] private Transform _shopZoneButtonsContainer;
        [SerializeField] private TextMeshProUGUI _shopZonePromptText;

        [Header("Status Feedback")]
        [SerializeField] private TextMeshProUGUI _feedbackText;
        [SerializeField] private TextMeshProUGUI _selectedSquadPromptText;

        public const int PriceSecondCall = 200;
        public const int PriceRestoreArea = 250;
        public const int PricePauseGrowth = 350;
        public const int PriceRollback = 500;

        private enum ShopActionType
        {
            None,
            RestoreDestroyed,
            PauseThreatGrowth,
            RollbackThreat
        }

        private ShopActionType _currentShopAction = ShopActionType.None;
        private SquadType _selectedSquad;
        private string _selectedSquadName;
        private Coroutine _feedbackCoroutine;

        private float _lastOpenedTime = -1f;

        public bool IsOpen => _rootPanel != null && _rootPanel.activeSelf;
        public event System.Action OnPhoneOpened;
        public event System.Action OnPhoneClosed;
        public event System.Action<ZoneType, SquadType> OnSquadDispatchedSuccess;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            WireListeners();
        }

        private void WireListeners()
        {
            // Wire main buttons
            if (_outsideCloseButton != null) { _outsideCloseButton.onClick.RemoveAllListeners(); _outsideCloseButton.onClick.AddListener(OnBackdropClicked); }
            if (_reportButton != null) { _reportButton.onClick.RemoveAllListeners(); _reportButton.onClick.AddListener(OnReportClicked); }
            if (_shopButton != null) { _shopButton.onClick.RemoveAllListeners(); _shopButton.onClick.AddListener(OnShopClicked); }
            if (_squadBackButton != null) { _squadBackButton.onClick.RemoveAllListeners(); _squadBackButton.onClick.AddListener(ShowMainMenu); }
            if (_zoneBackButton != null) { _zoneBackButton.onClick.RemoveAllListeners(); _zoneBackButton.onClick.AddListener(ShowSquadSelection); }
            if (_shopBackButton != null) { _shopBackButton.onClick.RemoveAllListeners(); _shopBackButton.onClick.AddListener(ShowMainMenu); }
            if (_shopZoneBackButton != null) { _shopZoneBackButton.onClick.RemoveAllListeners(); _shopZoneBackButton.onClick.AddListener(ShowShop); }

            // Wire squad buttons
            if (_irrigatorsBtn != null) { _irrigatorsBtn.onClick.RemoveAllListeners(); _irrigatorsBtn.onClick.AddListener(() => OnSquadChosen(SquadType.Irrigators, "Irrigators")); }
            if (_lumberjacksBtn != null) { _lumberjacksBtn.onClick.RemoveAllListeners(); _lumberjacksBtn.onClick.AddListener(() => OnSquadChosen(SquadType.Lumberjacks, "Lumberjacks")); }
            if (_firefightersBtn != null) { _firefightersBtn.onClick.RemoveAllListeners(); _firefightersBtn.onClick.AddListener(() => OnSquadChosen(SquadType.Firefighters, "Firefighters")); }
            if (_rescueHeliBtn != null) { _rescueHeliBtn.onClick.RemoveAllListeners(); _rescueHeliBtn.onClick.AddListener(() => OnSquadChosen(SquadType.RescueHelicopter, "Rescue Helicopter")); }
            if (_climbersBtn != null) { _climbersBtn.onClick.RemoveAllListeners(); _climbersBtn.onClick.AddListener(() => OnSquadChosen(SquadType.MountainClimbers, "Mountain Climbers")); }
            if (_diversBtn != null) { _diversBtn.onClick.RemoveAllListeners(); _diversBtn.onClick.AddListener(() => OnSquadChosen(SquadType.Divers, "Divers")); }

            // Wire zone buttons
            if (_mountainsBtn != null) { _mountainsBtn.onClick.RemoveAllListeners(); _mountainsBtn.onClick.AddListener(() => OnZoneChosen(ZoneType.Mountains)); }
            if (_seaBtn != null) { _seaBtn.onClick.RemoveAllListeners(); _seaBtn.onClick.AddListener(() => OnZoneChosen(ZoneType.Sea)); }
            if (_forestBtn != null) { _forestBtn.onClick.RemoveAllListeners(); _forestBtn.onClick.AddListener(() => OnZoneChosen(ZoneType.Forest)); }
            if (_fieldBtn != null) { _fieldBtn.onClick.RemoveAllListeners(); _fieldBtn.onClick.AddListener(() => OnZoneChosen(ZoneType.Field)); }

            // Wire shop purchase buttons
            if (_buySecondCallBtn != null) { _buySecondCallBtn.onClick.RemoveAllListeners(); _buySecondCallBtn.onClick.AddListener(OnBuySecondCall); }
            if (_buyRestoreAreaBtn != null) { _buyRestoreAreaBtn.onClick.RemoveAllListeners(); _buyRestoreAreaBtn.onClick.AddListener(OnBuyRestoreArea); }
            if (_buyPauseGrowthBtn != null) { _buyPauseGrowthBtn.onClick.RemoveAllListeners(); _buyPauseGrowthBtn.onClick.AddListener(OnBuyPauseGrowth); }
            if (_buyRollbackBtn != null) { _buyRollbackBtn.onClick.RemoveAllListeners(); _buyRollbackBtn.onClick.AddListener(OnBuyRollback); }
        }

        public void TriggerBuySecondCall() => OnBuySecondCall();
        public void TriggerBuyRestoreArea() => OnBuyRestoreArea();
        public void TriggerBuyPauseGrowth() => OnBuyPauseGrowth();
        public void TriggerBuyRollback() => OnBuyRollback();

        private void OnBackdropClicked()
        {
            if (Time.unscaledTime - _lastOpenedTime < 0.15f) return;
            ClosePhone();
        }

        private void Start()
        {
            ClosePhone();
        }

        public void OpenPhone()
        {
            _lastOpenedTime = Time.unscaledTime;
            if (_rootPanel != null) _rootPanel.SetActive(true);
            ShowMainMenu();
            ClearFeedback();
            OnPhoneOpened?.Invoke();
        }

        public void ClosePhone()
        {
            if (_rootPanel != null) _rootPanel.SetActive(false);
            ClearFeedback();
            OnPhoneClosed?.Invoke();
        }

        public void ShowMainMenu()
        {
            if (_mainMenuPanel != null) _mainMenuPanel.SetActive(true);
            if (_squadSelectPanel != null) _squadSelectPanel.SetActive(false);
            if (_zoneSelectPanel != null) _zoneSelectPanel.SetActive(false);
            if (_shopPanel != null) _shopPanel.SetActive(false);
            if (_shopTargetZonePanel != null) _shopTargetZonePanel.SetActive(false);

            UpdateReportButtonState();
        }

        private void UpdateReportButtonState()
        {
            if (_reportButton == null) return;

            bool limitReached = DayCycleManager.Instance != null && DayCycleManager.Instance.HasDispatchedToday;
            _reportButton.interactable = !limitReached;

            if (limitReached)
            {
                int max = DayCycleManager.Instance?.MaxDispatchesPerDay ?? 1;
                ShowFeedback(CivilGame.Localization.LocalizationManager.Get("phone.limit_reached", max));
            }
        }

        public void OnReportClicked()
        {
            if (DayCycleManager.Instance != null && DayCycleManager.Instance.HasDispatchedToday)
            {
                ShowFeedback(CivilGame.Localization.LocalizationManager.Get("phone.limit_reached_alert"));
                return;
            }

            ShowSquadSelection();
        }

        public void ShowSquadSelection()
        {
            if (_mainMenuPanel != null) _mainMenuPanel.SetActive(false);
            if (_squadSelectPanel != null) _squadSelectPanel.SetActive(true);
            if (_zoneSelectPanel != null) _zoneSelectPanel.SetActive(false);
            if (_shopPanel != null) _shopPanel.SetActive(false);
            if (_shopTargetZonePanel != null) _shopTargetZonePanel.SetActive(false);
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
            if (_shopPanel != null) _shopPanel.SetActive(false);
            if (_shopTargetZonePanel != null) _shopTargetZonePanel.SetActive(false);

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
                OnSquadDispatchedSuccess?.Invoke(zone, _selectedSquad);
            }
            else
            {
                ShowFeedback($"<color=#f87171>{message}</color>");
            }
        }

        #region Shop

        public void OnShopClicked()
        {
            ShowShop();
        }

        public void ShowShop()
        {
            if (_mainMenuPanel != null) _mainMenuPanel.SetActive(false);
            if (_squadSelectPanel != null) _squadSelectPanel.SetActive(false);
            if (_zoneSelectPanel != null) _zoneSelectPanel.SetActive(false);
            if (_shopPanel != null) _shopPanel.SetActive(true);
            if (_shopTargetZonePanel != null) _shopTargetZonePanel.SetActive(false);

            UpdateShopButtonsState();
        }

        private void UpdateShopButtonsState()
        {
            var dcm = DayCycleManager.Instance != null ? DayCycleManager.Instance : FindAnyObjectByType<DayCycleManager>();
            if (dcm == null) return;

            var ms = MoneySystem.Instance != null ? MoneySystem.Instance : FindAnyObjectByType<MoneySystem>();
            int currentMoney = ms != null ? ms.money : 0;

            // 1. Second report call ($200): disabled if already unlocked 2 dispatches today, or not enough money
            if (_buySecondCallBtn != null)
            {
                bool canBuy = currentMoney >= PriceSecondCall && dcm.MaxDispatchesPerDay < 2;
                _buySecondCallBtn.interactable = canBuy;
            }

            // 2. Restore destroyed area ($250): disabled if NO destroyed regions or not enough money
            if (_buyRestoreAreaBtn != null)
            {
                var destroyed = dcm.GetDestroyedZones();
                bool hasDestroyed = destroyed.Count > 0;
                _buyRestoreAreaBtn.interactable = hasDestroyed && currentMoney >= PriceRestoreArea;
            }

            // 3. Pause threat growth for one day ($350): disabled if NO threatened regions or not enough money
            if (_buyPauseGrowthBtn != null)
            {
                var threatened = dcm.GetThreatenedZones();
                bool hasThreatened = threatened.Count > 0;
                _buyPauseGrowthBtn.interactable = hasThreatened && currentMoney >= PricePauseGrowth;
            }

            // 4. Roll back threat level by 1 level ($500): disabled if NO active threats or not enough money
            if (_buyRollbackBtn != null)
            {
                var threatened = dcm.GetThreatenedZones();
                bool hasThreatened = threatened.Count > 0;
                _buyRollbackBtn.interactable = hasThreatened && currentMoney >= PriceRollback;
            }
        }

        private void OnBuySecondCall()
        {
            var dcm = DayCycleManager.Instance != null ? DayCycleManager.Instance : FindAnyObjectByType<DayCycleManager>();
            var ms = MoneySystem.Instance != null ? MoneySystem.Instance : FindAnyObjectByType<MoneySystem>();
            if (ms == null || dcm == null) return;

            if (ms.SpendMoney(PriceSecondCall))
            {
                dcm.EnableSecondReportCall();
                UpdateShopButtonsState();
                ShowFeedback(CivilGame.Localization.LocalizationManager.Get("phone.shop_success_call"));
            }
            else
            {
                ShowFeedback(CivilGame.Localization.LocalizationManager.Get("phone.shop_insufficient_funds", PriceSecondCall));
            }
        }

        private void OnBuyRestoreArea()
        {
            var dcm = DayCycleManager.Instance != null ? DayCycleManager.Instance : FindAnyObjectByType<DayCycleManager>();
            if (dcm == null) return;
            var destroyed = dcm.GetDestroyedZones();
            if (destroyed.Count == 0)
            {
                ShowFeedback(CivilGame.Localization.LocalizationManager.Get("phone.shop_no_destroyed"));
                return;
            }

            _currentShopAction = ShopActionType.RestoreDestroyed;
            OpenShopTargetZoneSelection(CivilGame.Localization.LocalizationManager.Get("phone.shop_prompt_restore"), destroyed);
        }

        private void OnBuyPauseGrowth()
        {
            var dcm = DayCycleManager.Instance != null ? DayCycleManager.Instance : FindAnyObjectByType<DayCycleManager>();
            if (dcm == null) return;
            var threatened = dcm.GetThreatenedZones();
            if (threatened.Count == 0)
            {
                ShowFeedback(CivilGame.Localization.LocalizationManager.Get("phone.shop_no_threatened"));
                return;
            }

            _currentShopAction = ShopActionType.PauseThreatGrowth;
            OpenShopTargetZoneSelection(CivilGame.Localization.LocalizationManager.Get("phone.shop_prompt_pause"), threatened);
        }

        private void OnBuyRollback()
        {
            var dcm = DayCycleManager.Instance != null ? DayCycleManager.Instance : FindAnyObjectByType<DayCycleManager>();
            if (dcm == null) return;
            var threatened = dcm.GetThreatenedZones();
            if (threatened.Count == 0)
            {
                ShowFeedback(CivilGame.Localization.LocalizationManager.Get("phone.shop_no_threatened"));
                return;
            }

            _currentShopAction = ShopActionType.RollbackThreat;
            OpenShopTargetZoneSelection(CivilGame.Localization.LocalizationManager.Get("phone.shop_prompt_rollback"), threatened);
        }

        private void OpenShopTargetZoneSelection(string prompt, List<ZoneModel> zones)
        {
            if (_shopPanel != null) _shopPanel.SetActive(false);
            if (_shopTargetZonePanel != null) _shopTargetZonePanel.SetActive(true);

            if (_shopZonePromptText != null)
            {
                _shopZonePromptText.text = prompt;
            }

            PopulateShopZoneButtons(zones);
        }

        private void PopulateShopZoneButtons(List<ZoneModel> zones)
        {
            if (_shopZoneButtonsContainer == null) return;

            // Clear previous dynamic zone buttons (preserve prompt and back button)
            for (int i = _shopZoneButtonsContainer.childCount - 1; i >= 0; i--)
            {
                var child = _shopZoneButtonsContainer.GetChild(i);
                if (child.name.StartsWith("ZoneBtn_"))
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        DestroyImmediate(child.gameObject);
                    }
                    else
                    {
                        Destroy(child.gameObject);
                    }
#else
                    Destroy(child.gameObject);
#endif
                }
            }

            Sprite roundedSprite = _buySecondCallBtn != null && _buySecondCallBtn.image != null ? _buySecondCallBtn.image.sprite : null;
            TMP_FontAsset fontAsset = _shopZonePromptText != null ? _shopZonePromptText.font : null;

            int insertIndex = 1; // Insert right after prompt (index 0) and before back button

            foreach (var z in zones)
            {
                var zoneCapture = z;
                GameObject bGo = new GameObject($"ZoneBtn_{zoneCapture.zoneType}", typeof(RectTransform));
                bGo.transform.SetParent(_shopZoneButtonsContainer, false);
                bGo.transform.SetSiblingIndex(insertIndex++);

                var bRt = bGo.GetComponent<RectTransform>();
                bRt.anchorMin = new Vector2(0.5f, 0.5f);
                bRt.anchorMax = new Vector2(0.5f, 0.5f);
                bRt.pivot = new Vector2(0.5f, 0.5f);
                bRt.sizeDelta = new Vector2(480, 54);

                var bImg = bGo.AddComponent<UnityEngine.UI.Image>();
                if (roundedSprite != null)
                {
                    bImg.sprite = roundedSprite;
                    bImg.type = UnityEngine.UI.Image.Type.Sliced;
                }
                bImg.color = new Color(0.16f, 0.24f, 0.35f);

                var btn = bGo.AddComponent<UnityEngine.UI.Button>();
                var colors = btn.colors;
                colors.normalColor = new Color(0.16f, 0.24f, 0.35f);
                colors.highlightedColor = new Color(0.24f, 0.36f, 0.52f);
                colors.pressedColor = new Color(0.10f, 0.16f, 0.24f);
                btn.colors = colors;

                var le = bGo.AddComponent<UnityEngine.UI.LayoutElement>();
                le.preferredHeight = 54;
                le.minHeight = 54;

                GameObject tGo = new GameObject("Label", typeof(RectTransform));
                tGo.transform.SetParent(bGo.transform, false);
                var tTmp = tGo.AddComponent<TextMeshProUGUI>();
                if (fontAsset != null) tTmp.font = fontAsset;

                string zName = CivilGame.Localization.LocalizationManager.GetZoneName(zoneCapture.zoneType);
                string extraInfo = "";
                if (_currentShopAction == ShopActionType.RestoreDestroyed)
                {
                    extraInfo = $" {CivilGame.Localization.LocalizationManager.Get("status.tag_destroyed")}";
                }
                else if (_currentShopAction == ShopActionType.PauseThreatGrowth && zoneCapture.activeThreat != null)
                {
                    string threatName = CivilGame.Localization.LocalizationManager.GetThreatName(zoneCapture.activeThreat.definition.type);
                    extraInfo = $" [{threatName}]";
                }
                else if (_currentShopAction == ShopActionType.RollbackThreat && zoneCapture.activeThreat != null)
                {
                    string threatName = CivilGame.Localization.LocalizationManager.GetThreatName(zoneCapture.activeThreat.definition.type);
                    string stagePrefix = CivilGame.Localization.LocalizationManager.Get("status.stage_prefix", zoneCapture.activeThreat.currentStage);
                    extraInfo = $" [{threatName} - {stagePrefix}]";
                }

                tTmp.text = $"{zName}{extraInfo}";
                tTmp.fontSize = 17;
                tTmp.fontStyle = FontStyles.Bold;
                tTmp.alignment = TextAlignmentOptions.Center;
                tTmp.color = Color.white;
                tTmp.raycastTarget = false;

                var tRt = tGo.GetComponent<RectTransform>();
                tRt.anchorMin = Vector2.zero;
                tRt.anchorMax = Vector2.one;
                tRt.offsetMin = Vector2.zero;
                tRt.offsetMax = Vector2.zero;
                tRt.pivot = new Vector2(0.5f, 0.5f);

                btn.onClick.AddListener(() => OnShopZoneSelected(zoneCapture.zoneType));
            }

            // Force layout rebuild
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_shopZoneButtonsContainer.GetComponent<RectTransform>());
        }

        private void OnShopZoneSelected(ZoneType zoneType)
        {
            if (DayCycleManager.Instance == null || MoneySystem.Instance == null) return;

            string zoneName = CivilGame.Localization.LocalizationManager.GetZoneName(zoneType);

            if (_currentShopAction == ShopActionType.RestoreDestroyed)
            {
                if (MoneySystem.Instance.SpendMoney(PriceRestoreArea))
                {
                    DayCycleManager.Instance.RestoreDestroyedZone(zoneType);
                    ShowShop();
                    ShowFeedback(CivilGame.Localization.LocalizationManager.Get("phone.shop_success_restore", zoneName));
                }
                else
                {
                    ShowFeedback(CivilGame.Localization.LocalizationManager.Get("phone.shop_insufficient_funds", PriceRestoreArea));
                }
            }
            else if (_currentShopAction == ShopActionType.PauseThreatGrowth)
            {
                if (MoneySystem.Instance.SpendMoney(PricePauseGrowth))
                {
                    DayCycleManager.Instance.PauseThreatGrowthForZone(zoneType);
                    ShowShop();
                    ShowFeedback(CivilGame.Localization.LocalizationManager.Get("phone.shop_success_pause", zoneName));
                }
                else
                {
                    ShowFeedback(CivilGame.Localization.LocalizationManager.Get("phone.shop_insufficient_funds", PricePauseGrowth));
                }
            }
            else if (_currentShopAction == ShopActionType.RollbackThreat)
            {
                if (MoneySystem.Instance.SpendMoney(PriceRollback))
                {
                    DayCycleManager.Instance.RollbackThreatLevel(zoneType);
                    ShowShop();
                    ShowFeedback(CivilGame.Localization.LocalizationManager.Get("phone.shop_success_rollback", zoneName));
                }
                else
                {
                    ShowFeedback(CivilGame.Localization.LocalizationManager.Get("phone.shop_insufficient_funds", PriceRollback));
                }
            }

            _currentShopAction = ShopActionType.None;
        }

        #endregion

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

