using UnityEngine;
using CivilGame.DayCycle;

namespace CivilGame.Threats
{
    public class FieldZoneVisualController : MonoBehaviour
    {
        [Header("Meadow / Field Renderer")]
        [SerializeField] private SpriteRenderer _fieldRenderer;

        [Header("Default State (Нормальный луг)")]
        [SerializeField] private Sprite _healthySprite;  // луг 1

        [Header("Drought Phases (Засуха)")]
        [Tooltip("Этап 1 засухи: начальное высыхание травы")]
        [SerializeField] private Sprite _droughtStage1Sprite;   // луг 2 засуха
        [Tooltip("Этап 2 засухи: пожелтение и сухая земля")]
        [SerializeField] private Sprite _droughtStage2Sprite;   // луг 3 засуха
        [Tooltip("Этап 3 засухи: полное высыхание / гибель растительности")]
        [SerializeField] private Sprite _droughtStage3Sprite;   // луг 4 засуха

        [Header("Wildfire Phases (Пожар - Заготовки под будущие спрайты)")]
        [Tooltip("Этап 1 пожара: задымление / тление (если спрайта нет, будет использован дымчатый оттенок)")]
        [SerializeField] private Sprite _fireStage1Sprite;
        [Tooltip("Этап 2 пожара: открытое пламя (если спрайта нет, будет использован огненный оттенок)")]
        [SerializeField] private Sprite _fireStage2Sprite;
        [Tooltip("Этап 3 пожара: бушующий верховой пожар / обугливание (если спрайта нет, оттенок пепелища)")]
        [SerializeField] private Sprite _fireStage3Sprite;

        [Header("Destroyed State (Разрушенная зона)")]
        [Tooltip("Спрайт полностью уничтоженного луга (если не задан, используется 3-я стадия текущей угрозы)")]
        [SerializeField] private Sprite _destroyedSprite;

        [Header("Fallback Tint (пока нет спрайтов огня)")]
        [SerializeField] private bool _useColorTintFallback = true;

        private void Awake()
        {
            if (_fieldRenderer == null)
            {
                _fieldRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void LateUpdate()
        {
            UpdateFieldVisual();
        }

        public void UpdateFieldVisual()
        {
            if (_fieldRenderer == null) return;

            var dcm = DayCycleManager.Instance != null ? DayCycleManager.Instance : FindAnyObjectByType<DayCycleManager>();
            if (dcm == null) return;

            var fieldModel = dcm.GetZoneModel(ZoneType.Field);
            if (fieldModel == null) return;

            // 1. Полностью разрушенный сектор
            if (fieldModel.isDestroyed)
            {
                if (_destroyedSprite != null)
                {
                    _fieldRenderer.sprite = _destroyedSprite;
                    _fieldRenderer.color = Color.white;
                }
                else if (_droughtStage3Sprite != null)
                {
                    _fieldRenderer.sprite = _droughtStage3Sprite;
                    _fieldRenderer.color = Color.white;
                }
                return;
            }

            // 2. Активная угроза: ЗАСУХА
            if (fieldModel.activeThreat != null && fieldModel.activeThreat.definition.type == ThreatType.Drought)
            {
                int stage = fieldModel.activeThreat.currentStage;
                _fieldRenderer.color = Color.white;

                if (stage == 1 && _droughtStage1Sprite != null)
                {
                    _fieldRenderer.sprite = _droughtStage1Sprite;
                }
                else if (stage == 2 && _droughtStage2Sprite != null)
                {
                    _fieldRenderer.sprite = _droughtStage2Sprite;
                }
                else if (stage >= 3 && _droughtStage3Sprite != null)
                {
                    _fieldRenderer.sprite = _droughtStage3Sprite;
                }
                else if (_healthySprite != null)
                {
                    _fieldRenderer.sprite = _healthySprite;
                }
                return;
            }

            // 3. Активная угроза: ПОЖАР (спрайты при наличии, цвет управляется LeftWindowFireVisualController)
            if (fieldModel.activeThreat != null && fieldModel.activeThreat.definition.type == ThreatType.Wildfire)
            {
                int stage = fieldModel.activeThreat.currentStage;

                if (stage == 1 && _fireStage1Sprite != null)
                {
                    _fieldRenderer.sprite = _fireStage1Sprite;
                }
                else if (stage == 2 && _fireStage2Sprite != null)
                {
                    _fieldRenderer.sprite = _fireStage2Sprite;
                }
                else if (stage >= 3 && _fireStage3Sprite != null)
                {
                    _fieldRenderer.sprite = _fireStage3Sprite;
                }
                else if (_healthySprite != null)
                {
                    _fieldRenderer.sprite = _healthySprite;
                }
                return;
            }

            // 4. Спокойное состояние (угрозы нет)
            if (_healthySprite != null)
            {
                _fieldRenderer.sprite = _healthySprite;
            }
            _fieldRenderer.color = Color.white;
        }
    }
}

