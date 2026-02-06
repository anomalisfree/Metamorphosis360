using System.Collections.Generic;
using Main.Domain;
using UnityEngine;

namespace Main.Infrastructure
{
    [CreateAssetMenu(fileName = "AvatarCatalog", menuName = "Main/Avatar Catalog")]
    public sealed class AvatarCatalog : ScriptableObject
    {
        [Header("Available Avatars")]
        [SerializeField] private List<AvatarEntry> avatars = new();
        
        [Header("Settings")]
        [SerializeField] private string defaultAvatarId = "avatar_default";

        public IReadOnlyList<AvatarEntry> Avatars => avatars;
        public string DefaultAvatarId => defaultAvatarId;

        public AvatarEntry GetById(string avatarId)
        {
            if (string.IsNullOrEmpty(avatarId))
                return GetDefault();

            foreach (var avatar in avatars)
            {
                if (avatar.Id == avatarId)
                    return avatar;
            }

            Debug.LogWarning($"[AvatarCatalog] Avatar not found: {avatarId}, using default");
            return GetDefault();
        }

        public AvatarEntry GetDefault()
        {
            if (avatars.Count == 0)
            {
                Debug.LogError("[AvatarCatalog] No avatars in catalog!");
                return null;
            }

            foreach (var avatar in avatars)
            {
                if (avatar.Id == defaultAvatarId)
                    return avatar;
            }

            return avatars[0];
        }

        public List<AvatarEntry> GetByCategory(AvatarCategory category)
        {
            var result = new List<AvatarEntry>();
            foreach (var avatar in avatars)
            {
                if (avatar.Category == category)
                    result.Add(avatar);
            }
            return result;
        }

        public bool Exists(string avatarId)
        {
            foreach (var avatar in avatars)
            {
                if (avatar.Id == avatarId)
                    return true;
            }
            return false;
        }

#if UNITY_EDITOR
        public void AddAvatar(AvatarEntry entry)
        {
            avatars.Add(entry);
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }

    [System.Serializable]
    public sealed class AvatarEntry
    {
        [Header("Identification")]
        [Tooltip("Unique ID for the avatar, used for saving/loading user selection")]
        [SerializeField] private string id;
        
        [Tooltip("Display name")]
        [SerializeField] private string displayName;

        [Header("Resources")]
        [Tooltip("Reference to the avatar prefab")]
        [SerializeField] private GameObject prefab;
        
        [Tooltip("Preview image for UI selection")]
        [SerializeField] private Sprite previewSprite;

        [Header("Classification")]
        [Tooltip("Avatar category")]
        [SerializeField] private AvatarCategory category = AvatarCategory.Neutral;

        [Header("Animation")]
        [Tooltip("Animation controller (optional, if not embedded in the prefab)")]
        [SerializeField] private RuntimeAnimatorController animatorController;

        public string Id => id;
        public string DisplayName => displayName;
        public GameObject Prefab => prefab;
        public Sprite PreviewSprite => previewSprite;
        public AvatarCategory Category => category;
        public RuntimeAnimatorController AnimatorController => animatorController;

        public AvatarEntry() { }

        public AvatarEntry(string id, string displayName, GameObject prefab, 
            Sprite previewSprite = null, AvatarCategory category = AvatarCategory.Neutral,
            RuntimeAnimatorController animatorController = null)
        {
            this.id = id;
            this.displayName = displayName;
            this.prefab = prefab;
            this.previewSprite = previewSprite;
            this.category = category;
            this.animatorController = animatorController;
        }
    }
}
