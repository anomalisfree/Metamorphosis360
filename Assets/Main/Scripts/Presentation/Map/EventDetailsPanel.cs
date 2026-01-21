using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Main.Domain;
using DomainEventType = Main.Domain.EventType;

namespace Main.Presentation.Map
{
    public class EventDetailsPanel : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject panelContainer;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI typeText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Image typeIcon;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button actionButton;
        [SerializeField] private TextMeshProUGUI actionButtonText;

        [Header("Type Colors")]
        [SerializeField] private Color questColor = new(0.2f, 0.6f, 1f);
        [SerializeField] private Color battleColor = new(1f, 0.2f, 0.2f);
        [SerializeField] private Color socialColor = new(0.2f, 1f, 0.4f);
        [SerializeField] private Color treasureColor = new(1f, 0.8f, 0.2f);
        [SerializeField] private Color bossColor = new(0.6f, 0.1f, 0.8f);
        [SerializeField] private Color specialColor = new(1f, 0.5f, 0f);

        public event Action<EventData> OnActionClicked;

        private EventData _currentEvent;

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }

            if (actionButton != null)
            {
                actionButton.onClick.AddListener(HandleActionClick);
            }

            Hide();
        }

        private void OnDestroy()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
            }

            if (actionButton != null)
            {
                actionButton.onClick.RemoveListener(HandleActionClick);
            }
        }

        public void Show(EventData eventData)
        {
            if (eventData == null)
                return;

            _currentEvent = eventData;

            if (titleText != null)
            {
                titleText.text = eventData.Title;
            }

            if (descriptionText != null)
            {
                descriptionText.text = eventData.Description;
            }

            if (typeText != null)
            {
                typeText.text = GetTypeDisplayName(eventData.Type);
                typeText.color = GetColorForType(eventData.Type);
            }

            if (typeIcon != null)
            {
                typeIcon.color = GetColorForType(eventData.Type);
            }

            if (timeText != null)
            {
                timeText.text = FormatEventTime(eventData);
            }

            if (statusText != null)
            {
                UpdateStatusText(eventData);
            }

            if (actionButtonText != null)
            {
                actionButtonText.text = GetActionButtonText(eventData);
            }

            if (panelContainer != null)
            {
                panelContainer.SetActive(true);
            }
            else
            {
                gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
            _currentEvent = null;

            if (panelContainer != null)
            {
                panelContainer.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public bool IsVisible()
        {
            return panelContainer != null ? panelContainer.activeSelf : gameObject.activeSelf;
        }

        private void HandleActionClick()
        {
            if (_currentEvent != null)
            {
                OnActionClicked?.Invoke(_currentEvent);
            }
        }

        private void UpdateStatusText(EventData eventData)
        {
            if (eventData.IsCurrentlyActive())
            {
                statusText.text = "● ACTIVE";
                statusText.color = Color.green;
            }
            else if (eventData.IsUpcoming())
            {
                var startTime = eventData.GetStartDateTime();
                var timeUntil = startTime - DateTime.UtcNow;

                if (timeUntil.TotalHours < 1)
                {
                    statusText.text = $"Starts in {(int)timeUntil.TotalMinutes} min";
                }
                else if (timeUntil.TotalDays < 1)
                {
                    statusText.text = $"Starts in {(int)timeUntil.TotalHours} hours";
                }
                else
                {
                    statusText.text = $"Starts in {(int)timeUntil.TotalDays} days";
                }
                statusText.color = Color.yellow;
            }
            else
            {
                statusText.text = "ENDED";
                statusText.color = Color.gray;
            }
        }

        private string FormatEventTime(EventData eventData)
        {
            var start = eventData.GetStartDateTime().ToLocalTime();
            var end = eventData.GetEndDateTime().ToLocalTime();

            if (start.Date == end.Date)
            {
                return $"{start:MMM dd, yyyy}\n{start:HH:mm} - {end:HH:mm}";
            }
            else
            {
                return $"{start:MMM dd, HH:mm} -\n{end:MMM dd, HH:mm}";
            }
        }

        private string GetTypeDisplayName(string type)
        {
            return type switch
            {
                DomainEventType.Quest => "Quest",
                DomainEventType.Battle => "Battle",
                DomainEventType.Social => "Social Event",
                DomainEventType.Treasure => "Treasure Hunt",
                DomainEventType.Boss => "Boss Fight",
                DomainEventType.Special => "Special Event",
                _ => type
            };
        }

        private string GetActionButtonText(EventData eventData)
        {
            if (!eventData.IsCurrentlyActive())
            {
                return "View Details";
            }

            return eventData.Type switch
            {
                DomainEventType.Quest => "Start Quest",
                DomainEventType.Battle => "Join Battle",
                DomainEventType.Social => "Join Event",
                DomainEventType.Treasure => "Hunt Treasure",
                DomainEventType.Boss => "Fight Boss",
                DomainEventType.Special => "Participate",
                _ => "Go"
            };
        }

        private Color GetColorForType(string eventType)
        {
            return eventType switch
            {
                DomainEventType.Quest => questColor,
                DomainEventType.Battle => battleColor,
                DomainEventType.Social => socialColor,
                DomainEventType.Treasure => treasureColor,
                DomainEventType.Boss => bossColor,
                DomainEventType.Special => specialColor,
                _ => Color.white
            };
        }
    }
}
