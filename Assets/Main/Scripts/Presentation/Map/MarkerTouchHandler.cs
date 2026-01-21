using UnityEngine;
using UnityEngine.EventSystems;

namespace Main.Presentation.Map
{
    public class MarkerTouchHandler : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private Camera raycastCamera;
        [SerializeField] private EventDetailsPanel eventDetailsPanel;

        [Header("Settings")]
        [SerializeField] private float maxRaycastDistance = 1000f;
        [SerializeField] private LayerMask markerLayerMask = ~0; // All layers by default

        private void Awake()
        {
            if (raycastCamera == null)
            {
                raycastCamera = Camera.main;
            }
        }

        private void Update()
        {
            // Handle touch input (mobile)
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    if (!IsPointerOverUI())
                    {
                        TrySelectMarker(touch.position);
                    }
                }
            }
            // Handle mouse input (desktop/editor)
            else if (Input.GetMouseButtonDown(0))
            {
                if (!IsPointerOverUI())
                {
                    TrySelectMarker(Input.mousePosition);
                }
            }
        }

        private void TrySelectMarker(Vector2 screenPosition)
        {
            if (raycastCamera == null)
            {
                Debug.LogWarning("[MarkerTouchHandler] No camera assigned for raycasting");
                return;
            }

            var ray = raycastCamera.ScreenPointToRay(screenPosition);

            if (Physics.Raycast(ray, out var hit, maxRaycastDistance, markerLayerMask))
            {
                var clickHandler = hit.collider.GetComponent<EventMarkerClickHandler>();
                
                if (clickHandler == null)
                {
                    clickHandler = hit.collider.GetComponentInParent<EventMarkerClickHandler>();
                }

                if (clickHandler != null)
                {
                    HandleMarkerClick(clickHandler);
                }
            }
        }

        private void HandleMarkerClick(EventMarkerClickHandler clickHandler)
        {
            // Invoke the handler's click action
            clickHandler.HandleClick();

            // Show event details panel if available
            if (eventDetailsPanel != null && clickHandler.EventData != null)
            {
                eventDetailsPanel.Show(clickHandler.EventData);
            }
        }

        private bool IsPointerOverUI()
        {
            // Check if touching/clicking on UI elements
            if (EventSystem.current == null)
                return false;

            // For touch
            if (Input.touchCount > 0)
            {
                return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            }

            // For mouse
            return EventSystem.current.IsPointerOverGameObject();
        }

        // Set the camera used for raycasting (useful for AR where camera may change).
        public void SetCamera(Camera camera)
        {
            raycastCamera = camera;
        }

        // Set the event details panel reference.
        public void SetEventDetailsPanel(EventDetailsPanel panel)
        {
            eventDetailsPanel = panel;
        }
    }
}
