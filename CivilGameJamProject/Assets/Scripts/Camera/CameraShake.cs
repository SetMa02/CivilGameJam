using System.Collections;
using UnityEngine;

namespace CivilGame.CameraEffects
{
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        private Vector3 _originalLocalPos;
        private Coroutine _shakeCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _originalLocalPos = transform.localPosition;
        }

        public void Shake(float duration = 2.5f, float intensity = 0.12f)
        {
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
            }
            _shakeCoroutine = StartCoroutine(ShakeRoutine(duration, intensity));
        }

        private IEnumerator ShakeRoutine(float duration, float intensity)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                // Fade out shake intensity near the end
                float currentIntensity = Mathf.Lerp(intensity, 0f, progress);

                float offsetX = (Mathf.PerlinNoise(Time.time * 25f, 0f) * 2f - 1f) * currentIntensity;
                float offsetY = (Mathf.PerlinNoise(0f, Time.time * 25f) * 2f - 1f) * currentIntensity;

                transform.localPosition = new Vector3(_originalLocalPos.x + offsetX, _originalLocalPos.y + offsetY, _originalLocalPos.z);
                yield return null;
            }

            transform.localPosition = _originalLocalPos;
            _shakeCoroutine = null;
        }
    }
}
