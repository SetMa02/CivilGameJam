using UnityEngine;
using CivilGame.DayCycle;

namespace CivilGame.Threats
{
    public class SeaZoneVisualController : MonoBehaviour
    {
        [Header("Sea / Water Renderer")]
        [SerializeField] private SpriteRenderer _waterRenderer;

        [Header("Water Animator")]
        [SerializeField] private Animator _waterAnimator;
        [SerializeField] private string _tsunamiBoolParam = "IsTsunami";

        [Header("Healthy State (Нормальное море)")]
        [SerializeField] private Sprite _healthySprite; // MAP2 - water

        [Header("Tsunami Phases (Цунами)")]
        [Tooltip("Этап 1 цунами: резкий отлив воды (если не задан, используется нормальный спрайт с анимацией PrerpareTsunami)")]
        [SerializeField] private Sprite _tsunamiStage1Sprite;
        [Tooltip("Этап 2 цунами: гигантская приливная волна (water TSUNAMI)")]
        [SerializeField] private Sprite _tsunamiStage2Sprite;

        [Header("Destroyed State (Разрушенное море)")]
        [Tooltip("Спрайт моря, когда сектор полностью разрушен цунами (water TSUNAMI)")]
        [SerializeField] private Sprite _destroyedSprite; // water TSUNAMI

        public Animator WaterAnimator => _waterAnimator;
        public bool IsTsunamiAnimationActive => _waterAnimator != null && _waterAnimator.GetBool(_tsunamiBoolParam);

        private void Awake()
        {
            if (_waterRenderer == null)
            {
                _waterRenderer = GetComponent<SpriteRenderer>();
            }

            if (_waterAnimator == null)
            {
                _waterAnimator = GetComponent<Animator>();
            }

            if (_waterAnimator != null)
            {
                _waterAnimator.enabled = true;
            }
        }

        private void LateUpdate()
        {
            UpdateSeaVisual();
        }

        public void UpdateSeaVisual()
        {
            if (_waterRenderer == null) return;

            var dcm = DayCycleManager.Instance != null ? DayCycleManager.Instance : FindAnyObjectByType<DayCycleManager>();
            if (dcm == null) return;

            var seaModel = dcm.GetZoneModel(ZoneType.Sea);
            if (seaModel == null) return;

            // 1. Море разрушено
            if (seaModel.isDestroyed)
            {
                if (_destroyedSprite != null)
                {
                    _waterRenderer.sprite = _destroyedSprite;
                }
                else if (_tsunamiStage2Sprite != null)
                {
                    _waterRenderer.sprite = _tsunamiStage2Sprite;
                }

                SetTsunamiAnimation(false);
                return;
            }

            // 2. Активная угроза: ЦУНАМИ
            if (seaModel.activeThreat != null && seaModel.activeThreat.definition.type == ThreatType.Tsunami)
            {
                int stage = seaModel.activeThreat.currentStage;

                if (stage == 1)
                {
                    // Этап 1: Вода начинает отступать / анимация подготовки к цунами (перед затоплением)
                    if (_tsunamiStage1Sprite != null)
                    {
                        _waterRenderer.sprite = _tsunamiStage1Sprite;
                    }
                    else if (_healthySprite != null)
                    {
                        _waterRenderer.sprite = _healthySprite;
                    }

                    SetTsunamiAnimation(true);
                }
                else if (stage >= 2)
                {
                    // Этап 2: Вода затопляет берег (гигантская волна)
                    if (_tsunamiStage2Sprite != null)
                    {
                        _waterRenderer.sprite = _tsunamiStage2Sprite;
                    }

                    SetTsunamiAnimation(false);
                }
                return;
            }

            // 3. Спокойное море (угроза нейтрализована или отсутствует)
            if (_healthySprite != null)
            {
                _waterRenderer.sprite = _healthySprite;
            }

            SetTsunamiAnimation(false);
        }

        private void SetTsunamiAnimation(bool isTsunami)
        {
            if (_waterAnimator != null)
            {
                if (!_waterAnimator.enabled)
                {
                    _waterAnimator.enabled = true;
                }

                if (_waterAnimator.gameObject.activeInHierarchy && _waterAnimator.runtimeAnimatorController != null)
                {
                    _waterAnimator.SetBool(_tsunamiBoolParam, isTsunami);
                }
            }
        }
    }
}

