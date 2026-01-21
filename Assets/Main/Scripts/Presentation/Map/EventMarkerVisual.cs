using UnityEngine;
using Main.Domain;
using DomainEventType = Main.Domain.EventType;

namespace Main.Presentation.Map
{
    public class EventMarkerVisual : MonoBehaviour
    {
        [Header("Marker Settings")]
        [SerializeField] private float _markerHeight = 3f;
        [SerializeField] private float _pinRadius = 0.3f;
        [SerializeField] private float _headRadius = 0.8f;
        [SerializeField] private float _bobSpeed = 2f;
        [SerializeField] private float _bobAmount = 0.3f;
        [SerializeField] private float _rotateSpeed = 30f;

        [Header("Type Colors")]
        [SerializeField] private Color _questColor = new(0.2f, 0.6f, 1f);
        [SerializeField] private Color _battleColor = new(1f, 0.2f, 0.2f);
        [SerializeField] private Color _socialColor = new(0.2f, 1f, 0.4f);
        [SerializeField] private Color _treasureColor = new(1f, 0.8f, 0.2f);
        [SerializeField] private Color _bossColor = new(0.6f, 0.1f, 0.8f);
        [SerializeField] private Color _specialColor = new(1f, 0.5f, 0f);
        [SerializeField] private Color _defaultColor = new(0.5f, 0.5f, 0.5f);

        private GameObject _pinObject;
        private GameObject _headObject;
        private MeshRenderer _headRenderer;
        private float _initialY;
        private string _currentType;

        private void Awake()
        {
            CreateMarkerVisuals();
        }

        private void Start()
        {
            _initialY = transform.position.y;
        }

        private void Update()
        {
            // Bobbing animation
            var newY = _initialY + Mathf.Sin(Time.time * _bobSpeed) * _bobAmount;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);

            // Rotation
            _headObject.transform.Rotate(Vector3.up, _rotateSpeed * Time.deltaTime);
        }

        private void CreateMarkerVisuals()
        {
            // Create pin (cylinder)
            _pinObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _pinObject.name = "Pin";
            _pinObject.transform.SetParent(transform);
            _pinObject.transform.localPosition = new Vector3(0, _markerHeight / 2f, 0);
            _pinObject.transform.localScale = new Vector3(_pinRadius, _markerHeight / 2f, _pinRadius);

            // Remove collider from pin (we'll use click handler on parent)
            var pinCollider = _pinObject.GetComponent<Collider>();
            if (pinCollider != null)
                Destroy(pinCollider);

            var pinRenderer = _pinObject.GetComponent<MeshRenderer>();
            pinRenderer.material = CreateUnlitMaterial(new Color(0.3f, 0.3f, 0.3f));

            // Create head (sphere)
            _headObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _headObject.name = "Head";
            _headObject.transform.SetParent(transform);
            _headObject.transform.localPosition = new Vector3(0, _markerHeight + _headRadius, 0);
            _headObject.transform.localScale = Vector3.one * _headRadius * 2f;

            // Remove collider from head
            var headCollider = _headObject.GetComponent<Collider>();
            if (headCollider != null)
                Destroy(headCollider);

            _headRenderer = _headObject.GetComponent<MeshRenderer>();
            _headRenderer.material = CreateUnlitMaterial(_defaultColor);

            // Add main collider for click detection
            var mainCollider = gameObject.AddComponent<CapsuleCollider>();
            mainCollider.center = new Vector3(0, _markerHeight / 2f + _headRadius, 0);
            mainCollider.height = _markerHeight + _headRadius * 2f;
            mainCollider.radius = _headRadius;
        }

        private Material CreateUnlitMaterial(Color color)
        {
            // Use Unlit/Color shader for visibility
            var shader = Shader.Find("Unlit/Color");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader);
            material.color = color;
            return material;
        }

        public void SetEventType(string eventType)
        {
            if (_currentType == eventType)
                return;

            _currentType = eventType;
            var color = GetColorForType(eventType);

            if (_headRenderer != null)
            {
                _headRenderer.material.color = color;
            }
        }

        private Color GetColorForType(string eventType)
        {
            return eventType switch
            {
                DomainEventType.Quest => _questColor,
                DomainEventType.Battle => _battleColor,
                DomainEventType.Social => _socialColor,
                DomainEventType.Treasure => _treasureColor,
                DomainEventType.Boss => _bossColor,
                DomainEventType.Special => _specialColor,
                _ => _defaultColor
            };
        }

        public void SetSelected(bool selected)
        {
            // Scale up when selected
            var scale = selected ? 1.3f : 1f;
            transform.localScale = Vector3.one * scale;
        }
    }
}
