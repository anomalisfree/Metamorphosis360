using System;
using System.Collections.Generic;
using Firebase.Database;
using Firebase.Extensions;
using Main.Domain;
using UnityEngine;

namespace Main.Services
{
    public sealed class EventsService : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private FirebaseDatabaseService firebaseService;
        [SerializeField] private LocationService locationService;

        [Header("Settings")]
        [SerializeField] private float visibilityRadiusKm = 5f;
        [SerializeField] private bool showExpiredEvents = false;

        public IReadOnlyDictionary<string, EventData> Events => _events;

        public event Action<string, EventData> OnEventAppeared;
        public event Action<string, EventData> OnEventUpdated;
        public event Action<string> OnEventRemoved;

        private readonly Dictionary<string, EventData> _events = new();
        private bool _isSubscribed;

        private void OnEnable()
        {
            if (firebaseService != null)
            {
                if (firebaseService.IsInitialized)
                {
                    SubscribeToEvents();
                }
                else
                {
                    firebaseService.OnInitialized += SubscribeToEvents;
                }
            }
        }

        private void OnDisable()
        {
            if (firebaseService != null)
            {
                firebaseService.OnInitialized -= SubscribeToEvents;
            }

            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            if (_isSubscribed)
                return;

            firebaseService.SubscribeToEvents(
                OnEventAdded,
                OnEventChanged,
                OnEventRemovedFromDb
            );

            _isSubscribed = true;
        }

        private void UnsubscribeFromEvents()
        {
            _events.Clear();
            _isSubscribed = false;
        }

        private void OnEventAdded(string eventId, string json)
        {
            var eventData = JsonUtility.FromJson<EventData>(json);
            eventData.Id = eventId;

            if (!IsEventValid(eventData))
                return;

            if (!IsEventNearby(eventData))
                return;

            _events[eventId] = eventData;
            OnEventAppeared?.Invoke(eventId, eventData);
        }

        private void OnEventChanged(string eventId, string json)
        {
            var eventData = JsonUtility.FromJson<EventData>(json);
            eventData.Id = eventId;

            if (!IsEventValid(eventData))
            {
                RemoveEvent(eventId);
                return;
            }

            var isNearby = IsEventNearby(eventData);
            var wasTracked = _events.ContainsKey(eventId);

            if (isNearby)
            {
                _events[eventId] = eventData;

                if (wasTracked)
                {
                    OnEventUpdated?.Invoke(eventId, eventData);
                }
                else
                {
                    OnEventAppeared?.Invoke(eventId, eventData);
                }
            }
            else if (wasTracked)
            {
                RemoveEvent(eventId);
            }
        }

        private void OnEventRemovedFromDb(string eventId)
        {
            RemoveEvent(eventId);
        }

        private void RemoveEvent(string eventId)
        {
            if (_events.Remove(eventId))
            {
                OnEventRemoved?.Invoke(eventId);
            }
        }

        private bool IsEventValid(EventData eventData)
        {
            if (eventData == null)
                return false;

            if (!eventData.IsActive)
                return false;

            if (!showExpiredEvents && eventData.IsExpired())
                return false;

            return true;
        }

        private bool IsEventNearby(EventData eventData)
        {
            if (locationService == null || !locationService.IsRunning)
                return true;

            var currentLocation = locationService.CurrentLocation;
            var distance = CalculateDistanceKm(
                currentLocation.x, currentLocation.y,
                eventData.Latitude, eventData.Longitude
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

        public void RefreshEvents()
        {
            var eventsToRemove = new List<string>();

            foreach (var kvp in _events)
            {
                if (!IsEventNearby(kvp.Value) || !IsEventValid(kvp.Value))
                {
                    eventsToRemove.Add(kvp.Key);
                }
            }

            foreach (var eventId in eventsToRemove)
            {
                RemoveEvent(eventId);
            }
        }

        public EventData GetEvent(string eventId)
        {
            return _events.TryGetValue(eventId, out var eventData) ? eventData : null;
        }

        public List<EventData> GetEventsByType(string type)
        {
            var result = new List<EventData>();
            foreach (var kvp in _events)
            {
                if (kvp.Value.Type == type)
                {
                    result.Add(kvp.Value);
                }
            }
            return result;
        }

        public List<EventData> GetActiveEvents()
        {
            var result = new List<EventData>();
            foreach (var kvp in _events)
            {
                if (kvp.Value.IsCurrentlyActive())
                {
                    result.Add(kvp.Value);
                }
            }
            return result;
        }
    }
}
