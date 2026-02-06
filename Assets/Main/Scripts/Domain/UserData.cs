using System;

namespace Main.Domain
{
    /// <summary>
    /// Данные авторизованного пользователя.
    /// Не зависит от Ready Player Me - использует локальные префабы аватаров.
    /// </summary>
    [Serializable]
    public sealed class UserData
    {
        /// <summary>
        /// Уникальный идентификатор пользователя (Firebase UID).
        /// </summary>
        public string UserId { get; }
        
        /// <summary>
        /// Email пользователя (пустой для гостей).
        /// </summary>
        public string Email { get; }
        
        /// <summary>
        /// Отображаемое имя пользователя.
        /// </summary>
        public string DisplayName { get; }
        
        /// <summary>
        /// ID выбранного аватара (ключ для загрузки из Resources).
        /// Например: "avatar_male_01", "avatar_female_02"
        /// </summary>
        public string AvatarId { get; }
        
        /// <summary>
        /// Является ли пользователь гостем (анонимная авторизация).
        /// </summary>
        public bool IsGuest { get; }
        
        /// <summary>
        /// Время создания аккаунта (Unix timestamp в миллисекундах).
        /// </summary>
        public long CreatedAt { get; }

        public UserData(
            string userId,
            string email = "",
            string displayName = "",
            string avatarId = "",
            bool isGuest = false,
            long createdAt = 0)
        {
            UserId = userId ?? throw new ArgumentNullException(nameof(userId));
            Email = email ?? "";
            DisplayName = displayName ?? "";
            AvatarId = avatarId ?? "";
            IsGuest = isGuest;
            CreatedAt = createdAt > 0 ? createdAt : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// Создаёт копию UserData с изменённым аватаром.
        /// </summary>
        public UserData WithAvatar(string newAvatarId)
        {
            return new UserData(
                userId: UserId,
                email: Email,
                displayName: DisplayName,
                avatarId: newAvatarId,
                isGuest: IsGuest,
                createdAt: CreatedAt
            );
        }

        /// <summary>
        /// Создаёт копию UserData с изменённым именем.
        /// </summary>
        public UserData WithDisplayName(string newDisplayName)
        {
            return new UserData(
                userId: UserId,
                email: Email,
                displayName: newDisplayName,
                avatarId: AvatarId,
                isGuest: IsGuest,
                createdAt: CreatedAt
            );
        }

        /// <summary>
        /// Проверяет, выбран ли аватар.
        /// </summary>
        public bool HasAvatar => !string.IsNullOrEmpty(AvatarId);

        /// <summary>
        /// Проверяет валидность данных пользователя.
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(UserId);

        public override string ToString()
        {
            return $"UserData[{UserId}, {DisplayName}, Avatar: {AvatarId}, Guest: {IsGuest}]";
        }
    }
}
