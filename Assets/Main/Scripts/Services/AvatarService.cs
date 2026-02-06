using System;
using System.Collections.Generic;
using Main.Domain;
using Main.Infrastructure;
using UnityEngine;

namespace Main.Services
{
    /// <summary>
    /// Сервис для загрузки и управления аватарами из локального каталога.
    /// Заменяет Ready Player Me AvatarLoaderService.
    /// </summary>
    public sealed class AvatarService : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private AvatarCatalog avatarCatalog;
        
        [Header("Settings")]
        [SerializeField] private bool cacheInstances = true;

        /// <summary>
        /// Вызывается при успешной загрузке аватара.
        /// Параметры: avatarId, созданный GameObject.
        /// </summary>
        public event Action<string, GameObject> OnAvatarLoaded;
        
        /// <summary>
        /// Вызывается при ошибке загрузки.
        /// Параметры: avatarId, сообщение об ошибке.
        /// </summary>
        public event Action<string, string> OnAvatarLoadFailed;

        private readonly Dictionary<string, GameObject> _instanceCache = new();

        /// <summary>
        /// Каталог доступных аватаров.
        /// </summary>
        public AvatarCatalog Catalog => avatarCatalog;

        /// <summary>
        /// Загружает аватар текущего пользователя.
        /// </summary>
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

        /// <summary>
        /// Загружает аватар по ID.
        /// </summary>
        /// <param name="avatarId">ID аватара из каталога</param>
        /// <param name="parent">Родительский Transform (опционально)</param>
        /// <returns>Созданный экземпляр аватара или null при ошибке</returns>
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

            // Проверяем кэш
            if (cacheInstances && _instanceCache.TryGetValue(avatarId, out var cached))
            {
                if (cached != null)
                {
                    Debug.Log($"[AvatarService] Using cached avatar: {avatarId}");
                    var instance = Instantiate(cached, parent);
                    instance.SetActive(true);
                    OnAvatarLoaded?.Invoke(avatarId, instance);
                    return instance;
                }
                _instanceCache.Remove(avatarId);
            }

            // Получаем запись из каталога
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

            // Создаём экземпляр
            var avatar = Instantiate(entry.Prefab, parent);
            avatar.name = $"Avatar_{entry.Id}";

            // Настраиваем аниматор, если указан в каталоге
            if (entry.AnimatorController != null)
            {
                var animator = avatar.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    animator.runtimeAnimatorController = entry.AnimatorController;
                }
            }

            // Кэшируем оригинальный префаб для будущих инстанцирований
            if (cacheInstances)
            {
                _instanceCache[avatarId] = entry.Prefab;
            }

            Debug.Log($"[AvatarService] Avatar loaded: {avatarId}");
            OnAvatarLoaded?.Invoke(avatarId, avatar);
            
            return avatar;
        }

        /// <summary>
        /// Загружает аватар асинхронно (для совместимости с UI).
        /// В текущей реализации - синхронная загрузка, но сохраняет callback-паттерн.
        /// </summary>
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

        /// <summary>
        /// Получает информацию об аватаре без загрузки.
        /// </summary>
        public AvatarEntry GetAvatarInfo(string avatarId)
        {
            return avatarCatalog?.GetById(avatarId);
        }

        /// <summary>
        /// Получает список всех доступных аватаров.
        /// </summary>
        public IReadOnlyList<AvatarEntry> GetAllAvatars()
        {
            return avatarCatalog?.Avatars ?? new List<AvatarEntry>();
        }

        /// <summary>
        /// Получает аватары по категории.
        /// </summary>
        public List<AvatarEntry> GetAvatarsByCategory(AvatarCategory category)
        {
            return avatarCatalog?.GetByCategory(category) ?? new List<AvatarEntry>();
        }

        /// <summary>
        /// Очищает кэш загруженных аватаров.
        /// </summary>
        public void ClearCache()
        {
            _instanceCache.Clear();
            Debug.Log("[AvatarService] Cache cleared");
        }

        private void OnDestroy()
        {
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
