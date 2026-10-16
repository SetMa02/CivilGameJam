using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CivilGame.Lighthouse
{
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(Button))]
    public class NavigationButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Appearance Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float _idleAlpha = 0.35f;

        [Range(0f, 1f)]
        [SerializeField] private float _hoverAlpha = 1.0f;

        [SerializeField] private float _hoverScaleMultiplier = 1.15f;
        [SerializeField] private float _animationSpeed = 12f;

        private Image _image;
        private Button _button;
        private RectTransform _rectTransform;

        private Vector3 _baseScale = Vector3.one;
        private Vector3 _targetScale = Vector3.one;
        private float _targetAlpha;
        private float _currentAlpha;

        private void Awake()
        {
            _image = GetComponent<Image>();
            _button = GetComponent<Button>();
            _rectTransform = GetComponent<RectTransform>();

            _baseScale = _rectTransform.localScale;
            _targetScale = _baseScale;
            _targetAlpha = _idleAlpha;
            _currentAlpha = _idleAlpha;

            ApplyAlpha(_currentAlpha);
        }

        private void Update()
        {
            // Smoothly animate scale
            _rectTransform.localScale = Vector3.Lerp(_rectTransform.localScale, _targetScale, Time.deltaTime * _animationSpeed);

            // Smoothly animate alpha
            _currentAlpha = Mathf.MoveTowards(_currentAlpha, _targetAlpha, Time.deltaTime * _animationSpeed);
            ApplyAlpha(_currentAlpha);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_button.interactable) return;
            _targetScale = _baseScale * _hoverScaleMultiplier;
            _targetAlpha = _hoverAlpha;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _targetScale = _baseScale;
            _targetAlpha = _idleAlpha;
        }

        private void OnDisable()
        {
            _targetScale = _baseScale;
            _rectTransform.localScale = _baseScale;
            _targetAlpha = _idleAlpha;
            _currentAlpha = _idleAlpha;
            ApplyAlpha(_currentAlpha);
        }

        private void ApplyAlpha(float a)
        {
            if (_image != null)
            {
                Color c = _image.color;
                c.a = a;
                _image.color = c;
            }
        }
    }
}
