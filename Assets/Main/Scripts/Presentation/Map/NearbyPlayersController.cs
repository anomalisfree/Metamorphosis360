using System.Collections.Generic;
using Main.Domain;
using Main.Services;
using UnityEngine;
using Mapbox.Unity.Map;
using ReadyPlayerMe.Core;

namespace Main.Presentation.Map
{
    public sealed class NearbyPlayersController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private NearbyPlayersService nearbyPlayersService;
        [SerializeField] private AvatarLoaderService avatarLoaderService;
        [SerializeField] private AbstractMap map;

        [Header("Settings")]
        [SerializeField] private float avatarScale = 1f;
        [SerializeField] private float avatarHeightOffset = 0f;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float arrivalThreshold = 0.1f;

        [Header("Animation")]
        [SerializeField] private string walkAnimParam = "Walk";

        private readonly Dictionary<string, NearbyPlayerAvatar> _playerAvatars = new();

        private void OnEnable()
        {
            if (nearbyPlayersService != null)
            {
                nearbyPlayersService.OnPlayerAppeared += HandlePlayerAppeared;
                nearbyPlayersService.OnPlayerUpdated += HandlePlayerUpdated;
                nearbyPlayersService.OnPlayerDisappeared += HandlePlayerDisappeared;
            }

            if (avatarLoaderService != null)
            {
                avatarLoaderService.OnAvatarLoaded += HandleAvatarLoaded;
            }
        }

        private void OnDisable()
        {
            if (nearbyPlayersService != null)
            {
                nearbyPlayersService.OnPlayerAppeared -= HandlePlayerAppeared;
                nearbyPlayersService.OnPlayerUpdated -= HandlePlayerUpdated;
                nearbyPlayersService.OnPlayerDisappeared -= HandlePlayerDisappeared;
            }

            if (avatarLoaderService != null)
            {
                avatarLoaderService.OnAvatarLoaded -= HandleAvatarLoaded;
            }

            ClearAllAvatars();
        }

        private void LateUpdate()
        {
            if (map == null)
                return;

            foreach (var kvp in _playerAvatars)
            {
                UpdateAvatarMovement(kvp.Value);
            }
        }

        private void HandlePlayerAppeared(string playerId, PlayerLocationData playerData)
        {
            if (_playerAvatars.ContainsKey(playerId))
                return;

            var container = new GameObject($"Player_{playerId}");
            container.transform.SetParent(transform);

            var targetPos = GetWorldPosition(playerData);

            var avatar = new NearbyPlayerAvatar
            {
                PlayerId = playerId,
                PlayerData = playerData,
                Container = container,
                AvatarInstance = null,
                IsLoading = true,
                TargetPosition = targetPos,
                IsMoving = false,
                Animator = null
            };

            container.transform.position = targetPos;

            _playerAvatars[playerId] = avatar;

            LoadPlayerAvatar(playerData.AvatarId, playerData.AvatarGender, container.transform);
        }

        private void HandlePlayerUpdated(string playerId, PlayerLocationData playerData)
        {
            if (!_playerAvatars.TryGetValue(playerId, out var avatar))
                return;

            avatar.PlayerData = playerData;
            avatar.TargetPosition = GetWorldPosition(playerData);

            if (avatar.AvatarInstance != null && 
                avatar.PlayerData.AvatarId != playerData.AvatarId)
            {
                Destroy(avatar.AvatarInstance);
                avatar.AvatarInstance = null;
                avatar.Animator = null;
                avatar.IsLoading = true;
                LoadPlayerAvatar(playerData.AvatarId, playerData.AvatarGender, avatar.Container.transform);
            }
        }

        private void HandlePlayerDisappeared(string playerId)
        {
            if (!_playerAvatars.TryGetValue(playerId, out var avatar))
                return;

            if (avatar.Container != null)
            {
                Destroy(avatar.Container);
            }

            _playerAvatars.Remove(playerId);
        }

        private void HandleAvatarLoaded(string avatarId, GameObject avatarInstance)
        {
            foreach (var kvp in _playerAvatars)
            {
                var avatar = kvp.Value;
                
                if (avatar.PlayerData.AvatarId == avatarId && avatar.IsLoading)
                {
                    if (avatarInstance.transform.parent == avatar.Container.transform)
                    {
                        avatar.AvatarInstance = avatarInstance;
                        avatar.IsLoading = false;
                        avatar.Animator = avatarInstance.GetComponentInChildren<Animator>();

                        avatarInstance.transform.localPosition = Vector3.zero;
                        avatarInstance.transform.localScale = Vector3.one * avatarScale;

                        SetIdleAnimation(avatar);
                    }
                }
            }
        }

        private void LoadPlayerAvatar(string avatarId, string genderString, Transform parent)
        {
            if (avatarLoaderService == null || string.IsNullOrEmpty(avatarId))
                return;

            var gender = OutfitGender.Neutral;
            if (!string.IsNullOrEmpty(genderString))
            {
                System.Enum.TryParse(genderString, out gender);
            }

            avatarLoaderService.LoadAvatar(avatarId, gender, parent);
        }

        private Vector3 GetWorldPosition(PlayerLocationData playerData)
        {
            if (playerData.Latitude == 0 && playerData.Longitude == 0)
                return Vector3.zero;

            var latLon = new Mapbox.Utils.Vector2d(
                playerData.Latitude,
                playerData.Longitude
            );

            var worldPos = map.GeoToWorldPosition(latLon, true);
            worldPos.y += avatarHeightOffset;

            return worldPos;
        }

        private void UpdateAvatarMovement(NearbyPlayerAvatar avatar)
        {
            if (avatar.Container == null || map == null)
                return;

            avatar.TargetPosition = GetWorldPosition(avatar.PlayerData);

            var currentPos = avatar.Container.transform.position;
            var targetPos = avatar.TargetPosition;
            var distance = Vector3.Distance(currentPos, targetPos);

            if (distance > arrivalThreshold)
            {
                var direction = (targetPos - currentPos).normalized;
                var newPos = Vector3.MoveTowards(currentPos, targetPos, moveSpeed * Time.deltaTime);
                avatar.Container.transform.position = newPos;

                if (avatar.AvatarInstance != null && direction.sqrMagnitude > 0.001f)
                {
                    direction.y = 0;
                    if (direction.sqrMagnitude > 0.001f)
                    {
                        var targetRotation = Quaternion.LookRotation(direction);
                        avatar.AvatarInstance.transform.rotation = Quaternion.Slerp(
                            avatar.AvatarInstance.transform.rotation,
                            targetRotation,
                            Time.deltaTime * rotationSpeed
                        );
                    }
                }

                if (!avatar.IsMoving)
                {
                    avatar.IsMoving = true;
                    SetWalkAnimation(avatar);
                }
            }
            else
            {
                avatar.Container.transform.position = targetPos;

                if (avatar.IsMoving)
                {
                    avatar.IsMoving = false;
                    SetIdleAnimation(avatar);
                }
            }
        }

        private void SetWalkAnimation(NearbyPlayerAvatar avatar)
        {
            if (avatar.Animator == null)
                return;

            TrySetAnimatorBool(avatar.Animator, walkAnimParam, true);
        }

        private void SetIdleAnimation(NearbyPlayerAvatar avatar)
        {
            if (avatar.Animator == null)
                return;

            TrySetAnimatorBool(avatar.Animator, walkAnimParam, false);
        }

        private void TrySetAnimatorBool(Animator animator, string paramName, bool value)
        {
            foreach (var param in animator.parameters)
            {
                if (param.name == paramName && param.type == AnimatorControllerParameterType.Bool)
                {
                    animator.SetBool(paramName, value);
                    return;
                }
            }
        }

        private void ClearAllAvatars()
        {
            foreach (var kvp in _playerAvatars)
            {
                if (kvp.Value.Container != null)
                {
                    Destroy(kvp.Value.Container);
                }
            }

            _playerAvatars.Clear();
        }

        private class NearbyPlayerAvatar
        {
            public string PlayerId;
            public PlayerLocationData PlayerData;
            public GameObject Container;
            public GameObject AvatarInstance;
            public bool IsLoading;
            public Vector3 TargetPosition;
            public bool IsMoving;
            public Animator Animator;
        }
    }
}
