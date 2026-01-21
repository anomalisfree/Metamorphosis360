using System;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using Main.Core;
using UnityEngine;

namespace Main.Services
{
    public sealed class FirebaseDatabaseService : MonoBehaviour
    {

        public static FirebaseDatabaseService Instance { get; private set; }

        public bool IsInitialized { get; private set; }

        public event Action OnInitialized;
        public event Action<string> OnError;

        private DatabaseReference _database;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InitializeFirebase();
        }

        private void InitializeFirebase()
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                var dependencyStatus = task.Result;
                if (dependencyStatus == DependencyStatus.Available)
                {
                    var databaseUrl = FirebaseConfigReader.DatabaseUrl;
                    var database = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance, databaseUrl);
                    _database = database.RootReference;
                    IsInitialized = true;
                    OnInitialized?.Invoke();
                }
                else
                {
                    var error = $"Could not resolve Firebase dependencies: {dependencyStatus}";
                    Debug.LogError($"[FirebaseDatabaseService] {error}");
                    OnError?.Invoke(error);
                }
            });
        }

        #region Players

        public void SetPlayerLocation(string playerId, object data, Action onSuccess = null, Action<string> onError = null)
        {
            if (!IsInitialized)
            {
                onError?.Invoke("Firebase not initialized");
                return;
            }

            var json = JsonUtility.ToJson(data);
            _database.Child("players").Child(playerId).SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    var error = task.Exception?.Message ?? "Unknown error";
                    Debug.LogError($"[FirebaseDatabaseService] Failed to set player location: {error}");
                    onError?.Invoke(error);
                }
                else
                {
                    onSuccess?.Invoke();
                }
            });
        }

        public void UpdatePlayerLocation(string playerId, double latitude, double longitude, Action onSuccess = null, Action<string> onError = null)
        {
            if (!IsInitialized)
            {
                onError?.Invoke("Firebase not initialized");
                return;
            }

            var updates = new System.Collections.Generic.Dictionary<string, object>
            {
                ["Latitude"] = latitude,
                ["Longitude"] = longitude,
                ["UpdatedAt"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                ["IsOnline"] = true
            };

            _database.Child("players").Child(playerId).UpdateChildrenAsync(updates).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    var error = task.Exception?.Message ?? "Unknown error";
                    Debug.LogError($"[FirebaseDatabaseService] Failed to update player location: {error}");
                    onError?.Invoke(error);
                }
                else
                {
                    onSuccess?.Invoke();
                }
            });
        }

        public void SetPlayerOffline(string playerId)
        {
            if (!IsInitialized || string.IsNullOrEmpty(playerId))
                return;

            var updates = new System.Collections.Generic.Dictionary<string, object>
            {
                ["IsOnline"] = false,
                ["UpdatedAt"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            _database.Child("players").Child(playerId).UpdateChildrenAsync(updates);
        }

        public void SetupPresence(string playerId)
        {
            if (!IsInitialized || string.IsNullOrEmpty(playerId))
                return;

            var playerRef = _database.Child("players").Child(playerId);
            var connectedRef = FirebaseDatabase.DefaultInstance.GetReference(".info/connected");

            connectedRef.ValueChanged += (sender, args) =>
            {
                if (args.Snapshot.Value != null && (bool)args.Snapshot.Value)
                {
                    var offlineData = new System.Collections.Generic.Dictionary<string, object>
                    {
                        ["IsOnline"] = false,
                        ["Timestamp"] = ServerValue.Timestamp
                    };

                    playerRef.OnDisconnect().UpdateChildren(offlineData);

                    playerRef.Child("IsOnline").SetValueAsync(true);
                }
            };
        }

        public void SubscribeToPlayers(
            Action<string, string> onPlayerAdded,
            Action<string, string> onPlayerChanged,
            Action<string> onPlayerRemoved)
        {
            if (!IsInitialized)
                return;

            var playersRef = _database.Child("players");

            playersRef.ChildAdded += (sender, args) =>
            {
                if (args.Snapshot.Exists)
                {
                    var json = args.Snapshot.GetRawJsonValue();
                    onPlayerAdded?.Invoke(args.Snapshot.Key, json);
                }
            };

            playersRef.ChildChanged += (sender, args) =>
            {
                if (args.Snapshot.Exists)
                {
                    var json = args.Snapshot.GetRawJsonValue();
                    onPlayerChanged?.Invoke(args.Snapshot.Key, json);
                }
            };

            playersRef.ChildRemoved += (sender, args) =>
            {
                onPlayerRemoved?.Invoke(args.Snapshot.Key);
            };
        }

        public void SubscribeToPlayersInArea(double centerLat, double centerLon, double radiusKm, 
            Action<string, object> onPlayerAdded, 
            Action<string, object> onPlayerChanged,
            Action<string> onPlayerRemoved)
        {
            if (!IsInitialized)
                return;

            var playersRef = _database.Child("players");

            playersRef.ChildAdded += (sender, args) =>
            {
                if (args.Snapshot.Exists)
                {
                    var json = args.Snapshot.GetRawJsonValue();
                    onPlayerAdded?.Invoke(args.Snapshot.Key, json);
                }
            };

            playersRef.ChildChanged += (sender, args) =>
            {
                if (args.Snapshot.Exists)
                {
                    var json = args.Snapshot.GetRawJsonValue();
                    onPlayerChanged?.Invoke(args.Snapshot.Key, json);
                }
            };

            playersRef.ChildRemoved += (sender, args) =>
            {
                onPlayerRemoved?.Invoke(args.Snapshot.Key);
            };
        }

        #endregion

        #region Events

        public void SubscribeToEvents(
            Action<string, string> onEventAdded,
            Action<string, string> onEventChanged,
            Action<string> onEventRemoved)
        {
            if (!IsInitialized)
                return;

            var eventsRef = _database.Child("events");

            eventsRef.ChildAdded += (sender, args) =>
            {
                if (args.Snapshot.Exists)
                {
                    var json = args.Snapshot.GetRawJsonValue();
                    onEventAdded?.Invoke(args.Snapshot.Key, json);
                }
            };

            eventsRef.ChildChanged += (sender, args) =>
            {
                if (args.Snapshot.Exists)
                {
                    var json = args.Snapshot.GetRawJsonValue();
                    onEventChanged?.Invoke(args.Snapshot.Key, json);
                }
            };

            eventsRef.ChildRemoved += (sender, args) =>
            {
                onEventRemoved?.Invoke(args.Snapshot.Key);
            };
        }

        public void CreateEvent(object eventData, Action<string> onSuccess = null, Action<string> onError = null)
        {
            if (!IsInitialized)
            {
                onError?.Invoke("Firebase not initialized");
                return;
            }

            var newEventRef = _database.Child("events").Push();
            var eventId = newEventRef.Key;

            var json = JsonUtility.ToJson(eventData);
            newEventRef.SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    var error = task.Exception?.Message ?? "Unknown error";
                    Debug.LogError($"[FirebaseDatabaseService] Failed to create event: {error}");
                    onError?.Invoke(error);
                }
                else
                {
                    onSuccess?.Invoke(eventId);
                }
            });
        }

        public void UpdateEvent(string eventId, object eventData, Action onSuccess = null, Action<string> onError = null)
        {
            if (!IsInitialized)
            {
                onError?.Invoke("Firebase not initialized");
                return;
            }

            var json = JsonUtility.ToJson(eventData);
            _database.Child("events").Child(eventId).SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    var error = task.Exception?.Message ?? "Unknown error";
                    Debug.LogError($"[FirebaseDatabaseService] Failed to update event: {error}");
                    onError?.Invoke(error);
                }
                else
                {
                    onSuccess?.Invoke();
                }
            });
        }

        public void DeleteEvent(string eventId, Action onSuccess = null, Action<string> onError = null)
        {
            if (!IsInitialized)
            {
                onError?.Invoke("Firebase not initialized");
                return;
            }

            _database.Child("events").Child(eventId).RemoveValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    var error = task.Exception?.Message ?? "Unknown error";
                    Debug.LogError($"[FirebaseDatabaseService] Failed to delete event: {error}");
                    onError?.Invoke(error);
                }
                else
                {
                    onSuccess?.Invoke();
                }
            });
        }

        public void GetAllEvents(Action<string> onSuccess, Action<string> onError = null)
        {
            if (!IsInitialized)
            {
                onError?.Invoke("Firebase not initialized");
                return;
            }

            _database.Child("events").GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    var error = task.Exception?.Message ?? "Unknown error";
                    Debug.LogError($"[FirebaseDatabaseService] Failed to get events: {error}");
                    onError?.Invoke(error);
                }
                else if (task.Result.Exists)
                {
                    onSuccess?.Invoke(task.Result.GetRawJsonValue());
                }
                else
                {
                    onSuccess?.Invoke("{}");
                }
            });
        }

        #endregion

        private void OnApplicationPause(bool pauseStatus)
        {
            // Handle app pause/resume for presence
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
