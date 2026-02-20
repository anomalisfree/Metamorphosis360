using System;
using System.Collections.Generic;
using Main.Domain;
using Main.Infrastructure;
using UnityEngine;

namespace Main.Services
{
    public sealed class AvatarService : MonoBehaviour
    {
        public static AvatarService Instance { get; private set; }

        [Header("Dependencies")]
        [SerializeField] private AvatarCatalog avatarCatalog;
        
        [Header("Settings")]
        [SerializeField] private bool cacheInstances = true;
        public event Action<string, GameObject> OnAvatarLoaded;
        public event Action<string, string> OnAvatarLoadFailed;

        private readonly Dictionary<string, GameObject> _instanceCache = new();
        public AvatarCatalog Catalog => avatarCatalog;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            
            // Загружаем каталог из Resources если не назначен
            if (avatarCatalog == null)
            {
                avatarCatalog = Resources.Load<AvatarCatalog>("AvatarCatalog");
            }
        }

        public GameObject LoadCurrentUserAvatar(Transform parent = null)
        {
            var avatarId = UserDataRepository.GetAvatarId();
            
            if (string.IsNullOrEmpty(avatarId))
            {
                Debug.LogWarning("[AvatarService] No avatar selected for current user");
                avatarId = avatarCatalog?.DefaultAvatarId;
            }

            return LoadAvatar(avatarId, parent);
        }
        public GameObject LoadAvatar(string avatarId, Transform parent = null)
        {
            if (avatarCatalog == null)
            {
                var error = "AvatarCatalog is not assigned";
                Debug.LogError($"[AvatarService] {error}");
                OnAvatarLoadFailed?.Invoke(avatarId, error);
                return null;
            }

            if (string.IsNullOrEmpty(avatarId))
            {
                avatarId = avatarCatalog.DefaultAvatarId;
            }

            if (cacheInstances && _instanceCache.TryGetValue(avatarId, out var cached))
            {
                if (cached != null)
                {
                    var instance = Instantiate(cached, parent);
                    instance.SetActive(true);
                    
                    // Применяем AnimatorController из каталога
                    var cachedEntry = avatarCatalog.GetById(avatarId);
                    if (cachedEntry?.AnimatorController != null)
                    {
                        var animator = instance.GetComponentInChildren<Animator>();
                        if (animator != null)
                        {
                            animator.runtimeAnimatorController = cachedEntry.AnimatorController;
                        }
                    }
                    
                    OnAvatarLoaded?.Invoke(avatarId, instance);
                    return instance;
                }
                _instanceCache.Remove(avatarId);
            }

            var entry = avatarCatalog.GetById(avatarId);
            if (entry == null)
            {
                var error = $"Avatar not found in catalog: {avatarId}";
                Debug.LogError($"[AvatarService] {error}");
                OnAvatarLoadFailed?.Invoke(avatarId, error);
                return null;
            }

            if (entry.Prefab == null)
            {
                var error = $"Avatar prefab is null: {avatarId}";
                Debug.LogError($"[AvatarService] {error}");
                OnAvatarLoadFailed?.Invoke(avatarId, error);
                return null;
            }

            var avatar = Instantiate(entry.Prefab, parent);
            avatar.name = $"Avatar_{entry.Id}";

            if (entry.AnimatorController != null)
            {
                var animator = avatar.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    animator.runtimeAnimatorController = entry.AnimatorController;
                }
            }

            if (cacheInstances)
            {
                _instanceCache[avatarId] = entry.Prefab;
            }

            Debug.Log($"[AvatarService] Avatar loaded: {avatarId}");
            OnAvatarLoaded?.Invoke(avatarId, avatar);
            
            return avatar;
        }

        public void LoadAvatarAsync(string avatarId, Transform parent, 
            Action<GameObject> onSuccess, Action<string> onError)
        {
            var avatar = LoadAvatar(avatarId, parent);
            
            if (avatar != null)
            {
                onSuccess?.Invoke(avatar);
            }
            else
            {
                onError?.Invoke($"Failed to load avatar: {avatarId}");
            }
        }
        public AvatarEntry GetAvatarInfo(string avatarId)
        {
            return avatarCatalog?.GetById(avatarId);
        }

        public IReadOnlyList<AvatarEntry> GetAllAvatars()
        {
            return avatarCatalog?.Avatars ?? new List<AvatarEntry>();
        }

        public List<AvatarEntry> GetAvatarsByCategory(AvatarCategory category)
        {
            return avatarCatalog?.GetByCategory(category) ?? new List<AvatarEntry>();
        }
        public void ClearCache()
        {
            _instanceCache.Clear();
            Debug.Log("[AvatarService] Cache cleared");
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            ClearCache();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (avatarCatalog == null)
            {
                Debug.LogWarning("[AvatarService] AvatarCatalog is not assigned!");
            }
        }
#endif
    }
}
