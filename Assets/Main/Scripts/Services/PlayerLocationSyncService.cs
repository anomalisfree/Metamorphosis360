using System;
using Main.Domain;
using Main.Infrastructure;
using UnityEngine;

namespace Main.Services
{
    public sealed class PlayerLocationSyncService : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private LocationService locationService;
        [SerializeField] private FirebaseDatabaseService firebaseService;

        [Header("Settings")]
        [SerializeField] private float minUpdateDistanceMeters = 2f;
        [SerializeField] private float maxUpdateIntervalSeconds = 10f;

        public bool IsSyncing { get; private set; }

        public event Action<PlayerLocationData> OnLocationSynced;

        private AvatarData _avatarData;
        private PlayerLocationData _playerLocationData;
        private Vector2d _lastSyncedLocation;
        private float _timeSinceLastSync;
        private bool _isInitialized;

        private void OnEnable()
        {
            if (firebaseService != null)
            {
                if (firebaseService.IsInitialized)
                {
                    Initialize();
                }
                else
                {
                    firebaseService.OnInitialized += Initialize;
                }
            }

            if (locationService != null)
            {
                locationService.OnLocationUpdated += HandleLocationUpdated;
            }
        }

        private void OnDisable()
        {
            if (firebaseService != null)
            {
                firebaseService.OnInitialized -= Initialize;
            }

            if (locationService != null)
            {
                locationService.OnLocationUpdated -= HandleLocationUpdated;
            }

            SetPlayerOffline();
        }

        private void Update()
        {
            if (!IsSyncing)
                return;

            _timeSinceLastSync += Time.deltaTime;

            if (_timeSinceLastSync >= maxUpdateIntervalSeconds)
            {
                SyncLocation(locationService.CurrentLocation, force: true);
            }
        }

        private void Initialize()
        {
            _avatarData = AvatarDataRepository.Load();

            if (_avatarData == null)
            {
                Debug.LogWarning("[PlayerLocationSyncService] No avatar data found, sync disabled");
                return;
            }

            _playerLocationData = new PlayerLocationData(
                _avatarData,
                locationService.CurrentLocation.x,
                locationService.CurrentLocation.y
            );

            firebaseService.SetPlayerLocation(
                _avatarData.UserId,
                _playerLocationData,
                onSuccess: () =>
                {
                    _isInitialized = true;
                    IsSyncing = true;
                    _lastSyncedLocation = locationService.CurrentLocation;

                    firebaseService.SetupPresence(_avatarData.UserId);
                },
                onError: error =>
                {
                    Debug.LogError($"[PlayerLocationSyncService] Failed to initialize: {error}");
                }
            );
        }

        private void HandleLocationUpdated(Vector2d location)
        {
            if (!_isInitialized || !IsSyncing)
                return;

            SyncLocation(location, force: false);
        }

        private void SyncLocation(Vector2d location, bool force)
        {
            var distance = CalculateDistanceMeters(_lastSyncedLocation, location);

            if (!force && distance < minUpdateDistanceMeters)
                return;

            _playerLocationData.UpdateLocation(location.x, location.y);

            firebaseService.UpdatePlayerLocation(
                _avatarData.UserId,
                location.x,
                location.y,
                onSuccess: () =>
                {
                    _lastSyncedLocation = location;
                    _timeSinceLastSync = 0f;
                    OnLocationSynced?.Invoke(_playerLocationData);
                }
            );
        }

        private void SetPlayerOffline()
        {
            if (_avatarData != null && firebaseService != null && firebaseService.IsInitialized)
            {
                firebaseService.SetPlayerOffline(_avatarData.UserId);
            }

            IsSyncing = false;
        }

        public void PauseSync()
        {
            IsSyncing = false;
        }

        public void ResumeSync()
        {
            if (_isInitialized)
            {
                IsSyncing = true;
            }
        }

        private static double CalculateDistanceMeters(Vector2d from, Vector2d to)
        {
            const double earthRadiusMeters = 6371000;

            var dLat = (to.x - from.x) * Math.PI / 180;
            var dLon = (to.y - from.y) * Math.PI / 180;

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(from.x * Math.PI / 180) * Math.Cos(to.x * Math.PI / 180) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadiusMeters * c;
        }
    }
}
