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

        public int CurrentDay => _currentDay;
        public bool IsTransitioning => _isTransitioning;
        public bool IsGameOver => _isGameOver;

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

            if (_fadeDayAnnouncementText != null)
            {
                _fadeDayAnnouncementText.text = $"ДЕНЬ {_currentDay}";
            }

            // 1. Продвигаем существующие нерешенные угрозы
            AdvanceExistingThreats();

            // 2. Проверяем Game Over (все ли зоны уничтожены)
            CheckGameOver();

            // 3. Спавним новые угрозы, если игра не окончена
            if (!_isGameOver)
            {
                SpawnThreatsForCurrentDay();
            }

            UpdateUIAndViews();

            // Небольшая пауза в темноте
            yield return new WaitForSeconds(0.6f);

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

            // Находим свободные регионы, которые не уничтожены и где еще нет активной угрозы
            List<ZoneModel> availableZones = new List<ZoneModel>();
            foreach (var kvp in _zones)
            {
                if (!kvp.Value.isDestroyed && kvp.Value.activeThreat == null)
                {
                    availableZones.Add(kvp.Value);
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
            _zones.TryGetValue(type, out ZoneModel model);
            return model;
        }

        public void RestartGame()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }
}
