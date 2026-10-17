using UnityEngine;
using CivilGame.DayCycle;

namespace CivilGame.Threats
{
    public class ForestZoneVisualController : MonoBehaviour
    {
        [Header("Renderers on Left Window")]
        [Tooltip("Слой далеких деревьев (лес 1 задник)")]
        [SerializeField] private SpriteRenderer _farForestRenderer;
        [Tooltip("Слой переднего леса (лес 1 перед)")]
        [SerializeField] private SpriteRenderer _nearForestRenderer;
        [Tooltip("Слой фона/неба (Map 3 background)")]
        [SerializeField] private SpriteRenderer _backgroundRenderer;

        [Header("Far Forest Sprites (Этап 1+ засухи)")]
        [SerializeField] private Sprite _farForestHealthy;
        [SerializeField] private Sprite _farForestDrought;

        [Header("Near Forest Sprites (Этап 2+ засухи)")]
        [SerializeField] private Sprite _nearForestHealthy;
        [SerializeField] private Sprite _nearForestDrought;

        [Header("Background Sprites (Этап 3+ засухи)")]
        [SerializeField] private Sprite _backgroundHealthy;
        [SerializeField] private Sprite _backgroundDrought;

        [Header("Settings")]
        [Tooltip("Также менять фон на засушливый, если соседнее поле достигло 3-й стадии засухи или разрушено")]
        [SerializeField] private bool _allowFieldDroughtToAffectSky = true;

        private void LateUpdate()
        {
            UpdateForestVisual();
        }

        public void UpdateForestVisual()
        {
            var dcm = DayCycleManager.Instance != null ? DayCycleManager.Instance : FindAnyObjectByType<DayCycleManager>();
            if (dcm == null) return;

            var forestModel = dcm.GetZoneModel(ZoneType.Forest);
            var fieldModel = dcm.GetZoneModel(ZoneType.Field);

            bool isForestDestroyed = forestModel != null && forestModel.isDestroyed;
            bool isForestDrought = forestModel != null && forestModel.activeThreat != null && forestModel.activeThreat.definition.type == ThreatType.Drought;
            int forestDroughtStage = isForestDrought ? forestModel.activeThreat.currentStage : 0;

            bool isFieldDroughtStage3OrDestroyed = _allowFieldDroughtToAffectSky && fieldModel != null &&
                (fieldModel.isDestroyed || (fieldModel.activeThreat != null && fieldModel.activeThreat.definition.type == ThreatType.Drought && fieldModel.activeThreat.currentStage >= 3));

            // 1. ДАЛЕКИЕ ДЕРЕВЬЯ (лес 1 задник): засыхают на 1-м этапе засухи леса
            if (_farForestRenderer != null)
            {
                if (isForestDestroyed || forestDroughtStage >= 1)
                {
                    if (_farForestDrought != null) _farForestRenderer.sprite = _farForestDrought;
                }
                else
                {
                    if (_farForestHealthy != null) _farForestRenderer.sprite = _farForestHealthy;
                }
            }

            // 2. ПЕРЕДНИЙ ЛЕС (лес 1 перед): засыхает на 2-м этапе засухи леса
            if (_nearForestRenderer != null)
            {
                if (isForestDestroyed || forestDroughtStage >= 2)
                {
                    if (_nearForestDrought != null) _nearForestRenderer.sprite = _nearForestDrought;
                }
                else
                {
                    if (_nearForestHealthy != null) _nearForestRenderer.sprite = _nearForestHealthy;
                }
            }

            // 3. ФОН / НЕБО (Map 3 background): засыхает на 3-м этапе засухи (последний шанс спасти лес)
            if (_backgroundRenderer != null)
            {
                if (isForestDestroyed || forestDroughtStage >= 3 || isFieldDroughtStage3OrDestroyed)
                {
                    if (_backgroundDrought != null) _backgroundRenderer.sprite = _backgroundDrought;
                }
                else
                {
                    if (_backgroundHealthy != null) _backgroundRenderer.sprite = _backgroundHealthy;
                }
            }
        }
    }
}
