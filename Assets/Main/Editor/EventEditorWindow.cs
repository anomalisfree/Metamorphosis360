using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using Main.Core;
using Main.Domain;
using EventType = Main.Domain.EventType;

namespace Main.Editor
{
    public class EventEditorWindow : EditorWindow
    {

        private Vector2 _scrollPosition;
        private bool _isInitialized;
        private bool _isLoading;
        private string _statusMessage = "";
        private MessageType _statusType = MessageType.Info;

        private DatabaseReference _database;
        private List<EventData> _events = new();
        private EventData _selectedEvent;
        private bool _isCreatingNew;

        private string _title = "";
        private string _description = "";
        private string _type = EventType.Quest;
        private double _latitude;
        private double _longitude;
        private DateTime _startDate = DateTime.Now;
        private DateTime _endDate = DateTime.Now.AddHours(24);
        private bool _isActive = true;
        private string _imageUrl = "";
        private string _externalLink = "";
        private int _radius = 50;

        private string[] _eventTypes = new[]
        {
            EventType.Quest,
            EventType.Battle,
            EventType.Social,
            EventType.Treasure,
            EventType.Boss,
            EventType.Special
        };
        private int _selectedTypeIndex;

        [MenuItem("Metamorphosis/Event Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<EventEditorWindow>("Event Editor");
            window.minSize = new Vector2(600, 500);
            window.Show();
        }

        private void OnEnable()
        {
            InitializeFirebase();
        }

        private void InitializeFirebase()
        {
            _isLoading = true;
            _statusMessage = "Initializing Firebase...";

            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.Result == DependencyStatus.Available)
                {
                    var databaseUrl = FirebaseConfigReader.DatabaseUrl;
                    var database = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance, databaseUrl);
                    _database = database.RootReference;
                    _isInitialized = true;
                    _statusMessage = "Firebase initialized";
                    _statusType = MessageType.Info;
                    LoadAllEvents();
                }
                else
                {
                    _statusMessage = $"Firebase initialization failed: {task.Result}";
                    _statusType = MessageType.Error;
                    _isLoading = false;
                }
                Repaint();
            });
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.Width(280));
            DrawEventList();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            DrawEventEditor();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            DrawStatusBar();
        }

        private void DrawEventList()
        {
            EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);

            if (GUILayout.Button("+ New Event"))
            {
                CreateNewEvent();
            }

            if (GUILayout.Button("Refresh"))
            {
                LoadAllEvents();
            }

            EditorGUILayout.Space();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (var evt in _events)
            {
                var isSelected = _selectedEvent != null && _selectedEvent.Id == evt.Id;
                var style = isSelected ? EditorStyles.selectionRect : EditorStyles.helpBox;

                EditorGUILayout.BeginHorizontal(style);

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(evt.Title, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Type: {evt.Type}", EditorStyles.miniLabel);

                var statusColor = evt.IsCurrentlyActive() ? Color.green : 
                                  evt.IsUpcoming() ? Color.yellow : Color.gray;
                var status = evt.IsCurrentlyActive() ? "Active" : 
                             evt.IsUpcoming() ? "Upcoming" : "Expired";

                var oldColor = GUI.color;
                GUI.color = statusColor;
                EditorGUILayout.LabelField(status, EditorStyles.miniLabel);
                GUI.color = oldColor;

                EditorGUILayout.EndVertical();

                if (GUILayout.Button("Edit", GUILayout.Width(40)))
                {
                    SelectEvent(evt);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawEventEditor()
        {
            if (_selectedEvent == null && !_isCreatingNew)
            {
                EditorGUILayout.HelpBox("Select an event or create a new one", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField(_isCreatingNew ? "New Event" : "Edit Event", EditorStyles.boldLabel);

            EditorGUILayout.Space();

            // Basic info
            EditorGUILayout.LabelField("Basic Info", EditorStyles.boldLabel);
            _title = EditorGUILayout.TextField("Title", _title);
            _description = EditorGUILayout.TextField("Description", _description);

            _selectedTypeIndex = Array.IndexOf(_eventTypes, _type);
            if (_selectedTypeIndex < 0) _selectedTypeIndex = 0;
            _selectedTypeIndex = EditorGUILayout.Popup("Type", _selectedTypeIndex, _eventTypes);
            _type = _eventTypes[_selectedTypeIndex];

            _isActive = EditorGUILayout.Toggle("Is Active", _isActive);

            EditorGUILayout.Space();

            // Location
            EditorGUILayout.LabelField("Location", EditorStyles.boldLabel);
            _latitude = EditorGUILayout.DoubleField("Latitude", _latitude);
            _longitude = EditorGUILayout.DoubleField("Longitude", _longitude);
            _radius = EditorGUILayout.IntField("Activation Radius (m)", _radius);

            EditorGUILayout.Space();

            // Time
            EditorGUILayout.LabelField("Time", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Start", GUILayout.Width(50));
            DrawDateTimePicker(ref _startDate);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("End", GUILayout.Width(50));
            DrawDateTimePicker(ref _endDate);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Optional
            EditorGUILayout.LabelField("Optional", EditorStyles.boldLabel);
            _imageUrl = EditorGUILayout.TextField("Image URL", _imageUrl);
            _externalLink = EditorGUILayout.TextField("External Link", _externalLink);

            EditorGUILayout.Space();

            // Actions
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Save"))
            {
                SaveEvent();
            }

            if (!_isCreatingNew && GUILayout.Button("Delete"))
            {
                if (EditorUtility.DisplayDialog("Delete Event", 
                    $"Are you sure you want to delete '{_title}'?", "Delete", "Cancel"))
                {
                    DeleteEvent();
                }
            }

            if (GUILayout.Button("Cancel"))
            {
                ClearForm();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDateTimePicker(ref DateTime dateTime)
        {
            var year = EditorGUILayout.IntField(dateTime.Year, GUILayout.Width(50));
            var month = EditorGUILayout.IntField(dateTime.Month, GUILayout.Width(30));
            var day = EditorGUILayout.IntField(dateTime.Day, GUILayout.Width(30));
            var hour = EditorGUILayout.IntField(dateTime.Hour, GUILayout.Width(30));
            var minute = EditorGUILayout.IntField(dateTime.Minute, GUILayout.Width(30));

            try
            {
                dateTime = new DateTime(year, month, day, hour, minute, 0);
            }
            catch
            {
                Debug.LogWarning("Invalid date/time entered");
            }
        }

        private void DrawStatusBar()
        {
            EditorGUILayout.Space();

            if (_isLoading)
            {
                EditorGUILayout.HelpBox("Loading...", MessageType.Info);
            }
            else if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.HelpBox(_statusMessage, _statusType);
            }
        }

        private void LoadAllEvents()
        {
            if (!_isInitialized)
                return;

            _isLoading = true;
            _statusMessage = "Loading events...";
            Repaint();

            _database.Child("events").GetValueAsync().ContinueWithOnMainThread(task =>
            {
                _isLoading = false;

                if (task.IsFaulted)
                {
                    _statusMessage = $"Failed to load events: {task.Exception?.Message}";
                    _statusType = MessageType.Error;
                }
                else if (task.Result.Exists)
                {
                    _events.Clear();
                    foreach (var child in task.Result.Children)
                    {
                        try
                        {
                            var json = child.GetRawJsonValue();
                            var evt = JsonUtility.FromJson<EventData>(json);
                            evt.Id = child.Key;
                            _events.Add(evt);
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning($"Failed to parse event {child.Key}: {e.Message}");
                        }
                    }
                    _statusMessage = $"Loaded {_events.Count} events";
                    _statusType = MessageType.Info;
                }
                else
                {
                    _events.Clear();
                    _statusMessage = "No events found";
                    _statusType = MessageType.Info;
                }

                Repaint();
            });
        }

        private void CreateNewEvent()
        {
            _selectedEvent = null;
            _isCreatingNew = true;
            ClearFormFields();
        }

        private void SelectEvent(EventData evt)
        {
            _selectedEvent = evt;
            _isCreatingNew = false;

            _title = evt.Title ?? "";
            _description = evt.Description ?? "";
            _type = evt.Type ?? EventType.Quest;
            _latitude = evt.Latitude;
            _longitude = evt.Longitude;
            _startDate = evt.GetStartDateTime();
            _endDate = evt.GetEndDateTime();
            _isActive = evt.IsActive;
            _imageUrl = evt.ImageUrl ?? "";
            _externalLink = evt.ExternalLink ?? "";
            _radius = evt.Radius > 0 ? evt.Radius : 50;
        }

        private void SaveEvent()
        {
            if (string.IsNullOrWhiteSpace(_title))
            {
                _statusMessage = "Title is required";
                _statusType = MessageType.Warning;
                return;
            }

            _isLoading = true;
            _statusMessage = "Saving...";
            Repaint();

            var eventData = new EventData
            {
                Id = _isCreatingNew ? "" : _selectedEvent.Id,
                Title = _title,
                Description = _description,
                Type = _type,
                Latitude = _latitude,
                Longitude = _longitude,
                IsActive = _isActive,
                ImageUrl = _imageUrl,
                ExternalLink = _externalLink,
                Radius = _radius,
                CreatedAt = _isCreatingNew ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() : _selectedEvent.CreatedAt,
                UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            eventData.SetStartDateTime(_startDate);
            eventData.SetEndDateTime(_endDate);

            if (_isCreatingNew)
            {
                var newRef = _database.Child("events").Push();
                eventData.Id = newRef.Key;

                var json = JsonUtility.ToJson(eventData);
                newRef.SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
                {
                    HandleSaveResult(task, "Event created successfully");
                });
            }
            else
            {
                var json = JsonUtility.ToJson(eventData);
                _database.Child("events").Child(eventData.Id).SetRawJsonValueAsync(json)
                    .ContinueWithOnMainThread(task =>
                    {
                        HandleSaveResult(task, "Event updated successfully");
                    });
            }
        }

        private void HandleSaveResult(System.Threading.Tasks.Task task, string successMessage)
        {
            _isLoading = false;

            if (task.IsFaulted)
            {
                _statusMessage = $"Save failed: {task.Exception?.Message}";
                _statusType = MessageType.Error;
            }
            else
            {
                _statusMessage = successMessage;
                _statusType = MessageType.Info;
                ClearForm();
                LoadAllEvents();
            }

            Repaint();
        }

        private void DeleteEvent()
        {
            if (_selectedEvent == null)
                return;

            _isLoading = true;
            _statusMessage = "Deleting...";
            Repaint();

            _database.Child("events").Child(_selectedEvent.Id).RemoveValueAsync()
                .ContinueWithOnMainThread(task =>
                {
                    _isLoading = false;

                    if (task.IsFaulted)
                    {
                        _statusMessage = $"Delete failed: {task.Exception?.Message}";
                        _statusType = MessageType.Error;
                    }
                    else
                    {
                        _statusMessage = "Event deleted";
                        _statusType = MessageType.Info;
                        ClearForm();
                        LoadAllEvents();
                    }

                    Repaint();
                });
        }

        private void ClearForm()
        {
            _selectedEvent = null;
            _isCreatingNew = false;
            ClearFormFields();
        }

        private void ClearFormFields()
        {
            _title = "";
            _description = "";
            _type = EventType.Quest;
            _latitude = 0;
            _longitude = 0;
            _startDate = DateTime.Now;
            _endDate = DateTime.Now.AddHours(1);
            _isActive = true;
            _imageUrl = "";
            _externalLink = "";
            _radius = 50;
        }
    }
}
