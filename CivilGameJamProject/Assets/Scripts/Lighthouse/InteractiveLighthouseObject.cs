using UnityEngine;
using UnityEngine.InputSystem;

namespace CivilGame.Lighthouse
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class InteractiveLighthouseObject : MonoBehaviour
    {
        [Header("Hover Settings")]
        [Tooltip("Scale multiplier on hover")]
        [SerializeField] private float _hoverScaleMultiplier = 1.07f;

        [Tooltip("Smooth animation speed")]
        [SerializeField] private float _animationSpeed = 12f;

        [Header("Outline Settings")]
        [SerializeField] private Color _outlineColor = new Color(1f, 0.85f, 0.2f, 1f); // Warm golden highlight
        [SerializeField] private float _outlineThickness = 3.5f;

        private Vector3 _originalScale;
        private Vector3 _targetScale;
        private SpriteRenderer _spriteRenderer;
        private MaterialPropertyBlock _propBlock;
        private Camera _mainCamera;
        private Collider2D _collider;

        private static readonly int OutlineEnabledProp = Shader.PropertyToID("_OutlineEnabled");
        private static readonly int OutlineColorProp = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineThicknessProp = Shader.PropertyToID("_OutlineThickness");

        private bool _isHovered = false;

        private void Awake()
        {
            _originalScale = transform.localScale;
            _targetScale = _originalScale;
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<Collider2D>();
            _propBlock = new MaterialPropertyBlock();
            _mainCamera = Camera.main;

            UpdateOutline(false);
        }

        private void Start()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }
        }

        private void Update()
        {
            CheckMouseHover();

            // Smoothly interpolate scale
            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * _animationSpeed);
        }

        [Header("Click Events")]
        [SerializeField] private UnityEngine.Events.UnityEvent _onClick;

        public UnityEngine.Events.UnityEvent OnClick => _onClick;

        private void CheckMouseHover()
        {
            if (_collider == null) return;
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return;
            }

            Vector2 mouseScreenPos = Vector2.zero;
            bool mouseWasPressed = false;
            if (Mouse.current != null)
            {
                mouseScreenPos = Mouse.current.position.ReadValue();
                mouseWasPressed = Mouse.current.leftButton.wasPressedThisFrame;
            }

            Vector3 worldPos = _mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, -_mainCamera.transform.position.z));
            Vector2 worldPos2D = new Vector2(worldPos.x, worldPos.y);

            bool hoveringNow = _collider.OverlapPoint(worldPos2D);

            if (hoveringNow != _isHovered)
            {
                _isHovered = hoveringNow;
                SetHoverState(_isHovered);
            }

            if (hoveringNow && mouseWasPressed)
            {
                _onClick?.Invoke();
            }
        }

        private void SetHoverState(bool hovered)
        {
            _targetScale = hovered ? _originalScale * _hoverScaleMultiplier : _originalScale;
            UpdateOutline(hovered);
        }

        private void UpdateOutline(bool enabled)
        {
            if (_spriteRenderer == null) return;

            _spriteRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat(OutlineEnabledProp, enabled ? 1f : 0f);
            _propBlock.SetColor(OutlineColorProp, _outlineColor);
            _propBlock.SetFloat(OutlineThicknessProp, _outlineThickness);
            _spriteRenderer.SetPropertyBlock(_propBlock);
        }

        public void SetOriginalScale(Vector3 scale)
        {
            _originalScale = scale;
            _targetScale = _isHovered ? _originalScale * _hoverScaleMultiplier : _originalScale;
        }

        private void OnDisable()
        {
            _isHovered = false;
            transform.localScale = _originalScale;
            UpdateOutline(false);
        }
    }
}
