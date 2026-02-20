using System;
using Main.Core;
using Main.Domain;
using Main.Infrastructure;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DomainEventType = Main.Domain.EventType;

namespace Main.Presentation.AR
{
    public sealed class ARGameController : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI eventTitleText;
        [SerializeField] private TextMeshProUGUI eventTypeText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Button exitButton;
        [SerializeField] private Button completeButton;
        
        [Header("Game Settings")]
        [SerializeField] private float gameDuration = 60f;
        
        [Header("Result Panel")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultTitleText;
        [SerializeField] private TextMeshProUGUI resultScoreText;
        [SerializeField] private TextMeshProUGUI resultTimeText;
        [SerializeField] private Button returnToMapButton;

        public event Action<ARGameResult> OnGameCompleted;

        private EventData _currentEvent;
        private float _timeElapsed;
        private int _score;
        private bool _isGameActive;
        private bool _isGameCompleted;

        private void Start()
        {
            SetupUI();
            InitializeFromSession();
        }

        private void Update()
        {
            if (_isGameActive && !_isGameCompleted)
            {
                UpdateTimer();
            }
        }

        private void OnDestroy()
        {
            CleanupListeners();
        }

        #region Initialization

        private void SetupUI()
        {
            exitButton?.onClick.AddListener(OnExitClicked);
            completeButton?.onClick.AddListener(OnCompleteClicked);
            returnToMapButton?.onClick.AddListener(ReturnToMap);
            
            resultPanel?.SetActive(false);
        }

        private void CleanupListeners()
        {
            exitButton?.onClick.RemoveListener(OnExitClicked);
            completeButton?.onClick.RemoveListener(OnCompleteClicked);
            returnToMapButton?.onClick.RemoveListener(ReturnToMap);
        }

        private void InitializeFromSession()
        {
            if (!ARSessionData.HasActiveSession)
            {
                Debug.LogError("[ARGameController] No active AR session! Returning to map.");
                ReturnToMap();
                return;
            }

            _currentEvent = ARSessionData.CurrentEvent;
            
            Debug.Log($"[ARGameController] Starting AR game for event: {_currentEvent.Title}");
            
            UpdateEventInfo();
            StartGame();
        }

        private void UpdateEventInfo()
        {
            if (eventTitleText != null)
            {
                eventTitleText.text = _currentEvent.Title;
            }

            if (eventTypeText != null)
            {
                eventTypeText.text = GetEventTypeDisplayName(_currentEvent.Type);
            }
        }

        #endregion

        #region Game Logic

        private void StartGame()
        {
            _timeElapsed = 0f;
            _score = 0;
            _isGameActive = true;
            _isGameCompleted = false;
            
            UpdateScoreUI();
            
            Debug.Log("[ARGameController] Game started!");
        }

        private void UpdateTimer()
        {
            _timeElapsed += Time.deltaTime;
            
            if (timerText != null)
            {
                var remaining = Mathf.Max(0, gameDuration - _timeElapsed);
                var minutes = Mathf.FloorToInt(remaining / 60);
                var seconds = Mathf.FloorToInt(remaining % 60);
                timerText.text = $"{minutes:00}:{seconds:00}";
            }

            if (_timeElapsed >= gameDuration)
            {
                CompleteGame(false);
            }
        }

        public void AddScore(int points)
        {
            if (!_isGameActive || _isGameCompleted)
                return;

            _score += points;
            UpdateScoreUI();
            
            Debug.Log($"[ARGameController] Score: {_score} (+{points})");
        }

        private void UpdateScoreUI()
        {
            if (scoreText != null)
            {
                scoreText.text = $"Score: {_score}";
            }
        }

        private void CompleteGame(bool success)
        {
            if (_isGameCompleted)
                return;

            _isGameActive = false;
            _isGameCompleted = true;

            var result = new ARGameResult(
                isSuccess: success,
                score: _score,
                timeElapsed: _timeElapsed,
                eventId: _currentEvent.Id
            );

            ARSessionData.EndSession(result);
            
            Debug.Log($"[ARGameController] Game completed! Success: {success}, Score: {_score}");
            
            OnGameCompleted?.Invoke(result);
            ShowResultPanel(result);
        }

        #endregion

        #region UI Handlers

        private void OnExitClicked()
        {
            Debug.Log("[ARGameController] Exit clicked");
            CompleteGame(false);
        }

        private void OnCompleteClicked()
        {
            Debug.Log("[ARGameController] Complete clicked");
            CompleteGame(true);
        }

        private void ShowResultPanel(ARGameResult result)
        {
            if (resultPanel == null)
            {
                ReturnToMap();
                return;
            }

            resultPanel.SetActive(true);

            if (resultTitleText != null)
            {
                resultTitleText.text = result.IsSuccess ? "Success!" : "Game Over";
            }

            if (resultScoreText != null)
            {
                resultScoreText.text = $"Score: {result.Score}";
            }

            if (resultTimeText != null)
            {
                var minutes = Mathf.FloorToInt(result.TimeElapsed / 60);
                var seconds = Mathf.FloorToInt(result.TimeElapsed % 60);
                resultTimeText.text = $"Time: {minutes:00}:{seconds:00}";
            }
        }

        private void ReturnToMap()
        {
            Debug.Log("[ARGameController] Returning to Map");
            
            if (GameBootstrap.Instance?.StateMachine == null)
            {
                Debug.LogError("[ARGameController] GameBootstrap or StateMachine is null!");
                return;
            }

            GameBootstrap.Instance.StateMachine.SetState(AppState.Map);
        }

        #endregion

        #region Helpers

        private string GetEventTypeDisplayName(string type)
        {
            return type switch
            {
                DomainEventType.Quest => "Quest",
                DomainEventType.Battle => "Battle",
                DomainEventType.Social => "Social",
                DomainEventType.Treasure => "Treasure",
                DomainEventType.Boss => "Boss",
                DomainEventType.Special => "Special",
                _ => type
            };
        }

        #endregion
    }
}
