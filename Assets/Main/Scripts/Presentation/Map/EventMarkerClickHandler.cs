using System;
using Main.Domain;
using UnityEngine;

namespace Main.Presentation.Map
{
    public class EventMarkerClickHandler : MonoBehaviour
    {
        public EventData EventData { get; private set; }

        public event Action<EventData> OnClicked;

        private Action _onClick;

        public void Initialize(EventData eventData, Action onClick)
        {
            EventData = eventData;
            _onClick = onClick;
            UpdateVisuals();
        }

        public void UpdateEventData(EventData eventData)
        {
            EventData = eventData;
            UpdateVisuals();
        }

        protected virtual void UpdateVisuals()
        {
            // Override in derived classes to update marker UI
        }

        public void HandleClick()
        {
            _onClick?.Invoke();
            OnClicked?.Invoke(EventData);
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        private void OnMouseDown()
        {
            HandleClick();
        }
#endif
    }
}
