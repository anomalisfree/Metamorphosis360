using UnityEngine;
using Mapbox.Unity.Map;
using Main.Services;

namespace Main.Presentation.Map
{
    public sealed class MapController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AbstractMap map;
        [SerializeField] private Services.LocationService locationService;
        [SerializeField] private PlayerAvatarController playerAvatarController;

        [Header("Map Settings")]
        [SerializeField] private int initialZoom = 16;
        [SerializeField] private bool centerOnPlayer = true;
        [SerializeField] private float lerpSpeed = 5f;

        private bool _isMapInitialized;
        private Mapbox.Utils.Vector2d _targetLatLon;

        private void Awake()
        {
            if (map != null)
            {
                map.InitializeOnStart = false;
            }
        }

        private void OnEnable()
        {
            if (locationService != null)
            {
                locationService.OnLocationUpdated += HandleLocationUpdated;
                locationService.OnLocationError += HandleLocationError;
            }
        }

        private void OnDisable()
        {
            if (locationService != null)
            {
                locationService.OnLocationUpdated -= HandleLocationUpdated;
                locationService.OnLocationError -= HandleLocationError;
            }
        }

        private void LateUpdate()
        {
            if (!_isMapInitialized || !centerOnPlayer)
                return;

            var currentLatLon = map.CenterLatitudeLongitude;
            var newLat = Lerp(currentLatLon.x, _targetLatLon.x, Time.deltaTime * lerpSpeed);
            var newLon = Lerp(currentLatLon.y, _targetLatLon.y, Time.deltaTime * lerpSpeed);
            var newLatLon = new Mapbox.Utils.Vector2d(newLat, newLon);

            if (System.Math.Abs(currentLatLon.x - newLat) > 0.0000001 ||
                System.Math.Abs(currentLatLon.y - newLon) > 0.0000001)
            {
                map.UpdateMap(newLatLon, map.Zoom);
            }

            UpdatePlayerMarker();
        }

        private void HandleLocationUpdated(Vector2d location)
        {
            var mapboxLatLon = new Mapbox.Utils.Vector2d(location.x, location.y);
            _targetLatLon = mapboxLatLon;

            if (!_isMapInitialized)
            {
                InitializeMap(mapboxLatLon);
            }

            Debug.Log($"[MapController] Location updated: {location}");
        }

        private void HandleLocationError(string error)
        {
            Debug.LogWarning($"[MapController] Location error: {error}");
        }

        private void InitializeMap(Mapbox.Utils.Vector2d latLon)
        {
            Debug.Log($"[MapController] Initializing map at {latLon}, zoom {initialZoom}");

            map.OnInitialized += OnMapInitialized;
            map.Initialize(latLon, initialZoom);
        }

        private void OnMapInitialized()
        {
            map.OnInitialized -= OnMapInitialized;
            _isMapInitialized = true;
            Debug.Log("[MapController] Map initialized successfully");

            UpdatePlayerMarker();
        }

        private void UpdatePlayerMarker()
        {
            if (!_isMapInitialized)
                return;

            var worldPos = map.GeoToWorldPosition(map.CenterLatitudeLongitude, true);

            if (playerAvatarController != null)
            {
                playerAvatarController.UpdateWorldPosition(worldPos);
            }
        }

        private static double Lerp(double a, double b, double t)
        {
            t = System.Math.Max(0, System.Math.Min(1, t));
            return a + (b - a) * t;
        }

        public void CenterOnPlayer()
        {
            if (_isMapInitialized)
            {
                map.UpdateMap(_targetLatLon, map.Zoom);
            }
        }

        public void SetZoom(int zoom)
        {
            if (_isMapInitialized)
            {
                map.UpdateMap(map.CenterLatitudeLongitude, zoom);
            }
        }
    }
}
