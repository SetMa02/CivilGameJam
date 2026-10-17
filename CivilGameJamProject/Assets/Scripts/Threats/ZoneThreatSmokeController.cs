using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CivilGame.DayCycle;

namespace CivilGame.Threats
{
    [RequireComponent(typeof(ParticleSystem))]
    public class ZoneThreatSmokeController : MonoBehaviour
    {
        [Header("Particle System")]
        [SerializeField] private ParticleSystem _particleSystem;

        [Header("Monitored Zones")]
        [Tooltip("Если хотя бы в одной из этих зон есть угроза или она разрушена, дым включается.")]
        [SerializeField] private List<ZoneType> _monitoredZones = new List<ZoneType>();

        [Header("Threat Type Filter")]
        [Tooltip("Игнорировать засуху (засуха сушит траву, но не создает дымовой столб)")]
        [SerializeField] private bool _ignoreDrought = true;
        [Tooltip("Игнорировать цунами (цунами — это вода/волна, а не дым)")]
        [SerializeField] private bool _ignoreTsunami = true;

        [Header("Settings")]
        [Tooltip("Очищать активные частицы дыма сразу, когда все зоны исцелены")]
        [SerializeField] private bool _clearWhenHealthy = true;

        public bool IsSmoking { get; private set; } = false;
        public List<ZoneType> MonitoredZones => _monitoredZones;

        private void Awake()
        {
            if (_particleSystem == null)
            {
                _particleSystem = GetComponent<ParticleSystem>();
            }

            if (_particleSystem != null)
            {
                // Disable playOnAwake so Unity does not auto-start particles when GameObject is enabled
                var main = _particleSystem.main;
                main.playOnAwake = false;
            }
        }

        private void Start()
        {
            UpdateSmokeState(true);
        }

        private void OnEnable()
        {
            // Initial check on enable
            UpdateSmokeState(true);
            // Run a delayed check next frame to overcome Unity's internal particle enable lifecycle
            StartCoroutine(EnsureCorrectSmokeStateRoutine());
        }

        private IEnumerator EnsureCorrectSmokeStateRoutine()
        {
            yield return null;
            UpdateSmokeState(true);
        }

        private void LateUpdate()
        {
            UpdateSmokeState(false);
        }

        public void UpdateSmokeState(bool force = false)
        {
            if (_particleSystem == null) return;

            var dcm = DayCycleManager.Instance != null ? DayCycleManager.Instance : FindAnyObjectByType<DayCycleManager>();
            if (dcm == null) return;

            bool shouldSmoke = false;
            foreach (var zoneType in _monitoredZones)
            {
                var model = dcm.GetZoneModel(zoneType);
                if (model == null) continue;

                if (model.isDestroyed)
                {
                    // Sea being destroyed is tsunami/flood, only smoke if not ignoring tsunami
                    if (zoneType == ZoneType.Sea && _ignoreTsunami) continue;
                    shouldSmoke = true;
                    break;
                }

                if (model.activeThreat != null)
                {
                    var threatType = model.activeThreat.definition.type;
                    if (_ignoreDrought && threatType == ThreatType.Drought) continue;
                    if (_ignoreTsunami && threatType == ThreatType.Tsunami) continue;

                    shouldSmoke = true;
                    break;
                }
            }

            IsSmoking = shouldSmoke;
            var emission = _particleSystem.emission;

            if (shouldSmoke)
            {
                if (!_particleSystem.isPlaying || !emission.enabled)
                {
                    emission.enabled = true;
                    _particleSystem.Play();
                }
            }
            else
            {
                // When healthy: MUST be completely stopped and cleared
                if (_particleSystem.isPlaying || emission.enabled || _particleSystem.particleCount > 0 || force)
                {
                    emission.enabled = false;
                    if (_clearWhenHealthy)
                    {
                        _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        _particleSystem.Clear(true);
                    }
                    else
                    {
                        _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    }
                }
            }
        }
    }
}

