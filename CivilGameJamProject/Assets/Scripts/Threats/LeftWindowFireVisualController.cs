using System.Collections;
using UnityEngine;
using CivilGame.DayCycle;

namespace CivilGame.Threats
{
    public class LeftWindowFireVisualController : MonoBehaviour
    {
        [Header("Forest Fire Particles")]
        [Tooltip("Партиклы среднего пожара в лесу (Этап 2)")]
        [SerializeField] private ParticleSystem _mediumFireForest;
        [Tooltip("Партиклы сильного / верхового пожара в лесу (Этап 3 / Разрушение)")]
        [SerializeField] private ParticleSystem _bigFireForest;

        [Header("Field Fire Particles")]
        [Tooltip("Партиклы малого возгорания на поляне (Этап 1)")]
        [SerializeField] private ParticleSystem _smallFireField;
        [Tooltip("Партиклы среднего пожара на поляне (Этап 2)")]
        [SerializeField] private ParticleSystem _mediumFireField;
        [Tooltip("Партиклы сильного пожара на поляне (Этап 3 / Разрушение)")]
        [SerializeField] private ParticleSystem _bigFireField;

        [Header("Renderers for Charring / Blackening")]
        [Tooltip("Спрайт заднего леса (лес 1 задник)")]
        [SerializeField] private SpriteRenderer _farForestRenderer;
        [Tooltip("Спрайт переднего леса (лес 1 перед)")]
        [SerializeField] private SpriteRenderer _nearForestRenderer;
        [Tooltip("Спрайт травы поляны (луг 1)")]
        [SerializeField] private SpriteRenderer _meadowRenderer;

        [Header("Charring Colors")]
        [SerializeField] private Color _healthyColor = Color.white;
        [SerializeField] private Color _stage1CharColor = new Color(0.78f, 0.74f, 0.70f, 1f); // Легкий нагар / дымка
        [SerializeField] private Color _stage2CharColor = new Color(0.48f, 0.42f, 0.38f, 1f); // Сильное обугливание
        [SerializeField] private Color _stage3CharColor = new Color(0.20f, 0.17f, 0.17f, 1f); // Почти черное пепелище

        private void Awake()
        {
            EnsureParticleSetup(_mediumFireForest);
            EnsureParticleSetup(_bigFireForest);
            EnsureParticleSetup(_smallFireField);
            EnsureParticleSetup(_mediumFireField);
            EnsureParticleSetup(_bigFireField);
        }

        private void EnsureParticleSetup(ParticleSystem ps)
        {
            if (ps == null) return;
            var main = ps.main;
            main.playOnAwake = false;
            ps.gameObject.SetActive(true);

            var em = ps.emission;
            em.enabled = false;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Clear(true);
        }

        private void OnEnable()
        {
            UpdateFireVisuals(true);
            StartCoroutine(EnsureFireStateRoutine());
        }

        private IEnumerator EnsureFireStateRoutine()
        {
            yield return null;
            UpdateFireVisuals(true);
        }

        private void LateUpdate()
        {
            UpdateFireVisuals(false);
        }

        public void UpdateFireVisuals(bool force = false)
        {
            var dcm = DayCycleManager.Instance != null ? DayCycleManager.Instance : FindAnyObjectByType<DayCycleManager>();
            if (dcm == null) return;

            var forestModel = dcm.GetZoneModel(ZoneType.Forest);
            var fieldModel = dcm.GetZoneModel(ZoneType.Field);

            UpdateForestFire(forestModel, force);
            UpdateFieldFire(fieldModel, force);
        }

        private void UpdateForestFire(ZoneModel forestModel, bool force)
        {
            int fireStage = 0;
            if (forestModel != null)
            {
                if (forestModel.isDestroyed)
                {
                    fireStage = 3;
                }
                else if (forestModel.activeThreat != null && forestModel.activeThreat.definition.type == ThreatType.Wildfire)
                {
                    fireStage = forestModel.activeThreat.currentStage;
                }
            }

            // Управление партиклами леса
            // Этап 1: Только дым (обрабатывается ZoneThreatSmokeController), огонь выключен
            // Этап 2: MediomFireForest
            // Этап 3+: BigFireForest
            bool playMedium = (fireStage == 2);
            bool playBig = (fireStage >= 3);

            SetParticleActive(_mediumFireForest, playMedium, force);
            SetParticleActive(_bigFireForest, playBig, force);

            // Обугливание деревьев в зависимости от стадии
            Color targetTreeColor = _healthyColor;
            if (fireStage == 1) targetTreeColor = _stage1CharColor;
            else if (fireStage == 2) targetTreeColor = _stage2CharColor;
            else if (fireStage >= 3) targetTreeColor = _stage3CharColor;

            if (_farForestRenderer != null) _farForestRenderer.color = targetTreeColor;
            if (_nearForestRenderer != null) _nearForestRenderer.color = targetTreeColor;
        }

        private void UpdateFieldFire(ZoneModel fieldModel, bool force)
        {
            int fireStage = 0;
            if (fieldModel != null)
            {
                if (fieldModel.isDestroyed)
                {
                    // Если поле уничтожено пожаром
                    if (fieldModel.activeThreat != null && fieldModel.activeThreat.definition.type == ThreatType.Wildfire)
                    {
                        fireStage = 3;
                    }
                    else
                    {
                        fireStage = 0;
                    }
                }
                else if (fieldModel.activeThreat != null && fieldModel.activeThreat.definition.type == ThreatType.Wildfire)
                {
                    fireStage = fieldModel.activeThreat.currentStage;
                }
            }

            // Управление партиклами поляны
            // Этап 1: SmallFireField
            // Этап 2: MediomFireField
            // Этап 3+: BigFireField
            bool playSmall = (fireStage == 1);
            bool playMedium = (fireStage == 2);
            bool playBig = (fireStage >= 3);

            SetParticleActive(_smallFireField, playSmall, force);
            SetParticleActive(_mediumFireField, playMedium, force);
            SetParticleActive(_bigFireField, playBig, force);

            // Обугливание травы поляны в зависимости от стадии
            Color targetMeadowColor = _healthyColor;
            if (fireStage == 1) targetMeadowColor = _stage1CharColor;
            else if (fireStage == 2) targetMeadowColor = _stage2CharColor;
            else if (fireStage >= 3) targetMeadowColor = _stage3CharColor;

            if (_meadowRenderer != null) _meadowRenderer.color = targetMeadowColor;
        }

        private void SetParticleActive(ParticleSystem ps, bool active, bool force)
        {
            if (ps == null) return;

            var em = ps.emission;
            if (active)
            {
                if (!ps.isPlaying || !em.enabled || force)
                {
                    em.enabled = true;
                    if (!ps.isPlaying) ps.Play();
                }
            }
            else
            {
                if (ps.isPlaying || em.enabled || ps.particleCount > 0 || force)
                {
                    em.enabled = false;
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.Clear(true);
                }
            }
        }
    }
}
