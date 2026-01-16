using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;

namespace Main.Services
{
    public sealed class FirebaseDatabaseService : MonoBehaviour
    {
        private const string DATABASE_URL = "https://metamorphosis360-c7dc2-default-rtdb.firebaseio.com/";

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
                    var database = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance, DATABASE_URL);
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
