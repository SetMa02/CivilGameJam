using UnityEngine;
using CivilGame.DayCycle;

namespace CivilGame.Threats
{
    public class MountainZoneVisualController : MonoBehaviour
    {
        [Header("Mountain Renderer")]
        [SerializeField] private SpriteRenderer _mountainRenderer;

        [Header("Phase Sprites")]
        [SerializeField] private Sprite _healthySprite;
        [SerializeField] private Sprite _phase1Sprite;
        [SerializeField] private Sprite _phase2Sprite;

        private void LateUpdate()
        {
            UpdateMountainVisual();
        }

        public void UpdateMountainVisual()
        {
            if (_mountainRenderer == null) return;

            var dcm = DayCycleManager.Instance != null ? DayCycleManager.Instance : FindAnyObjectByType<DayCycleManager>();
            if (dcm == null) return;

            var mountainModel = dcm.GetZoneModel(ZoneType.Mountains);
            if (mountainModel == null) return;

            if (mountainModel.isDestroyed)
            {
                // If completely destroyed, show final broken phase or destroyed state
                if (_phase2Sprite != null) _mountainRenderer.sprite = _phase2Sprite;
                return;
            }

            if (mountainModel.activeThreat != null && mountainModel.activeThreat.definition.type == ThreatType.Landslide)
            {
                int stage = mountainModel.activeThreat.currentStage;
                if (stage == 1 && _phase1Sprite != null)
                {
                    _mountainRenderer.sprite = _phase1Sprite;
                }
                else if (stage >= 2 && _phase2Sprite != null)
                {
                    _mountainRenderer.sprite = _phase2Sprite;
                }
                else if (_healthySprite != null)
                {
                    _mountainRenderer.sprite = _healthySprite;
                }
            }
            else
            {
                if (_healthySprite != null)
                {
                    _mountainRenderer.sprite = _healthySprite;
                }
            }
        }
    }
}
