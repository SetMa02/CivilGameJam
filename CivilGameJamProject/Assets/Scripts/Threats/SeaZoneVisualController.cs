using UnityEngine;
using CivilGame.DayCycle;

namespace CivilGame.Threats
{
    public class SeaZoneVisualController : MonoBehaviour
    {
        [Header("Sea / Water Renderer")]
        [SerializeField] private SpriteRenderer _waterRenderer;

        [Header("Healthy State (Нормальное море)")]
        [SerializeField] private Sprite _healthySprite; // MAP2 - water

        [Header("Tsunami Phases (Цунами)")]
        [Tooltip("Этап 1 цунами: резкий отлив воды (если не задан, используется нормальный спрайт)")]
        [SerializeField] private Sprite _tsunamiStage1Sprite;
        [Tooltip("Этап 2 цунами: гигантская приливная волна (water TSUNAMI)")]
        [SerializeField] private Sprite _tsunamiStage2Sprite;

        [Header("Destroyed State (Разрушенное море)")]
        [Tooltip("Спрайт моря, когда сектор полностью разрушен цунами (water TSUNAMI)")]
        [SerializeField] private Sprite _destroyedSprite; // water TSUNAMI

        private void Awake()
        {
            if (_waterRenderer == null)
            {
                _waterRenderer = GetComponent<SpriteRenderer>();
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
                return;
            }

            // 2. Активная угроза: ЦУНАМИ
            if (seaModel.activeThreat != null && seaModel.activeThreat.definition.type == ThreatType.Tsunami)
            {
                int stage = seaModel.activeThreat.currentStage;
                if (stage >= 2 && _tsunamiStage2Sprite != null)
                {
                    _waterRenderer.sprite = _tsunamiStage2Sprite;
                }
                else if (stage == 1 && _tsunamiStage1Sprite != null)
                {
                    _waterRenderer.sprite = _tsunamiStage1Sprite;
                }
                else if (_healthySprite != null)
                {
                    _waterRenderer.sprite = _healthySprite;
                }
                return;
            }

            // 3. Спокойное море
            if (_healthySprite != null)
            {
                _waterRenderer.sprite = _healthySprite;
            }
        }
    }
}
