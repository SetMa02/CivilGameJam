using UnityEngine;
using TMPro;

namespace CivilGame.Threats
{
    public class ZoneView : MonoBehaviour
    {
        [Header("Zone Configuration")]
        [SerializeField] private ZoneType _zoneType;
        [SerializeField] private SpriteRenderer _indicatorRenderer;
        [SerializeField] private TextMeshPro _zoneLabel;
        [SerializeField] private TextMeshPro _statusLabel;

        public ZoneType ZoneType => _zoneType;

        public void Initialize(ZoneType type)
        {
            _zoneType = type;
        }

        public void UpdateView(ZoneModel model)
        {
            if (model == null) return;

            Color targetColor = model.GetIndicatorColor();
            if (_indicatorRenderer != null)
            {
                _indicatorRenderer.color = targetColor;
            }

            if (_zoneLabel != null)
            {
                _zoneLabel.text = CivilGame.Localization.LocalizationManager.GetZoneName(model.zoneType).ToUpper();
            }

            if (_statusLabel != null)
            {
                if (model.isDestroyed)
                {
                    _statusLabel.text = CivilGame.Localization.LocalizationManager.Get("status.destroyed");
                }
                else if (model.activeThreat != null)
                {
                    string threatName = CivilGame.Localization.LocalizationManager.GetThreatName(model.activeThreat.definition.type);
                    string stageDesc = model.activeThreat.GetStageDescription();
                    string colorTag = model.activeThreat.IsFinalStage ? "<color=#ef4444>" : "<color=#fef08a>";
                    _statusLabel.text = $"{colorTag}<b>{threatName}</b>\n{stageDesc}</color>";
                }
                else
                {
                    _statusLabel.text = CivilGame.Localization.LocalizationManager.Get("status.calm");
                }
            }
        }
    }
}
