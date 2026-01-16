using System;
using System.Collections;
using UnityEngine;

namespace Main.Services
{
    public sealed class LocationService : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float desiredAccuracyMeters = 5f;
        [SerializeField] private float updateDistanceMeters = 5f;
        [SerializeField] private float updateIntervalSeconds = 1f;
        [SerializeField] private float initTimeoutSeconds = 20f;

        [Header("Editor Testing")]
        [Tooltip("Use these coordinates when running in the Unity Editor")]
        [SerializeField] private double editorLatitude = 54.350178; 
        [SerializeField] private double editorLongitude = 18.650743;

        public Vector2d CurrentLocation { get; private set; }

        public float Accuracy { get; private set; }

        public bool IsRunning { get; private set; }

        public event Action<Vector2d> OnLocationUpdated;

        public event Action<string> OnLocationError;

        private Coroutine _updateCoroutine;

        private void OnEnable()
        {
            StartLocationUpdates();
        }

        private void OnDisable()
        {
            StopLocationUpdates();
        }

        public void StartLocationUpdates()
        {
            if (_updateCoroutine != null)
                return;

            _updateCoroutine = StartCoroutine(LocationUpdateRoutine());
        }

        public void StopLocationUpdates()
        {
            if (_updateCoroutine != null)
            {
                StopCoroutine(_updateCoroutine);
                _updateCoroutine = null;
            }

            if (Input.location.status == LocationServiceStatus.Running)
            {
                Input.location.Stop();
            }

            IsRunning = false;
        }

        private IEnumerator LocationUpdateRoutine()
        {
#if UNITY_EDITOR
            IsRunning = true;
            CurrentLocation = new Vector2d(editorLatitude, editorLongitude);
            Accuracy = 1f;
            OnLocationUpdated?.Invoke(CurrentLocation);
            Debug.Log($"[LocationService] Editor mode: using fake location {CurrentLocation}");

            while (true)
            {
                yield return new WaitForSeconds(updateIntervalSeconds);
                CurrentLocation = new Vector2d(CurrentLocation.x + UnityEngine.Random.Range(-0.0001f, 0.0001f), CurrentLocation.y + UnityEngine.Random.Range(-0.0001f, 0.0001f));
                OnLocationUpdated?.Invoke(CurrentLocation);
            }
#else
            if (!Input.location.isEnabledByUser)
            {
                OnLocationError?.Invoke("Location services disabled by user");
                Debug.LogWarning("[LocationService] Location services disabled by user");
                yield break;
            }

            Input.location.Start(desiredAccuracyMeters, updateDistanceMeters);

            float elapsed = 0f;
            while (Input.location.status == LocationServiceStatus.Initializing && elapsed < initTimeoutSeconds)
            {
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
            }

            if (Input.location.status == LocationServiceStatus.Failed)
            {
                OnLocationError?.Invoke("Unable to determine device location");
                Debug.LogWarning("[LocationService] Unable to determine device location");
                yield break;
            }

            if (Input.location.status == LocationServiceStatus.Initializing)
            {
                OnLocationError?.Invoke("Location initialization timed out");
                Debug.LogWarning("[LocationService] Location initialization timed out");
                Input.location.Stop();
                yield break;
            }

            IsRunning = true;
            Debug.Log("[LocationService] GPS started successfully");

            while (true)
            {
                var data = Input.location.lastData;
                var newLocation = new Vector2d(data.latitude, data.longitude);

                if (!CurrentLocation.Equals(newLocation))
                {
                    CurrentLocation = newLocation;
                    Accuracy = data.horizontalAccuracy;
                    OnLocationUpdated?.Invoke(CurrentLocation);
                    Debug.Log($"[LocationService] Location updated: {CurrentLocation}, accuracy: {Accuracy}m");
                }

                yield return new WaitForSeconds(updateIntervalSeconds);
            }
#endif
        }
    }

    [Serializable]
    public struct Vector2d
    {
        public double x; 
        public double y; 

        public Vector2d(double latitude, double longitude)
        {
            x = latitude;
            y = longitude;
        }

        public override string ToString() => $"({x:F6}, {y:F6})";

        public override bool Equals(object obj)
        {
            if (obj is Vector2d other)
            {
                return Math.Abs(x - other.x) < 0.000001 && Math.Abs(y - other.y) < 0.000001;
            }
            return false;
        }

        public override int GetHashCode() => x.GetHashCode() ^ y.GetHashCode();
    }
}
