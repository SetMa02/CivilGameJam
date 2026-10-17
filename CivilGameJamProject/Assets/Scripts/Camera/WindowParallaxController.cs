using UnityEngine;
using UnityEngine.InputSystem;

namespace CivilGame.CameraEffects
{
    public class WindowParallaxController : MonoBehaviour
    {
        public static WindowParallaxController Instance { get; private set; }

        [Header("Parallax Limits")]
        [SerializeField] private float _maxOffsetX = 0.55f;
        [SerializeField] private float _maxOffsetY = 0.35f;
        [SerializeField] private float _smoothSpeed = 5.0f;

        [Header("State")]
        [SerializeField] private bool _isParallaxEnabled = false;

        private Vector3 _originalCameraPos;
        private Vector3 _targetCameraPos;
        private Transform _cameraTransform;

        public bool IsParallaxEnabled => _isParallaxEnabled;
        public bool IsLockedBySequence { get; set; } = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            var cam = Camera.main;
            if (cam != null)
            {
                _cameraTransform = cam.transform;
                _originalCameraPos = _cameraTransform.position;
                _targetCameraPos = _originalCameraPos;
            }
        }

        private void Update()
        {
            if (_cameraTransform == null || IsLockedBySequence) return;

            if (_isParallaxEnabled && Mouse.current != null)
            {
                Vector2 mousePos = Mouse.current.position.ReadValue();
                float normX = (mousePos.x / Screen.width - 0.5f) * 2f;
                float normY = (mousePos.y / Screen.height - 0.5f) * 2f;

                // Clamp between -1 and 1
                normX = Mathf.Clamp(normX, -1f, 1f);
                normY = Mathf.Clamp(normY, -1f, 1f);

                _targetCameraPos = _originalCameraPos + new Vector3(normX * _maxOffsetX, normY * _maxOffsetY, 0f);
            }
            else
            {
                // Smoothly return to center when parallax is disabled (e.g. in Hall)
                _targetCameraPos = _originalCameraPos;
            }

            _cameraTransform.position = Vector3.Lerp(_cameraTransform.position, _targetCameraPos, Time.deltaTime * _smoothSpeed);
        }

        public void SetParallaxActive(bool active)
        {
            _isParallaxEnabled = active;
        }

        public void ResetToCenterImmediate()
        {
            if (_cameraTransform != null)
            {
                _cameraTransform.position = _originalCameraPos;
                _targetCameraPos = _originalCameraPos;
            }
        }
    }
}
