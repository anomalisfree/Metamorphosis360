using Main.Domain;
using UnityEngine;

namespace Main.Infrastructure
{
    public static class UserDataRepository
    {
        private const string Key_UserId = "User_UserId";
        private const string Key_Email = "User_Email";
        private const string Key_DisplayName = "User_DisplayName";
        private const string Key_AvatarId = "User_AvatarId";
        private const string Key_IsGuest = "User_IsGuest";
        private const string Key_CreatedAt = "User_CreatedAt";

        public static bool HasSavedUser => PlayerPrefs.HasKey(Key_UserId);

        public static void Save(UserData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[UserDataRepository] Attempted to save null UserData");
                return;
            }

            PlayerPrefs.SetString(Key_UserId, data.UserId);
            PlayerPrefs.SetString(Key_Email, data.Email);
            PlayerPrefs.SetString(Key_DisplayName, data.DisplayName);
            PlayerPrefs.SetString(Key_AvatarId, data.AvatarId);
            PlayerPrefs.SetInt(Key_IsGuest, data.IsGuest ? 1 : 0);
            PlayerPrefs.SetString(Key_CreatedAt, data.CreatedAt.ToString());
            PlayerPrefs.Save();

            Debug.Log($"[UserDataRepository] Saved user: {data.UserId}");
        }

        public static UserData Load()
        {
            if (!HasSavedUser)
            {
                Debug.Log("[UserDataRepository] No saved user data");
                return null;
            }

            var createdAtStr = PlayerPrefs.GetString(Key_CreatedAt, "0");
            long.TryParse(createdAtStr, out var createdAt);

            var userData = new UserData(
                userId: PlayerPrefs.GetString(Key_UserId),
                email: PlayerPrefs.GetString(Key_Email, ""),
                displayName: PlayerPrefs.GetString(Key_DisplayName, ""),
                avatarId: PlayerPrefs.GetString(Key_AvatarId, ""),
                isGuest: PlayerPrefs.GetInt(Key_IsGuest, 0) == 1,
                createdAt: createdAt
            );

            Debug.Log($"[UserDataRepository] Loaded user: {userData.UserId}");
            return userData;
        }

        public static void UpdateAvatarId(string avatarId)
        {
            PlayerPrefs.SetString(Key_AvatarId, avatarId ?? "");
            PlayerPrefs.Save();
            Debug.Log($"[UserDataRepository] Updated avatar: {avatarId}");
        }

        public static void UpdateDisplayName(string displayName)
        {
            PlayerPrefs.SetString(Key_DisplayName, displayName ?? "");
            PlayerPrefs.Save();
            Debug.Log($"[UserDataRepository] Updated display name: {displayName}");
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(Key_UserId);
            PlayerPrefs.DeleteKey(Key_Email);
            PlayerPrefs.DeleteKey(Key_DisplayName);
            PlayerPrefs.DeleteKey(Key_AvatarId);
            PlayerPrefs.DeleteKey(Key_IsGuest);
            PlayerPrefs.DeleteKey(Key_CreatedAt);
            PlayerPrefs.Save();

            Debug.Log("[UserDataRepository] Cleared user data");
        }

        public static string GetAvatarId()
        {
            return PlayerPrefs.GetString(Key_AvatarId, "");
        }

        public static string GetUserId()
        {
            return PlayerPrefs.GetString(Key_UserId, "");
        }
        
        public static bool IsCurrentUserGuest()
        {
            return PlayerPrefs.GetInt(Key_IsGuest, 0) == 1;
        }
    }
}
