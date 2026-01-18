using System;
using System.Collections.Generic;
using Main.Domain;
using Main.Infrastructure;
using UnityEngine;

namespace Main.Services
{
    public sealed class NearbyPlayersService : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private FirebaseDatabaseService firebaseService;
        [SerializeField] private LocationService locationService;

        [Header("Settings")]
        [SerializeField] private float visibilityRadiusKm = 1f;
        [SerializeField] private long staleDataThresholdMs = 5 * 60 * 1000; // 5 minutes

        public IReadOnlyDictionary<string, PlayerLocationData> NearbyPlayers => _nearbyPlayers;

        public event Action<string, PlayerLocationData> OnPlayerAppeared;
        public event Action<string, PlayerLocationData> OnPlayerUpdated;
        public event Action<string> OnPlayerDisappeared;

        private readonly Dictionary<string, PlayerLocationData> _nearbyPlayers = new();
        private string _currentUserId;
        private bool _isSubscribed;

        private void OnEnable()
        {
            var avatarData = AvatarDataRepository.Load();
            _currentUserId = avatarData?.UserId;

            if (firebaseService != null)
            {
                if (firebaseService.IsInitialized)
                {
                    SubscribeToPlayers();
                }
                else
                {
                    firebaseService.OnInitialized += SubscribeToPlayers;
                }
            }
        }

        private void OnDisable()
        {
            if (firebaseService != null)
            {
                firebaseService.OnInitialized -= SubscribeToPlayers;
            }

            UnsubscribeFromPlayers();
        }

        private void SubscribeToPlayers()
        {
            if (_isSubscribed)
                return;

            firebaseService.SubscribeToPlayers(
                OnPlayerAdded,
                OnPlayerChanged,
                OnPlayerRemoved
            );

            _isSubscribed = true;
        }

        private void UnsubscribeFromPlayers()
        {
            _nearbyPlayers.Clear();
            _isSubscribed = false;
        }

        private void OnPlayerAdded(string playerId, string json)
        {
            if (playerId == _currentUserId)
                return;

            var playerData = JsonUtility.FromJson<PlayerLocationData>(json);
            
            if (!IsPlayerValid(playerData))
                return;

            if (!IsPlayerNearby(playerData))
                return;

            _nearbyPlayers[playerId] = playerData;
            OnPlayerAppeared?.Invoke(playerId, playerData);
        }

        private void OnPlayerChanged(string playerId, string json)
        {
            if (playerId == _currentUserId)
                return;

            var playerData = JsonUtility.FromJson<PlayerLocationData>(json);
            
            if (!IsPlayerValid(playerData))
            {
                RemovePlayer(playerId);
                return;
            }

            var isNearby = IsPlayerNearby(playerData);
            var wasTracked = _nearbyPlayers.ContainsKey(playerId);

            if (isNearby)
            {
                _nearbyPlayers[playerId] = playerData;

                if (wasTracked)
                {
                    OnPlayerUpdated?.Invoke(playerId, playerData);
                }
                else
                {
                    OnPlayerAppeared?.Invoke(playerId, playerData);
                }
            }
            else if (wasTracked)
            {
                RemovePlayer(playerId);
            }
        }

        private void OnPlayerRemoved(string playerId)
        {
            RemovePlayer(playerId);
        }

        private void RemovePlayer(string playerId)
        {
            if (_nearbyPlayers.Remove(playerId))
            {
                OnPlayerDisappeared?.Invoke(playerId);
            }
        }

        private bool IsPlayerValid(PlayerLocationData playerData)
        {
            if (playerData == null)
                return false;

            if (!playerData.IsOnline)
                return false;

            if (playerData.IsStale(staleDataThresholdMs))
                return false;

            return true;
        }

        private bool IsPlayerNearby(PlayerLocationData playerData)
        {
            if (locationService == null || !locationService.IsRunning)
                return true; // Can't check distance, include player

            var currentLocation = locationService.CurrentLocation;
            var distance = CalculateDistanceKm(
                currentLocation.x, currentLocation.y,
                playerData.Latitude, playerData.Longitude
            );

            return distance <= visibilityRadiusKm;
        }

        private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthRadiusKm = 6371;

            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLon = (lon2 - lon1) * Math.PI / 180;

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadiusKm * c;
        }

        public void RefreshNearbyPlayers()
        {
            var playersToRemove = new List<string>();

            foreach (var kvp in _nearbyPlayers)
            {
                if (!IsPlayerNearby(kvp.Value) || !IsPlayerValid(kvp.Value))
                {
                    playersToRemove.Add(kvp.Key);
                }
            }

            foreach (var playerId in playersToRemove)
            {
                RemovePlayer(playerId);
            }
        }
    }
}
