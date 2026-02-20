using System.Collections;
using System.Collections.Generic;
using Main.Domain;
using Main.Services;
using UnityEngine;
using Mapbox.Unity.Map;

namespace Main.Presentation.Map
{
    public sealed class EventMarkersController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private AbstractMap map;
        [SerializeField] private EventDetailsPanel eventDetailsPanel;
        [SerializeField] private Services.LocationService locationService;

        [Header("Marker Prefabs (Optional - will create dynamically if null)")]
        [SerializeField] private GameObject defaultMarkerPrefab;
        [SerializeField] private GameObject questMarkerPrefab;
        [SerializeField] private GameObject battleMarkerPrefab;
        [SerializeField] private GameObject socialMarkerPrefab;
        [SerializeField] private GameObject treasureMarkerPrefab;
        [SerializeField] private GameObject bossMarkerPrefab;
        [SerializeField] private GameObject specialMarkerPrefab;

        [Header("Settings")]
        [SerializeField] private float markerHeightOffset = 1f;
        [SerializeField] private float markerScale = 1f;
        [SerializeField] private bool createDynamicMarkers = true;

        public event System.Action<EventData> OnMarkerClicked;

        private readonly Dictionary<string, EventMarker> _markers = new();
        private MarkerTouchHandler _touchHandler;
        private EventsService _eventsService;
        private bool _isSubscribed;

        private void Awake()
        {
            EnsureTouchHandler();
        }

        private void Start()
        {
            StartCoroutine(InitializeWithRetry());
        }

        private IEnumerator InitializeWithRetry()
        {
            float timeout = 10f;
            float elapsed = 0f;
            
            while (EventsService.Instance == null && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
            }

            _eventsService = EventsService.Instance;

            if (_eventsService == null)
            {
                Debug.LogError("[EventMarkersController] EventsService not found");
                yield break;
            }

            if (locationService != null)
            {
                _eventsService.SetLocationService(locationService);
            }

            elapsed = 0f;
            while (!_eventsService.IsInitialized && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
            }

            SubscribeToEvents();
            yield return new WaitForSeconds(1f);
            RestoreMarkersFromEvents();
        }

        private void OnEnable()
        {
            if (_eventsService != null && !_isSubscribed)
            {
                SubscribeToEvents();
                RestoreMarkersFromEvents();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
            ClearAllMarkers();
        }

        private void SubscribeToEvents()
        {
            if (_eventsService == null || _isSubscribed)
                return;

            _eventsService.OnEventAppeared += HandleEventAppeared;
            _eventsService.OnEventUpdated += HandleEventUpdated;
            _eventsService.OnEventRemoved += HandleEventRemoved;
            _isSubscribed = true;
        }

        private void UnsubscribeFromEvents()
        {
            if (_eventsService == null || !_isSubscribed)
                return;

            _eventsService.OnEventAppeared -= HandleEventAppeared;
            _eventsService.OnEventUpdated -= HandleEventUpdated;
            _eventsService.OnEventRemoved -= HandleEventRemoved;
            _isSubscribed = false;
        }

        private void RestoreMarkersFromEvents()
        {
            if (_eventsService == null)
                return;

            foreach (var kvp in _eventsService.Events)
            {
                if (!_markers.ContainsKey(kvp.Key))
                {
                    HandleEventAppeared(kvp.Key, kvp.Value);
                }
            }
        }

        private void EnsureTouchHandler()
        {
            _touchHandler = FindFirstObjectByType<MarkerTouchHandler>();
            
            if (_touchHandler == null)
            {
                _touchHandler = gameObject.AddComponent<MarkerTouchHandler>();
            }

            if (eventDetailsPanel != null)
            {
                _touchHandler.SetEventDetailsPanel(eventDetailsPanel);
            }
        }

        private void LateUpdate()
        {
            if (map == null)
                return;

            foreach (var kvp in _markers)
            {
                UpdateMarkerPosition(kvp.Value);
            }
        }

        private void HandleEventAppeared(string eventId, EventData eventData)
        {
            if (_markers.ContainsKey(eventId))
                return;

            var prefab = GetMarkerPrefab(eventData.Type);
            GameObject markerObj;

            if (prefab != null)
            {
                markerObj = Instantiate(prefab, transform);
            }
            else if (createDynamicMarkers)
            {
                markerObj = CreateDynamicMarker(eventData.Type);
            }
            else
            {
                return;
            }

            markerObj.name = $"Event_{eventId}";
            markerObj.transform.localScale = Vector3.one * markerScale;

            var marker = new EventMarker
            {
                EventId = eventId,
                EventData = eventData,
                GameObject = markerObj
            };

            var clickHandler = markerObj.GetComponent<EventMarkerClickHandler>();

            if (clickHandler == null)
            {
                clickHandler = markerObj.AddComponent<EventMarkerClickHandler>();
            }

            clickHandler.Initialize(eventData, () => OnMarkerClicked?.Invoke(eventData));

            _markers[eventId] = marker;
            UpdateMarkerPosition(marker);
        }

        private void HandleEventUpdated(string eventId, EventData eventData)
        {
            if (!_markers.TryGetValue(eventId, out var marker))
                return;

            marker.EventData = eventData;

            // Обновляем компонент клика
            var clickHandler = marker.GameObject.GetComponent<EventMarkerClickHandler>();
            
            if (clickHandler != null)
            {
                clickHandler.UpdateEventData(eventData);
            }
        }

        private void HandleEventRemoved(string eventId)
        {
            if (!_markers.TryGetValue(eventId, out var marker))
                return;

            if (marker.GameObject != null)
            {
                Destroy(marker.GameObject);
            }

            _markers.Remove(eventId);
        }

        private void UpdateMarkerPosition(EventMarker marker)
        {
            if (marker.GameObject == null || map == null)
                return;

            var latLon = new Mapbox.Utils.Vector2d(
                marker.EventData.Latitude,
                marker.EventData.Longitude
            );

            var worldPos = map.GeoToWorldPosition(latLon, true);
            worldPos.y += markerHeightOffset;

            marker.GameObject.transform.position = worldPos;
        }

        private GameObject GetMarkerPrefab(string eventType)
        {
            return eventType switch
            {
                Domain.EventType.Quest => questMarkerPrefab ?? defaultMarkerPrefab,
                Domain.EventType.Battle => battleMarkerPrefab ?? defaultMarkerPrefab,
                Domain.EventType.Social => socialMarkerPrefab ?? defaultMarkerPrefab,
                Domain.EventType.Treasure => treasureMarkerPrefab ?? defaultMarkerPrefab,
                Domain.EventType.Boss => bossMarkerPrefab ?? defaultMarkerPrefab,
                Domain.EventType.Special => specialMarkerPrefab ?? defaultMarkerPrefab,
                _ => defaultMarkerPrefab
            };
        }

        private GameObject CreateDynamicMarker(string eventType)
        {
            var markerObj = new GameObject("EventMarker");
            markerObj.transform.SetParent(transform);

            var visual = markerObj.AddComponent<EventMarkerVisual>();
            visual.SetEventType(eventType);

            return markerObj;
        }

        private void ClearAllMarkers()
        {
            foreach (var kvp in _markers)
            {
                if (kvp.Value.GameObject != null)
                {
                    Destroy(kvp.Value.GameObject);
                }
            }

            _markers.Clear();
        }

        private class EventMarker
        {
            public string EventId;
            public EventData EventData;
            public GameObject GameObject;
        }
    }
}
