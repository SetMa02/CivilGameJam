using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CivilGame.Threats;
using CivilGame.UI;

namespace CivilGame.DayCycle
{
    public class DayCycleManager : MonoBehaviour
    {
        public static DayCycleManager Instance { get; private set; }

        [Header("Day Progression")]
        [SerializeField] private int _currentDay = 1;
        [SerializeField] private float _baseThreats = 1.0f;
        [SerializeField] private float _threatGrowthPerDay = 0.5f;

        [Header("Ending Settings")]
        [SerializeField] private string _endGameSceneName = "NewScene";

        [Header("Ending Dialogue Customization (Day 8)")]
        [Tooltip("Ending 1 (Perfect run): Hello-hello! All preserved...")]
        [SerializeField] private DialogueLineData _endingPerfect = new DialogueLineData();
        [Tooltip("Ending 2 (Restored): High price for mistakes...")]
        [SerializeField] private DialogueLineData _endingRestored = new DialogueLineData();
        [Tooltip("Ending 3 (Failed zones): RANGER! What is this?! Why is a zone dead?!")]
        [SerializeField] private DialogueLineData _endingFailed = new DialogueLineData();

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _dayCounterText;
        [SerializeField] private CanvasGroup _fadeCanvasGroup;
        [SerializeField] private TextMeshProUGUI _fadeDayAnnouncementText;
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private Button _restartButton;

        [Header("Interactive Bed")]
        [SerializeField] private CivilGame.Lighthouse.InteractiveLighthouseObject _bedObject;

        [Header("Zone Views in Scene")]
        [SerializeField] private List<ZoneView> _zoneViews = new List<ZoneView>();

        private Dictionary<ZoneType, ZoneModel> _zones = new Dictionary<ZoneType, ZoneModel>();
        private List<ThreatDefinition> _threatDefinitions = new List<ThreatDefinition>();
        private bool _isTransitioning = false;
        private bool _isGameOver = false;

        private bool _hasAnyZoneEverBeenDestroyed = false;
        private bool _hasAnyZoneEverBeenRestored = false;

        public bool HasAnyZoneEverBeenDestroyed => _hasAnyZoneEverBeenDestroyed;
        public bool HasAnyZoneEverBeenRestored => _hasAnyZoneEverBeenRestored;

        public void SetZoneEverRestored(bool val) => _hasAnyZoneEverBeenRestored = val;
        public void SetZoneEverDestroyed(bool val) => _hasAnyZoneEverBeenDestroyed = val;

        [System.Serializable]
        public struct SquadDispatchOrder
        {
            public SquadType squad;
            public ZoneType zone;

            public SquadDispatchOrder(SquadType squad, ZoneType zone)
            {
                this.squad = squad;
                this.zone = zone;
            }
        }

        // Squad dispatches per day & delayed results
        private int _maxDispatchesPerDay = 1;
        private int _dispatchesUsedToday = 0;
        private bool _pauseThreatGrowthNextDay = false;
        private List<SquadDispatchOrder> _pendingDispatches = new List<SquadDispatchOrder>();
        private string _lastNightReport = "";

        public int CurrentDay => _currentDay;
        public bool IsTransitioning => _isTransitioning;
        public bool IsGameOver => _isGameOver;
        public int MaxDispatchesPerDay => _maxDispatchesPerDay;
        public int DispatchesUsedToday => _dispatchesUsedToday;
        public bool HasDispatchedToday => _dispatchesUsedToday >= _maxDispatchesPerDay;
        public string LastNightReport => _lastNightReport;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_bedObject != null)
            {
                _bedObject.OnClick.AddListener(TrySleep);
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.AddListener(RestartGame);
            }

            InitializeThreatDefinitions();
            InitializeZones();

            _currentDay = 1;
            _maxDispatchesPerDay = 1;
            _dispatchesUsedToday = 0;
            _pauseThreatGrowthNextDay = false;
            _pendingDispatches.Clear();

            CivilGame.Localization.LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
        }

        private void OnDestroy()
        {
            CivilGame.Localization.LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;

            if (_bedObject != null)
            {
                _bedObject.OnClick.RemoveListener(TrySleep);
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveListener(RestartGame);
            }
        }

        private void HandleLanguageChanged(CivilGame.Localization.GameLanguage newLang)
        {
            UpdateUIAndViews();
        }

        private void Start()
        {
            if (_gameOverPanel != null) _gameOverPanel.SetActive(false);
            if (_fadeCanvasGroup != null)
            {
                _fadeCanvasGroup.alpha = 0f;
                _fadeCanvasGroup.blocksRaycasts = false;
            }

            bool isTutorial = PlayerPrefs.GetInt(DayOneTutorialDirector.TutorialPrefKey, 0) == 0;
            if (isTutorial)
            {
                _currentDay = 1;
                // Scripted Day 1: DayOneTutorialDirector handles the single scripted threat (Drought)
            }
            else
            {
                _currentDay = 2; // Starts directly on Day 2 if restarting or tutorial completed
                SpawnThreatsForCurrentDay();
                TriggerMorningThreatAudioAndEffects();
            }

            UpdateUIAndViews();
        }

        private void InitializeThreatDefinitions()
        {
            _threatDefinitions = new List<ThreatDefinition>
            {
                new ThreatDefinition(ThreatType.Drought, "Drought", 3, ZoneType.Forest, ZoneType.Field),
                new ThreatDefinition(ThreatType.Wildfire, "Wildfire", 3, ZoneType.Forest, ZoneType.Field),
                new ThreatDefinition(ThreatType.Landslide, "Landslide", 2, ZoneType.Mountains),
                new ThreatDefinition(ThreatType.Tsunami, "Tsunami", 2, ZoneType.Sea)
            };
        }

        private void InitializeZones()
        {
            _zones.Clear();
            _zones[ZoneType.Sea] = new ZoneModel(ZoneType.Sea, "Sea");
            _zones[ZoneType.Mountains] = new ZoneModel(ZoneType.Mountains, "Mountains");
            _zones[ZoneType.Forest] = new ZoneModel(ZoneType.Forest, "Forest");
            _zones[ZoneType.Field] = new ZoneModel(ZoneType.Field, "Field");
        }

        public void RegisterZoneView(ZoneView view)
        {
            if (!_zoneViews.Contains(view))
            {
                _zoneViews.Add(view);
            }
            UpdateUIAndViews();
        }

        public void TrySleep()
        {
            if (_isTransitioning || _isGameOver) return;
            if (DayOneTutorialDirector.Instance != null && !DayOneTutorialDirector.Instance.CanSleepNow())
            {
                Debug.Log("[DayCycle] You cannot sleep yet during tutorial until shift is completed!");
                return;
            }
            StartCoroutine(DayTransitionRoutine());
        }

        private IEnumerator DayTransitionRoutine()
        {
            _isTransitioning = true;

            // Блокируем параллакс мыши на время перехода
            var parallaxCtrl = CivilGame.CameraEffects.WindowParallaxController.Instance != null 
                ? CivilGame.CameraEffects.WindowParallaxController.Instance 
                : FindAnyObjectByType<CivilGame.CameraEffects.WindowParallaxController>();
            if (parallaxCtrl != null)
            {
                parallaxCtrl.IsLockedBySequence = true;
                parallaxCtrl.SetParallaxActive(false);
            }

            var cam = Camera.main;
            Vector3 camOriginPos = cam != null ? cam.transform.position : new Vector3(0f, 0f, -10f);

            // 1. КИНЕМАТОГРАФИЧНЫЙ «ВЛЁТ» В КРОВАТЬ
            // Центр коллайдера кровати: (-7.09, -2.39, 0), камера подлетает почти в упор (Z = -2.2)
            Vector3 bedTargetPos = new Vector3(-6.8f, -2.2f, -2.2f);
            float diveDuration = 1.35f;
            float diveElapsed = 0f;

            if (_fadeCanvasGroup != null)
            {
                _fadeCanvasGroup.blocksRaycasts = true;
                _fadeCanvasGroup.alpha = 0f;
            }

            while (diveElapsed < diveDuration)
            {
                diveElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(diveElapsed / diveDuration);
                // Плавное ускорение влёта (SmoothStep / EaseInCubic)
                float moveCurve = t * t * (3f - 2f * t);

                if (cam != null)
                {
                    cam.transform.position = Vector3.Lerp(camOriginPos, bedTargetPos, moveCurve);
                }

                // Затемнение экрана начинается во второй половине влёта (после 45% пути)
                if (_fadeCanvasGroup != null)
                {
                    float fadeProgress = Mathf.Clamp01((t - 0.45f) / 0.55f);
                    _fadeCanvasGroup.alpha = fadeProgress;
                }

                yield return null;
            }

            if (_fadeCanvasGroup != null) _fadeCanvasGroup.alpha = 1f;

            // Возвращаем камеру на исходную позицию, пока экран полностью черный
            if (cam != null)
            {
                cam.transform.position = camOriginPos;
            }

            // --- НОЧНАЯ ФАЗА ---
            _currentDay++;

            // 1. Разрешаем отправленный днем отряд спасения
            ResolvePendingDispatch();

            // Проверка достижения 8-го дня (Конец игры)
            if (_currentDay >= 8)
            {
                yield return StartCoroutine(EndGameRoutine(parallaxCtrl));
                yield break;
            }

            if (_fadeDayAnnouncementText != null)
            {
                string dayHeader = CivilGame.Localization.LocalizationManager.Get("hud.day", _currentDay);
                if (!string.IsNullOrEmpty(_lastNightReport))
                {
                    _fadeDayAnnouncementText.text = $"{dayHeader}\n\n<size=20><color=#e2e8f0>{_lastNightReport}</color></size>";
                }
                else
                {
                    _fadeDayAnnouncementText.text = dayHeader;
                }
            }

            // 2. Продвигаем существующие нерешенные угрозы (если не куплена заморозка)
            if (!_pauseThreatGrowthNextDay)
            {
                AdvanceExistingThreats();
            }
            else
            {
                Debug.Log("[DayCycle] Развитие угроз было заморожено на этот день!");
                _pauseThreatGrowthNextDay = false; // Действует 1 день
            }

            // 3. Проверяем Game Over (все ли зоны уничтожены)
            CheckGameOver();

            // 4. Спавним новые угрозы, если игра не окончена
            if (!_isGameOver)
            {
                SpawnThreatsForCurrentDay();
            }

            // Новый день: сбрасываем дневной лимит использования
            _dispatchesUsedToday = 0;
            _maxDispatchesPerDay = 1; // Улучшение на второй вызов действует внутри дня покупки

            UpdateUIAndViews();

            // Продлённая пауза глубокого «сна» (2.8 секунды в темноте для спокойного чтения отчёта)
            yield return new WaitForSeconds(2.8f);

            // Fade In (экран плавно светлеет, утро наступает)
            if (_fadeCanvasGroup != null)
            {
                float duration = 1.0f;
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    _fadeCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / duration));
                    yield return null;
                }
                _fadeCanvasGroup.alpha = 0f;
                _fadeCanvasGroup.blocksRaycasts = false;
            }

            // Снимаем блокировку параллакса
            if (parallaxCtrl != null)
            {
                parallaxCtrl.IsLockedBySequence = false;
            }

            // --- УТРЕННИЕ ЗВУКИ И ЭФФЕКТЫ УГРОЗ ---
            TriggerMorningThreatAudioAndEffects();

            _isTransitioning = false;
        }

        private IEnumerator EndGameRoutine(CivilGame.CameraEffects.WindowParallaxController parallaxCtrl)
        {
            if (_fadeDayAnnouncementText != null)
            {
                string dayHeader = CivilGame.Localization.LocalizationManager.Get("hud.day", _currentDay);
                if (!string.IsNullOrEmpty(_lastNightReport))
                {
                    _fadeDayAnnouncementText.text = $"{dayHeader}\n\n<size=20><color=#e2e8f0>{_lastNightReport}</color></size>";
                }
                else
                {
                    _fadeDayAnnouncementText.text = dayHeader;
                }
            }

            UpdateUIAndViews();

            yield return new WaitForSeconds(2.8f);

            // Fade in morning
            if (_fadeCanvasGroup != null)
            {
                float duration = 1.0f;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    _fadeCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / duration));
                    yield return null;
                }
                _fadeCanvasGroup.alpha = 0f;
                _fadeCanvasGroup.blocksRaycasts = false;
            }

            if (parallaxCtrl != null)
            {
                parallaxCtrl.IsLockedBySequence = false;
            }

            yield return new WaitForSeconds(0.8f);

            // Determine which ending to play:
            // 1. Any zone currently destroyed?
            bool hasCurrentlyDestroyedZone = false;
            foreach (var kvp in _zones)
            {
                if (kvp.Value.isDestroyed)
                {
                    hasCurrentlyDestroyedZone = true;
                    break;
                }
            }

            string endingKey;
            DialogueLineData selectedLineData = null;
            if (hasCurrentlyDestroyedZone)
            {
                // Ending 3: Bad ending (some zones dead)
                endingKey = "ending.failed_zones";
                selectedLineData = _endingFailed;
            }
            else if (_hasAnyZoneEverBeenRestored)
            {
                // Ending 2: Zones were destroyed but restored
                endingKey = "ending.restored";
                selectedLineData = _endingRestored;
            }
            else
            {
                // Ending 1: Perfect run (all preserved)
                endingKey = "ending.perfect";
                selectedLineData = _endingPerfect;
            }

            bool advanceDialogue = false;
            if (GuideDialogueUI.Instance != null)
            {
                GuideDialogueUI.Instance.ShowDialogue(
                    CivilGame.Localization.LocalizationManager.Get("guide.name"),
                    CivilGame.Localization.LocalizationManager.Get(endingKey),
                    () => advanceDialogue = true,
                    selectedLineData);
            }
            else
            {
                advanceDialogue = true;
            }

            while (!advanceDialogue)
            {
                yield return null;
            }

            if (GuideDialogueUI.Instance != null)
            {
                GuideDialogueUI.Instance.HideDialogue();
            }

            // Fade out to black before loading NewScene
            if (_fadeCanvasGroup != null)
            {
                _fadeCanvasGroup.blocksRaycasts = true;
                float duration = 1.2f;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    _fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
                    yield return null;
                }
                _fadeCanvasGroup.alpha = 1f;
            }

            yield return new WaitForSeconds(0.4f);

            Debug.Log($"[DayCycle] End Game reached. Transitioning to '{_endGameSceneName}'...");
            UnityEngine.SceneManagement.SceneManager.LoadScene(_endGameSceneName);
        }

        private void TriggerMorningThreatAudioAndEffects()
        {
            if (CivilGame.Audio.AudioManager.Instance == null) return;

            bool hasDrought1Or2 = false;
            bool hasFireStage1 = false;
            bool hasFireStage2OrMore = false;
            bool hasLandslide = false;
            bool hasTsunami = false;

            foreach (var kvp in _zones)
            {
                ZoneModel zone = kvp.Value;
                if (zone.isDestroyed || zone.activeThreat == null) continue;

                var threatType = zone.activeThreat.definition.type;
                int stage = zone.activeThreat.currentStage;

                if (threatType == ThreatType.Drought && (stage == 1 || stage == 2))
                {
                    hasDrought1Or2 = true;
                }
                else if (threatType == ThreatType.Wildfire)
                {
                    if (stage == 1) hasFireStage1 = true;
                    else if (stage >= 2) hasFireStage2OrMore = true;
                }
                else if (threatType == ThreatType.Landslide)
                {
                    hasLandslide = true;
                }
                else if (threatType == ThreatType.Tsunami)
                {
                    hasTsunami = true;
                }
            }

            // 1. Засуха: стрекот цикад примерно 4 секунды
            if (hasDrought1Or2)
            {
                CivilGame.Audio.AudioManager.Instance.PlayCicadasTimed(4.0f, 0.85f);
            }

            // 2. Пожар: этап 1 - звук птиц ~4 секунды, этап 2+ - звук огня
            if (hasFireStage2OrMore)
            {
                CivilGame.Audio.AudioManager.Instance.SetFireActive(true, 0.8f);
            }
            else
            {
                CivilGame.Audio.AudioManager.Instance.SetFireActive(false);
                if (hasFireStage1)
                {
                    CivilGame.Audio.AudioManager.Instance.PlayBirdsTimed(4.0f, 0.85f);
                }
            }

            // 3. Оползень в горах: звук оползня (LandSlide)
            if (hasLandslide)
            {
                CivilGame.Audio.AudioManager.Instance.PlayLandslide(0.95f);
            }

            // 4. Цунами: с утра звуки землетрясения и легкая тряска экрана
            if (hasTsunami)
            {
                CivilGame.Audio.AudioManager.Instance.PlayEarthquake(0.95f);
                if (CivilGame.CameraEffects.CameraShake.Instance != null)
                {
                    CivilGame.CameraEffects.CameraShake.Instance.Shake(2.5f, 0.14f);
                }
            }
        }

        private void AdvanceExistingThreats()
        {
            foreach (var kvp in _zones)
            {
                ZoneModel zone = kvp.Value;
                if (zone.isDestroyed || zone.activeThreat == null) continue;

                if (zone.isFrozenForNextDay)
                {
                    zone.isFrozenForNextDay = false;
                    Debug.Log($"[DayCycle] Зона {zone.zoneName}: развитие угрозы заморожено на этот день!");
                    continue;
                }

                if (zone.activeThreat.IsFinalStage)
                {
                    // Угроза достигла предела и не была устранена -> регион уничтожен!
                    zone.isDestroyed = true;
                    zone.activeThreat = null;
                    _hasAnyZoneEverBeenDestroyed = true;
                    Debug.Log($"[DayCycle] Зона {zone.zoneName} ПОЛНОСТЬЮ УНИЧТОЖЕНА!");
                }
                else
                {
                    // Переход на следующий этап
                    zone.activeThreat.currentStage++;
                    Debug.Log($"[DayCycle] Зона {zone.zoneName}: угроза '{zone.activeThreat.definition.displayName}' перешла на этап {zone.activeThreat.currentStage}.");
                }
            }
        }

        private void SpawnThreatsForCurrentDay()
        {
            // Формула: 1 + (day - 1) * 0.5
            float expected = _baseThreats + (_currentDay - 1) * _threatGrowthPerDay;
            int guaranteed = Mathf.FloorToInt(expected);
            float bonusChance = expected - guaranteed;

            int countToSpawn = guaranteed;
            if (Random.value < bonusChance)
            {
                countToSpawn++;
            }

            // Находим свободные регионы, которые не уничтожены, где еще нет активной угрозы и не действует иммунитет
            List<ZoneModel> availableZones = new List<ZoneModel>();
            foreach (var kvp in _zones)
            {
                ZoneModel zone = kvp.Value;
                if (!zone.isDestroyed && zone.activeThreat == null && _currentDay > zone.immuneUntilDay)
                {
                    availableZones.Add(zone);
                }
            }

            // Ограничиваем числом свободных зон
            int actualSpawns = Mathf.Min(countToSpawn, availableZones.Count);
            if (actualSpawns <= 0) return;

            // Перемешиваем доступные зоны для случайного выбора
            for (int i = 0; i < availableZones.Count; i++)
            {
                int rnd = Random.Range(i, availableZones.Count);
                var temp = availableZones[i];
                availableZones[i] = availableZones[rnd];
                availableZones[rnd] = temp;
            }

            for (int i = 0; i < actualSpawns; i++)
            {
                ZoneModel targetZone = availableZones[i];
                ThreatDefinition threatDef = PickRandomThreatForZone(targetZone.zoneType);

                if (threatDef != null)
                {
                    targetZone.activeThreat = new ActiveThreat(threatDef, 1);
                    Debug.Log($"[DayCycle] День {_currentDay}: в зоне {targetZone.zoneName} заспавнена угроза '{threatDef.displayName}' (Этап 1).");
                }
            }
        }

        private ThreatDefinition PickRandomThreatForZone(ZoneType zoneType)
        {
            List<ThreatDefinition> eligible = new List<ThreatDefinition>();
            foreach (var def in _threatDefinitions)
            {
                if (def.allowedZones.Contains(zoneType))
                {
                    eligible.Add(def);
                }
            }

            if (eligible.Count == 0) return null;
            return eligible[Random.Range(0, eligible.Count)];
        }

        private void CheckGameOver()
        {
            bool allDestroyed = true;
            foreach (var kvp in _zones)
            {
                if (!kvp.Value.isDestroyed)
                {
                    allDestroyed = false;
                    break;
                }
            }

            if (allDestroyed)
            {
                _isGameOver = true;
                Debug.LogWarning("[DayCycle] ВСЕ ЗОНЫ УНИЧТОЖЕНЫ! GAME OVER!");
                if (_gameOverPanel != null)
                {
                    _gameOverPanel.SetActive(true);
                }
            }
        }

        public void UpdateUIAndViews()
        {
            if (_dayCounterText != null)
            {
                _dayCounterText.text = CivilGame.Localization.LocalizationManager.Get("hud.day", _currentDay);
            }

            foreach (var view in _zoneViews)
            {
                if (view != null && _zones.TryGetValue(view.ZoneType, out ZoneModel model))
                {
                    view.UpdateView(model);
                }
            }
        }

        public ZoneModel GetZoneModel(ZoneType type)
        {
            if (_zones.Count == 0)
            {
                InitializeThreatDefinitions();
                InitializeZones();
            }

            _zones.TryGetValue(type, out ZoneModel model);
            return model;
        }

        public bool DispatchSquad(ZoneType zoneType, SquadType squadType, out string resultMessage)
        {
            if (_zones.Count == 0)
            {
                InitializeThreatDefinitions();
                InitializeZones();
            }

            if (_dispatchesUsedToday >= _maxDispatchesPerDay)
            {
                resultMessage = CivilGame.Localization.LocalizationManager.Get("phone.limit_reached_alert");
                return false;
            }

            if (!_zones.TryGetValue(zoneType, out ZoneModel zone))
            {
                resultMessage = "Zone not found!";
                return false;
            }

            if (zone.isDestroyed)
            {
                string zName = CivilGame.Localization.LocalizationManager.GetZoneName(zone.zoneType);
                resultMessage = $"Zone {zName} is already destroyed! Squad cannot be dispatched there.";
                return false;
            }

            _dispatchesUsedToday++;
            _pendingDispatches.Add(new SquadDispatchOrder(squadType, zoneType));

            string squadName = CivilGame.Localization.LocalizationManager.GetSquadName(squadType);
            string zoneName = CivilGame.Localization.LocalizationManager.GetZoneName(zone.zoneType);
            resultMessage = CivilGame.Localization.LocalizationManager.Get("dispatch.accepted", squadName, zoneName, _dispatchesUsedToday, _maxDispatchesPerDay);
            Debug.Log($"[DayCycle] {resultMessage}");
            return true;
        }

        public void ResolvePendingDispatch()
        {
            if (_pendingDispatches.Count == 0)
            {
                _lastNightReport = "";
                return;
            }

            var reportBuilder = new System.Text.StringBuilder();

            foreach (var order in _pendingDispatches)
            {
                if (!_zones.TryGetValue(order.zone, out ZoneModel zone))
                {
                    reportBuilder.AppendLine(CivilGame.Localization.LocalizationManager.Get("report.lost_coords"));
                    continue;
                }

                string squadName = CivilGame.Localization.LocalizationManager.GetSquadName(order.squad);
                string zoneName = CivilGame.Localization.LocalizationManager.GetZoneName(zone.zoneType);

                if (zone.isDestroyed)
                {
                    reportBuilder.AppendLine(CivilGame.Localization.LocalizationManager.Get("report.already_destroyed", squadName, zoneName));
                    continue;
                }

                if (zone.activeThreat == null)
                {
                    reportBuilder.AppendLine(CivilGame.Localization.LocalizationManager.Get("report.no_threats", squadName, zoneName));
                    continue;
                }

                var threat = zone.activeThreat;
                bool isEffective = IsSquadEffective(order.squad, threat);

                if (isEffective)
                {
                    string threatName = CivilGame.Localization.LocalizationManager.GetThreatName(threat.definition.type);
                    zone.activeThreat = null;
                    // Зона получает иммунитет на текущий новый день
                    zone.immuneUntilDay = _currentDay;

                    // Награда за успешную ликвидацию бедствия
                    if (MoneySystem.Instance != null)
                    {
                        MoneySystem.Instance.AddMoneyAmount(MoneySystem.Instance.rewardPerResolvedThreat);
                    }

                    // Check if all fires resolved
                    bool anyFireRemaining = false;
                    foreach (var z in _zones.Values)
                    {
                        if (z.activeThreat != null && z.activeThreat.definition.type == ThreatType.Wildfire && z.activeThreat.currentStage >= 2)
                        {
                            anyFireRemaining = true;
                            break;
                        }
                    }
                    if (!anyFireRemaining && CivilGame.Audio.AudioManager.Instance != null)
                    {
                        CivilGame.Audio.AudioManager.Instance.SetFireActive(false);
                    }

                    reportBuilder.AppendLine(CivilGame.Localization.LocalizationManager.Get("report.success", squadName, threatName, zoneName, MoneySystem.Instance?.rewardPerResolvedThreat ?? 200));
                }
                else
                {
                    reportBuilder.AppendLine(CivilGame.Localization.LocalizationManager.Get("report.failure", squadName, zoneName));
                }
            }

            _lastNightReport = reportBuilder.ToString().TrimEnd();
            Debug.Log($"[DayCycle] {_lastNightReport}");

            _pendingDispatches.Clear();
        }

        #region Shop Capabilities

        public bool EnableSecondReportCall()
        {
            if (_maxDispatchesPerDay >= 2) return false;
            _maxDispatchesPerDay = 2;
            Debug.Log("[Shop] Second report call purchased! Max dispatches today = 2.");
            return true;
        }

        public List<ZoneModel> GetDestroyedZones()
        {
            if (_zones.Count == 0)
            {
                InitializeThreatDefinitions();
                InitializeZones();
            }

            var list = new List<ZoneModel>();
            foreach (var kvp in _zones)
            {
                if (kvp.Value.isDestroyed)
                {
                    list.Add(kvp.Value);
                }
            }
            return list;
        }

        public List<ZoneModel> GetThreatenedZones()
        {
            if (_zones.Count == 0)
            {
                InitializeThreatDefinitions();
                InitializeZones();
            }

            var list = new List<ZoneModel>();
            foreach (var kvp in _zones)
            {
                if (!kvp.Value.isDestroyed && kvp.Value.activeThreat != null)
                {
                    list.Add(kvp.Value);
                }
            }
            return list;
        }

        public bool RestoreDestroyedZone(ZoneType type)
        {
            if (_zones.Count == 0)
            {
                InitializeThreatDefinitions();
                InitializeZones();
            }

            if (_zones.TryGetValue(type, out ZoneModel zone) && zone.isDestroyed)
            {
                zone.isDestroyed = false;
                zone.activeThreat = null;
                zone.immuneUntilDay = _currentDay; // 1 день защиты
                _hasAnyZoneEverBeenRestored = true;
                UpdateUIAndViews();
                Debug.Log($"[Shop] Зона {zone.zoneName} полностью восстановлена!");
                return true;
            }
            return false;
        }

        public void PauseThreatGrowthForOneDay()
        {
            _pauseThreatGrowthNextDay = true;
            Debug.Log("[Shop] Развитие угроз приостановлено на 1 день.");
        }

        public bool PauseThreatGrowthForZone(ZoneType type)
        {
            if (_zones.Count == 0)
            {
                InitializeThreatDefinitions();
                InitializeZones();
            }

            if (_zones.TryGetValue(type, out ZoneModel zone) && !zone.isDestroyed && zone.activeThreat != null)
            {
                zone.isFrozenForNextDay = true;
                Debug.Log($"[Shop] Развитие угрозы в зоне {zone.zoneName} приостановлено на 1 день.");
                return true;
            }
            return false;
        }

        public bool RollbackThreatLevel(ZoneType type)
        {
            if (_zones.Count == 0)
            {
                InitializeThreatDefinitions();
                InitializeZones();
            }

            if (_zones.TryGetValue(type, out ZoneModel zone) && !zone.isDestroyed && zone.activeThreat != null)
            {
                if (zone.activeThreat.currentStage <= 1)
                {
                    // Угроза 1-го уровня устраняется полностью
                    zone.activeThreat = null;
                    zone.immuneUntilDay = _currentDay;
                    Debug.Log($"[Shop] Угроза 1-го уровня в {zone.zoneName} полностью устранена!");
                }
                else
                {
                    // Понижение на 1 уровень
                    zone.activeThreat.currentStage--;
                    Debug.Log($"[Shop] Угроза в {zone.zoneName} снижена до этапа {zone.activeThreat.currentStage}.");
                }
                UpdateUIAndViews();
                return true;
            }
            return false;
        }

        #endregion

        public static string GetSquadName(SquadType squad)
        {
            switch (squad)
            {
                case SquadType.Irrigators: return "Irrigators";
                case SquadType.Lumberjacks: return "Lumberjacks";
                case SquadType.Firefighters: return "Firefighters";
                case SquadType.RescueHelicopter: return "Rescue Helicopter";
                case SquadType.MountainClimbers: return "Mountain Climbers";
                case SquadType.Divers: return "Divers";
                default: return squad.ToString();
            }
        }

        private bool IsSquadEffective(SquadType squad, ActiveThreat threat)
        {
            var type = threat.definition.type;
            int stage = threat.currentStage;

            switch (squad)
            {
                case SquadType.Irrigators:
                    // Оросители: убирают засуху на первых этапах (этап 1 и 2)
                    return type == ThreatType.Drought && (stage == 1 || stage == 2);

                case SquadType.Lumberjacks:
                    // Лесорубы: убирают пожар на первом этапе (этап 1)
                    return type == ThreatType.Wildfire && stage == 1;

                case SquadType.Firefighters:
                    // Пожарные: убирают пожар на втором этапе (этап 2)
                    return type == ThreatType.Wildfire && stage == 2;

                case SquadType.RescueHelicopter:
                    // Спасательный вертолет: убирает пожар и оползень на критических этапах
                    if (type == ThreatType.Wildfire && stage >= 3) return true;
                    if (type == ThreatType.Landslide && stage >= 2) return true;
                    return false;

                case SquadType.MountainClimbers:
                    // Альпинисты: оползень на первых этапах (этап 1)
                    return type == ThreatType.Landslide && stage == 1;

                case SquadType.Divers:
                    // Дайверы: убирают цунами на первых этапах (этап 1)
                    return type == ThreatType.Tsunami && stage == 1;

                default:
                    return false;
            }
        }

        public void RestartGame()
        {
            PlayerPrefs.SetInt(DayOneTutorialDirector.TutorialPrefKey, 1);
            PlayerPrefs.Save();
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }
}
