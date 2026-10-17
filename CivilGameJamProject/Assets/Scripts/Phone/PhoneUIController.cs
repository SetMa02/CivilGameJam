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
            if (_shopBackButton != null) _shopBackButton.onClick.AddListener(ShowMainMenu);
            if (_shopZoneBackButton != null) _shopZoneBackButton.onClick.AddListener(ShowShop);

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

            // Wire shop purchase buttons
            if (_buySecondCallBtn != null) _buySecondCallBtn.onClick.AddListener(OnBuySecondCall);
            if (_buyRestoreAreaBtn != null) _buyRestoreAreaBtn.onClick.AddListener(OnBuyRestoreArea);
            if (_buyPauseGrowthBtn != null) _buyPauseGrowthBtn.onClick.AddListener(OnBuyPauseGrowth);
            if (_buyRollbackBtn != null) _buyRollbackBtn.onClick.AddListener(OnBuyRollback);
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
                ShowFeedback($"<color=#94a3b8>[Лимит вызовов ({max}/{max}) исчерпан. Ожидайте завтра или откройте SHOP.]</color>");
            }
        }

        private void OnReportClicked()
        {
            if (DayCycleManager.Instance != null && DayCycleManager.Instance.HasDispatchedToday)
            {
                ShowFeedback("<color=#f87171>Штаб сообщает: на сегодня лимит вызовов исчерпан! Ожидайте следующий день или используйте SHOP.</color>");
                return;
            }

            ShowSquadSelection();
        }

        private void ShowSquadSelection()
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
            }
            else
            {
                ShowFeedback($"<color=#f87171>{message}</color>");
            }
        }

        #region Shop

        private void OnShopClicked()
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
            if (MoneySystem.Instance == null || DayCycleManager.Instance == null) return;

            if (MoneySystem.Instance.SpendMoney(PriceSecondCall))
            {
                DayCycleManager.Instance.EnableSecondReportCall();
                UpdateShopButtonsState();
                ShowFeedback("<color=#4ade80>Успешно! Дополнительный вызов отряда разблокирован на сегодня.</color>");
            }
            else
            {
                ShowFeedback("<color=#f87171>Недостаточно средств ($200)!</color>");
            }
        }

        private void OnBuyRestoreArea()
        {
            if (DayCycleManager.Instance == null) return;
            var destroyed = DayCycleManager.Instance.GetDestroyedZones();
            if (destroyed.Count == 0)
            {
                ShowFeedback("<color=#94a3b8>Нет разрушенных регионов для восстановления.</color>");
                return;
            }

            _currentShopAction = ShopActionType.RestoreDestroyed;
            OpenShopTargetZoneSelection("Select DESTROYED zone to restore ($250):", destroyed);
        }

        private void OnBuyPauseGrowth()
        {
            if (DayCycleManager.Instance == null) return;
            var threatened = DayCycleManager.Instance.GetThreatenedZones();
            if (threatened.Count == 0)
            {
                ShowFeedback("<color=#94a3b8>Сейчас нет регионов под угрозой.</color>");
                return;
            }

            _currentShopAction = ShopActionType.PauseThreatGrowth;
            OpenShopTargetZoneSelection("Select THREATENED zone to freeze for 1 day ($350):", threatened);
        }

        private void OnBuyRollback()
        {
            if (DayCycleManager.Instance == null) return;
            var threatened = DayCycleManager.Instance.GetThreatenedZones();
            if (threatened.Count == 0)
            {
                ShowFeedback("<color=#94a3b8>Сейчас нет регионов под угрозой.</color>");
                return;
            }

            _currentShopAction = ShopActionType.RollbackThreat;
            OpenShopTargetZoneSelection("Select THREATENED zone to roll back by 1 ($500):", threatened);
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

            // Clear previous dynamic buttons
            for (int i = _shopZoneButtonsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_shopZoneButtonsContainer.GetChild(i).gameObject);
            }

            var roundedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/RoundedRect.png");
            var fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

            foreach (var z in zones)
            {
                var zoneCapture = z;
                GameObject bGo = new GameObject($"ZoneBtn_{zoneCapture.zoneType}", typeof(RectTransform));
                bGo.transform.SetParent(_shopZoneButtonsContainer, false);

                var bImg = bGo.AddComponent<Image>();
                if (roundedSprite != null)
                {
                    bImg.sprite = roundedSprite;
                    bImg.type = Image.Type.Sliced;
                }
                bImg.color = new Color(0.16f, 0.22f, 0.32f);

                var btn = bGo.AddComponent<Button>();
                var colors = btn.colors;
                colors.normalColor = new Color(0.16f, 0.22f, 0.32f);
                colors.highlightedColor = new Color(0.24f, 0.34f, 0.48f);
                colors.pressedColor = new Color(0.10f, 0.14f, 0.22f);
                btn.colors = colors;

                var le = bGo.AddComponent<LayoutElement>();
                le.preferredHeight = 52;
                le.minHeight = 52;

                GameObject tGo = new GameObject("Label", typeof(RectTransform));
                tGo.transform.SetParent(bGo.transform, false);
                var tTmp = tGo.AddComponent<TextMeshProUGUI>();
                if (fontAsset != null) tTmp.font = fontAsset;

                string extraInfo = "";
                if (_currentShopAction == ShopActionType.RestoreDestroyed)
                {
                    extraInfo = " [РАЗРУШЕН]";
                }
                else if (_currentShopAction == ShopActionType.PauseThreatGrowth && zoneCapture.activeThreat != null)
                {
                    extraInfo = $" [{zoneCapture.activeThreat.definition.displayName}]";
                }
                else if (_currentShopAction == ShopActionType.RollbackThreat && zoneCapture.activeThreat != null)
                {
                    extraInfo = $" [{zoneCapture.activeThreat.definition.displayName} - Этап {zoneCapture.activeThreat.currentStage}]";
                }

                tTmp.text = $"{zoneCapture.zoneName}{extraInfo}";
                tTmp.fontSize = 17;
                tTmp.fontStyle = FontStyles.Bold;
                tTmp.alignment = TextAlignmentOptions.Center;
                tTmp.color = Color.white;
                tTmp.raycastTarget = false;

                var tRt = tGo.GetComponent<RectTransform>();
                tRt.anchorMin = Vector2.zero;
                tRt.anchorMax = Vector2.one;
                tRt.sizeDelta = Vector2.zero;

                btn.onClick.AddListener(() => OnShopZoneSelected(zoneCapture.zoneType));
            }
        }

        private void OnShopZoneSelected(ZoneType zoneType)
        {
            if (DayCycleManager.Instance == null || MoneySystem.Instance == null) return;

            if (_currentShopAction == ShopActionType.RestoreDestroyed)
            {
                if (MoneySystem.Instance.SpendMoney(PriceRestoreArea))
                {
                    DayCycleManager.Instance.RestoreDestroyedZone(zoneType);
                    ShowShop();
                    ShowFeedback($"<color=#4ade80>Успешно! Регион {zoneType} полностью восстановлен!</color>");
                }
                else
                {
                    ShowFeedback("<color=#f87171>Недостаточно средств ($250)!</color>");
                }
            }
            else if (_currentShopAction == ShopActionType.PauseThreatGrowth)
            {
                if (MoneySystem.Instance.SpendMoney(PricePauseGrowth))
                {
                    DayCycleManager.Instance.PauseThreatGrowthForZone(zoneType);
                    ShowShop();
                    ShowFeedback($"<color=#4ade80>Успешно! Развитие угрозы в регионе {zoneType} заморожено на 1 день!</color>");
                }
                else
                {
                    ShowFeedback("<color=#f87171>Недостаточно средств ($350)!</color>");
                }
            }
            else if (_currentShopAction == ShopActionType.RollbackThreat)
            {
                if (MoneySystem.Instance.SpendMoney(PriceRollback))
                {
                    DayCycleManager.Instance.RollbackThreatLevel(zoneType);
                    ShowShop();
                    ShowFeedback($"<color=#4ade80>Успешно! Уровень угрозы в регионе {zoneType} снижен на 1!</color>");
                }
                else
                {
                    ShowFeedback("<color=#f87171>Недостаточно средств ($500)!</color>");
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

