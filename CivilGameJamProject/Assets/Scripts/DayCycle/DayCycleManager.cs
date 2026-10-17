using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CivilGame.Threats;

namespace CivilGame.DayCycle
{
    public class DayCycleManager : MonoBehaviour
    {
        public static DayCycleManager Instance { get; private set; }

        [Header("Day Progression")]
        [SerializeField] private int _currentDay = 1;
        [SerializeField] private float _baseThreats = 1.0f;
        [SerializeField] private float _threatGrowthPerDay = 0.5f;

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

        // One squad dispatch per day & delayed results
        private int _maxDispatchesPerDay = 1;
        private int _dispatchesUsedToday = 0;
        private bool _pauseThreatGrowthNextDay = false;
        private bool _hasPendingDispatch = false;
        private SquadType _pendingSquad;
        private ZoneType _pendingZone;
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
            _hasPendingDispatch = false;
        }

        private void OnDestroy()
        {
            if (_bedObject != null)
            {
                _bedObject.OnClick.RemoveListener(TrySleep);
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveListener(RestartGame);
            }
        }

        private void Start()
        {
            if (_gameOverPanel != null) _gameOverPanel.SetActive(false);
            if (_fadeCanvasGroup != null)
            {
                _fadeCanvasGroup.alpha = 0f;
                _fadeCanvasGroup.blocksRaycasts = false;
            }

            // Spawn initial threats for Day 1
            SpawnThreatsForCurrentDay();
            UpdateUIAndViews();

            // Morning sound cues for day 1
            TriggerMorningThreatAudioAndEffects();
        }

        private void InitializeThreatDefinitions()
        {
            _threatDefinitions = new List<ThreatDefinition>
            {
                // Засуха (3 стадии: Степь/Поле, Лес)
                new ThreatDefinition(ThreatType.Drought, "Засуха", 3, ZoneType.Forest, ZoneType.Field),

                // Пожар (3 стадии: Степь/Поле, Лес)
                new ThreatDefinition(ThreatType.Wildfire, "Лесной пожар", 3, ZoneType.Forest, ZoneType.Field),

                // Оползень (2 стадии: Горы - решается срочно!)
                new ThreatDefinition(ThreatType.Landslide, "Оползень", 2, ZoneType.Mountains),

                // Цунами (2 стадии: Море - решается срочно!)
                new ThreatDefinition(ThreatType.Tsunami, "Цунами", 2, ZoneType.Sea)
            };
        }

        private void InitializeZones()
        {
            _zones.Clear();
            _zones[ZoneType.Sea] = new ZoneModel(ZoneType.Sea, "Море");
            _zones[ZoneType.Mountains] = new ZoneModel(ZoneType.Mountains, "Горы");
            _zones[ZoneType.Forest] = new ZoneModel(ZoneType.Forest, "Лес");
            _zones[ZoneType.Field] = new ZoneModel(ZoneType.Field, "Поле");
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
            StartCoroutine(DayTransitionRoutine());
        }

        private IEnumerator DayTransitionRoutine()
        {
            _isTransitioning = true;

            // Fade Out (экран темнеет)
            if (_fadeCanvasGroup != null)
            {
                _fadeCanvasGroup.blocksRaycasts = true;
                float duration = 0.75f;
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    _fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
                    yield return null;
                }
                _fadeCanvasGroup.alpha = 1f;
            }

            // --- НОЧНАЯ ФАЗА ---
            _currentDay++;

            // 1. Разрешаем отправленный днем отряд спасения
            ResolvePendingDispatch();

            if (_fadeDayAnnouncementText != null)
            {
                if (!string.IsNullOrEmpty(_lastNightReport))
                {
                    _fadeDayAnnouncementText.text = $"ДЕНЬ {_currentDay}\n\n<size=20><color=#e2e8f0>{_lastNightReport}</color></size>";
                }
                else
                {
                    _fadeDayAnnouncementText.text = $"ДЕНЬ {_currentDay}";
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

            // Пауза в темноте
            yield return new WaitForSeconds(1.2f);

            // Fade In (экран светлеет)
            if (_fadeCanvasGroup != null)
            {
                float duration = 0.75f;
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

            // --- УТРЕННИЕ ЗВУКИ И ЭФФЕКТЫ УГРОЗ ---
            TriggerMorningThreatAudioAndEffects();

            _isTransitioning = false;
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
                _dayCounterText.text = $"ДЕНЬ {_currentDay}";
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
                resultMessage = $"Штаб сообщает: на сегодня лимит ({_maxDispatchesPerDay} в день) исчерпан! Дождитесь следующего дня или купите второй вызов в магазине.";
                return false;
            }

            if (!_zones.TryGetValue(zoneType, out ZoneModel zone))
            {
                resultMessage = "Зона не найдена!";
                return false;
            }

            if (zone.isDestroyed)
            {
                resultMessage = $"Зона {zone.zoneName} уже уничтожена! Отряд не может быть отправлен туда.";
                return false;
            }

            _dispatchesUsedToday++;
            _hasPendingDispatch = true;
            _pendingSquad = squadType;
            _pendingZone = zoneType;

            string squadName = GetSquadName(squadType);
            resultMessage = $"Запрос принят штабом!\nОтряд <b>{squadName}</b> направлен в сектор <b>{zone.zoneName}</b>.\nРезультаты операции станут известны завтра утром.";
            Debug.Log($"[DayCycle] {resultMessage}");
            return true;
        }

        private void ResolvePendingDispatch()
        {
            if (!_hasPendingDispatch)
            {
                _lastNightReport = "";
                return;
            }

            _hasPendingDispatch = false;

            if (!_zones.TryGetValue(_pendingZone, out ZoneModel zone))
            {
                _lastNightReport = "ОТЧЁТ: Координаты зоны были утеряны.";
                return;
            }

            string squadName = GetSquadName(_pendingSquad);

            if (zone.isDestroyed)
            {
                _lastNightReport = $"ОТЧЁТ: Отряд {squadName} прибыл в {zone.zoneName}, но сектор уже был разрушен.";
                return;
            }

            if (zone.activeThreat == null)
            {
                _lastNightReport = $"ОТЧЁТ: Отряд {squadName} обследовал сектор {zone.zoneName}. Угроз не обнаружено.";
                return;
            }

            var threat = zone.activeThreat;
            bool isEffective = IsSquadEffective(_pendingSquad, threat);

            if (isEffective)
            {
                string threatName = threat.definition.displayName;
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

                _lastNightReport = $"<color=#86efac>УСПЕХ!</color> Отряд {squadName} успешно ликвидировал угрозу '{threatName}' в секторе {zone.zoneName}! (+${MoneySystem.Instance?.rewardPerResolvedThreat ?? 200})";
                Debug.Log($"[DayCycle] {_lastNightReport}");
            }
            else
            {
                _lastNightReport = $"<color=#f87171>ПРОВАЛ!</color> Отряд {squadName} не смог ликвидировать угрозу в секторе {zone.zoneName}!";
                Debug.Log($"[DayCycle] {_lastNightReport}");
            }
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
            if (_zones.TryGetValue(type, out ZoneModel zone) && zone.isDestroyed)
            {
                zone.isDestroyed = false;
                zone.activeThreat = null;
                zone.immuneUntilDay = _currentDay; // 1 день защиты
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
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }
}
