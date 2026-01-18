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
                UpdateAvatarPosition(kvp.Value);
            }
        }

        private void HandlePlayerAppeared(string playerId, PlayerLocationData playerData)
        {
            if (_playerAvatars.ContainsKey(playerId))
                return;

            var container = new GameObject($"Player_{playerId}");
            container.transform.SetParent(transform);

            var avatar = new NearbyPlayerAvatar
            {
                PlayerId = playerId,
                PlayerData = playerData,
                Container = container,
                AvatarInstance = null,
                IsLoading = true
            };

            _playerAvatars[playerId] = avatar;

            UpdateAvatarPosition(avatar);
            LoadPlayerAvatar(playerData.AvatarId, playerData.AvatarGender, container.transform);
        }

        private void HandlePlayerUpdated(string playerId, PlayerLocationData playerData)
        {
            if (!_playerAvatars.TryGetValue(playerId, out var avatar))
                return;

            avatar.PlayerData = playerData;

            // Check if avatar changed
            if (avatar.AvatarInstance != null && 
                avatar.PlayerData.AvatarId != playerData.AvatarId)
            {
                Destroy(avatar.AvatarInstance);
                avatar.AvatarInstance = null;
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

                        avatarInstance.transform.localPosition = Vector3.zero;
                        avatarInstance.transform.localScale = Vector3.one * avatarScale;
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

        private void UpdateAvatarPosition(NearbyPlayerAvatar avatar)
        {
            if (avatar.Container == null || map == null)
                return;

            if (avatar.PlayerData.Latitude == 0 && avatar.PlayerData.Longitude == 0)
            {
                Debug.LogWarning($"[NearbyPlayersController] Invalid coordinates for {avatar.PlayerId}: (0, 0)");
                return;
            }

            var latLon = new Mapbox.Utils.Vector2d(
                avatar.PlayerData.Latitude,
                avatar.PlayerData.Longitude
            );

            var worldPos = map.GeoToWorldPosition(latLon, true);
            worldPos.y += avatarHeightOffset;

            avatar.Container.transform.position = worldPos;
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
        }
    }
}
