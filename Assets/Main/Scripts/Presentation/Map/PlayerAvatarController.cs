using UnityEngine;
using Main.Services;
using Main.Infrastructure;

namespace Main.Presentation.Map
{
    public sealed class PlayerAvatarController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AvatarLoaderService avatarLoaderService;
        [SerializeField] private Services.LocationService locationService;

        [Header("Avatar Settings")]
        [SerializeField] private float avatarScale = 0.5f;
        [SerializeField] private float avatarYOffset = 0f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float walkThreshold = 0.00001f;

        [Header("Animation")]
        [SerializeField] private string walkAnimParam = "Walk";

        private GameObject _avatarInstance;
        private Animator _animator;
        private Vector2d _lastLocation;
        private bool _isMoving;
        private Vector3 _targetPosition;
        private bool _hasTargetPosition;
        private Vector3 _lastMoveDirection;
        private bool _hasMoveDirection;
        private float _lastMovementTime;
        private const float IdleTimeoutSeconds = 0.5f;

        public GameObject AvatarInstance => _avatarInstance;

        public bool IsReady => _avatarInstance != null && _avatarInstance.activeInHierarchy;

        private void OnEnable()
        {
            if (avatarLoaderService != null)
            {
                avatarLoaderService.OnAvatarLoaded += HandleAvatarLoaded;
                avatarLoaderService.OnAvatarLoadFailed += HandleAvatarLoadFailed;
            }

            if (locationService != null)
            {
                locationService.OnLocationUpdated += HandleLocationUpdated;
            }
        }

        private void OnDisable()
        {
            if (avatarLoaderService != null)
            {
                avatarLoaderService.OnAvatarLoaded -= HandleAvatarLoaded;
                avatarLoaderService.OnAvatarLoadFailed -= HandleAvatarLoadFailed;
            }

            if (locationService != null)
            {
                locationService.OnLocationUpdated -= HandleLocationUpdated;
            }
        }

        private void Start()
        {
            if (AvatarDataRepository.HasSavedAvatar)
            {
                LoadAvatar();
            }
            else
            {
                Debug.LogWarning("[PlayerAvatarController] No saved avatar found");
            }
        }

        public void LoadAvatar()
        {
            if (avatarLoaderService == null)
            {
                Debug.LogError("[PlayerAvatarController] AvatarLoaderService not assigned");
                return;
            }

            Debug.Log("[PlayerAvatarController] Loading player avatar...");
            avatarLoaderService.LoadCurrentUserAvatar(transform);
        }

        public void ReloadAvatar()
        {
            if (_avatarInstance != null)
            {
                Destroy(_avatarInstance);
                _avatarInstance = null;
                _animator = null;
            }

            LoadAvatar();
        }

        public void UpdateWorldPosition(Vector3 worldPosition)
        {
            var newTarget = worldPosition + Vector3.up * avatarYOffset;
            
            if (_avatarInstance != null)
            {
                // Аватар всегда телепортируется на целевую позицию (центр карты)
                _avatarInstance.transform.position = newTarget;
            }
            
            _targetPosition = newTarget;
            _hasTargetPosition = true;
        }

        private void Update()
        {
            if (_avatarInstance == null)
                return;

            if (_hasMoveDirection && _lastMoveDirection.sqrMagnitude > 0.0001f)
            {
                var targetRotation = Quaternion.LookRotation(_lastMoveDirection);
                _avatarInstance.transform.rotation = Quaternion.Slerp(
                    _avatarInstance.transform.rotation,
                    targetRotation,
                    Time.deltaTime * rotationSpeed
                );
            }

            if (_isMoving && Time.time - _lastMovementTime > IdleTimeoutSeconds)
            {
                _isMoving = false;
                SetIdleAnimation();
                Debug.Log("[PlayerAvatarController] Stopped walking, switching to Idle");
            }
        }

        private void HandleLocationUpdated(Vector2d newLocation)
        {
            if (_lastLocation.x != 0 || _lastLocation.y != 0)
            {
                var deltaLat = newLocation.x - _lastLocation.x;
                var deltaLon = newLocation.y - _lastLocation.y;
                
                var distanceMeters = System.Math.Sqrt(deltaLat * deltaLat + deltaLon * deltaLon) * 111000;
                
                if (distanceMeters > 2.0)
                {
                    var direction = new Vector3((float)deltaLon, 0f, (float)deltaLat);
                    _lastMoveDirection = direction.normalized;
                    _hasMoveDirection = true;
                    _lastMovementTime = Time.time;
                    
                    if (!_isMoving)
                    {
                        _isMoving = true;
                        SetWalkAnimation();
                        Debug.Log("[PlayerAvatarController] Started walking");
                    }
                }
            }

            _lastLocation = newLocation;
        }

        private void HandleAvatarLoaded(string avatarId, GameObject avatar)
        {
            Debug.Log($"[PlayerAvatarController] Avatar loaded: {avatarId}");

            _avatarInstance = avatar;
            _avatarInstance.transform.SetParent(transform);
            _avatarInstance.transform.localPosition = Vector3.up * avatarYOffset;
            _avatarInstance.transform.localScale = Vector3.one * avatarScale;

            _animator = _avatarInstance.GetComponentInChildren<Animator>();
            
            if (_animator != null)
            {
                SetIdleAnimation();
            }
        }

        private void HandleAvatarLoadFailed(string avatarId, string error)
        {
            Debug.LogError($"[PlayerAvatarController] Failed to load avatar: {error}");
        }

        private void SetIdleAnimation()
        {
            if (_animator == null) return;

            TrySetAnimatorBool(walkAnimParam, false);
        }

        private void SetWalkAnimation()
        {
            if (_animator == null) return;

            TrySetAnimatorBool(walkAnimParam, true);
        }

        private void TrySetAnimatorBool(string paramName, bool value)
        {
            foreach (var param in _animator.parameters)
            {
                if (param.name == paramName && param.type == AnimatorControllerParameterType.Bool)
                {
                    _animator.SetBool(paramName, value);
                    return;
                }
            }
        }

        private static double CalculateDistance(Vector2d a, Vector2d b)
        {
            var dx = a.x - b.x;
            var dy = a.y - b.y;
            return System.Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
