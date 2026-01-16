using System;
using System.Collections.Generic;
using UnityEngine;
using ReadyPlayerMe.Core;
using Main.Infrastructure;
using ReadyPlayerMe.AvatarCreator;

namespace Main.Services
{
    public sealed class AvatarLoaderService : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private AvatarConfig avatarConfig;
        [SerializeField] private RuntimeAnimatorController masculineAnimatorController;
        [SerializeField] private RuntimeAnimatorController feminineAnimatorController;
        [SerializeField] private bool cacheLoadedAvatars = true;

        public event Action<string, GameObject> OnAvatarLoaded;
        public event Action<string, string> OnAvatarLoadFailed;
        public event Action<string, float> OnLoadProgress;


        private readonly Dictionary<string, GameObject> _avatarCache = new();
        private readonly Dictionary<string, AvatarObjectLoader> _activeLoaders = new();

        public void LoadCurrentUserAvatar(Transform parent = null)
        {
            var avatarData = AvatarDataRepository.Load();
            if (avatarData == null || string.IsNullOrEmpty(avatarData.AvatarId))
            {
                Debug.LogWarning("[AvatarLoaderService] No saved avatar data found");
                OnAvatarLoadFailed?.Invoke("", "No saved avatar data");
                return;
            }

            LoadAvatar(avatarData.AvatarId, avatarData.AvatarOutfitGender, parent);
        }

        public void LoadAvatar(string avatarId, OutfitGender gender = OutfitGender.Neutral, Transform parent = null)
        {
            if (string.IsNullOrEmpty(avatarId))
            {
                OnAvatarLoadFailed?.Invoke(avatarId, "Avatar ID is empty");
                return;
            }

            if (cacheLoadedAvatars && _avatarCache.TryGetValue(avatarId, out var cachedAvatar))
            {
                if (cachedAvatar != null)
                {
                    Debug.Log($"[AvatarLoaderService] Using cached avatar: {avatarId}");
                    var instance = Instantiate(cachedAvatar, parent);
                    instance.SetActive(true);
                    OnAvatarLoaded?.Invoke(avatarId, instance);
                    return;
                }
                else
                {
                    _avatarCache.Remove(avatarId);
                }
            }

            if (_activeLoaders.ContainsKey(avatarId))
            {
                Debug.Log($"[AvatarLoaderService] Avatar {avatarId} is already loading");
                return;
            }

            var loader = new AvatarObjectLoader();
            
            if (avatarConfig != null)
            {
                loader.AvatarConfig = avatarConfig;
            }

            _activeLoaders[avatarId] = loader;

            var capturedGender = gender;
            loader.OnCompleted += (sender, args) =>
            {
                _activeLoaders.Remove(avatarId);
                HandleLoadCompleted(avatarId, args, parent, capturedGender);
            };

            loader.OnFailed += (sender, args) =>
            {
                _activeLoaders.Remove(avatarId);
                HandleLoadFailed(avatarId, args.Message);
            };

            loader.OnProgressChanged += (sender, args) =>
            {
                OnLoadProgress?.Invoke(avatarId, args.Progress);
            };

            var avatarUrl = $"{Env.RPM_MODELS_BASE_URL}/{avatarId}.glb";
            Debug.Log($"[AvatarLoaderService] Loading avatar from: {avatarUrl}");
            
            loader.LoadAvatar(avatarUrl);
        }

        public void CancelLoad(string avatarId)
        {
            if (_activeLoaders.TryGetValue(avatarId, out var loader))
            {
                loader.Cancel();
                _activeLoaders.Remove(avatarId);
                Debug.Log($"[AvatarLoaderService] Cancelled loading: {avatarId}");
            }
        }

        public void CancelAllLoads()
        {
            foreach (var loader in _activeLoaders.Values)
            {
                loader.Cancel();
            }
            _activeLoaders.Clear();
        }

        public void ClearCache()
        {
            foreach (var avatar in _avatarCache.Values)
            {
                if (avatar != null)
                {
                    Destroy(avatar);
                }
            }
            _avatarCache.Clear();
        }

        private void OnDestroy()
        {
            CancelAllLoads();
            ClearCache();
        }

        private void HandleLoadCompleted(string avatarId, CompletionEventArgs args, Transform parent, OutfitGender gender)
        {
            var avatar = args.Avatar;
            
            AvatarAnimationHelper.SetupAnimator(args.Metadata, avatar);

            var selectedController = gender == OutfitGender.Feminine 
                ? feminineAnimatorController 
                : masculineAnimatorController;

            if (selectedController != null)
            {
                var animator = avatar.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    animator.runtimeAnimatorController = selectedController;
                    Debug.Log($"[AvatarLoaderService] Applied {gender} AnimatorController to avatar: {avatarId}");
                }
            }

            if (cacheLoadedAvatars)
            {
                avatar.SetActive(false);
                avatar.transform.SetParent(transform); 
                _avatarCache[avatarId] = avatar;

                var instance = Instantiate(avatar, parent);
                instance.SetActive(true);
                
                Debug.Log($"[AvatarLoaderService] Avatar loaded and cached: {avatarId}");
                OnAvatarLoaded?.Invoke(avatarId, instance);
            }
            else
            {
                if (parent != null)
                {
                    avatar.transform.SetParent(parent);
                }
                avatar.SetActive(true);
                
                Debug.Log($"[AvatarLoaderService] Avatar loaded: {avatarId}");
                OnAvatarLoaded?.Invoke(avatarId, avatar);
            }
        }

        private void HandleLoadFailed(string avatarId, string message)
        {
            Debug.LogError($"[AvatarLoaderService] Failed to load avatar {avatarId}: {message}");
            OnAvatarLoadFailed?.Invoke(avatarId, message);
        }
    }
}
