using System;
using UnityEngine;

namespace Main.Domain
{
    [Serializable]
    public sealed class AvatarInfo
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private string prefabPath;
        [SerializeField] private Sprite previewImage;
        [SerializeField] private AvatarCategory category;

        public string Id => id;
        public string DisplayName => displayName;
        public string PrefabPath => prefabPath;
        public Sprite PreviewImage => previewImage;
        public AvatarCategory Category => category;

        public AvatarInfo() { }

        public AvatarInfo(string id, string displayName, string prefabPath, 
            Sprite previewImage = null, AvatarCategory category = AvatarCategory.Neutral)
        {
            this.id = id;
            this.displayName = displayName;
            this.prefabPath = prefabPath;
            this.previewImage = previewImage;
            this.category = category;
        }
    }

    public enum AvatarCategory
    {
        Neutral = 0,
        Masculine = 1,
        Feminine = 2,
        Fantasy = 3,
        Robot = 4
    }
}
