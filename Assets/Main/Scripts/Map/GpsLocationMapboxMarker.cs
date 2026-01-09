using System.Collections;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using UnityEngine;

namespace Metamorphosis360.Main.Map
{
	public sealed class GpsLocationMapboxMarker : MonoBehaviour
	{
		[SerializeField] private AbstractMap _map;
		[SerializeField] private Transform _marker;

		[Header("Location")]
		[SerializeField] private float _desiredAccuracyInMeters = 10f;
		[SerializeField] private float _updateDistanceInMeters = 5f;
		[SerializeField] private float _updateIntervalSeconds = 0.5f;

		[Header("Map")]
		[SerializeField] private bool _recenterMapOnFirstFix = true;
		[SerializeField] private bool _snapToTerrain = true;
		[SerializeField] private float _markerHeightOffset = 1f;
		[SerializeField] private Vector3 _fallbackMarkerScale = new Vector3(2f, 2f, 2f);

		private bool _recentered;
		private bool _locationStarted;
		private float _nextUpdateTime;

		private void Awake()
		{
			if (_map == null)
			{
				_map = FindAnyMap();
			}
		}

		private static AbstractMap FindAnyMap()
		{
#if UNITY_2023_1_OR_NEWER
			return FindFirstObjectByType<AbstractMap>();
#else
			return FindObjectOfType<AbstractMap>();
#endif
		}

		private void OnEnable()
		{
			_startRoutine = StartCoroutine(StartLocationService());
		}

		private void OnDisable()
		{
			if (_startRoutine != null)
			{
				StopCoroutine(_startRoutine);
				_startRoutine = null;
			}

			if (_locationStarted)
			{
				Input.location.Stop();
				_locationStarted = false;
			}
		}

		private Coroutine _startRoutine;

		private IEnumerator StartLocationService()
		{
			if (!Input.location.isEnabledByUser)
			{
				Debug.LogWarning("[GpsLocationMapboxMarker] Location services are disabled or permission was denied.");
				yield break;
			}

			Input.location.Start(_desiredAccuracyInMeters, _updateDistanceInMeters);
			_locationStarted = true;

			var maxWaitSeconds = 20f;
			while (Input.location.status == LocationServiceStatus.Initializing && maxWaitSeconds > 0f)
			{
				maxWaitSeconds -= Time.unscaledDeltaTime;
				yield return null;
			}

			if (Input.location.status == LocationServiceStatus.Failed)
			{
				Debug.LogWarning("[GpsLocationMapboxMarker] Unable to determine device location (LocationServiceStatus.Failed)." );
				yield break;
			}

			if (Input.location.status != LocationServiceStatus.Running)
			{
				Debug.LogWarning($"[GpsLocationMapboxMarker] Location service not running. Status: {Input.location.status}");
				yield break;
			}

			_nextUpdateTime = 0f;
		}

		private void Update()
		{
			if (_map == null)
			{
				return;
			}

			if (!_locationStarted || Input.location.status != LocationServiceStatus.Running)
			{
				return;
			}

			if (Time.unscaledTime < _nextUpdateTime)
			{
				return;
			}
			_nextUpdateTime = Time.unscaledTime + Mathf.Max(0.05f, _updateIntervalSeconds);

			var data = Input.location.lastData;
			var latLon = new Vector2d(data.latitude, data.longitude);

			if (_recenterMapOnFirstFix && !_recentered)
			{
				_map.UpdateMap(latLon, _map.Zoom);
				_recentered = true;
			}

			EnsureMarker();

			var worldPos = _map.GeoToWorldPosition(latLon, _snapToTerrain);
			worldPos.y += _markerHeightOffset;
			_marker.position = worldPos;
		}

		private void EnsureMarker()
		{
			if (_marker != null)
			{
				return;
			}

			var markerGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			markerGo.name = "GPS Marker";

			// Put under map root so it moves/scales with the map.
			markerGo.transform.SetParent(_map.Root, true);
			markerGo.transform.localScale = _fallbackMarkerScale;

			_marker = markerGo.transform;
		}
	}
}
