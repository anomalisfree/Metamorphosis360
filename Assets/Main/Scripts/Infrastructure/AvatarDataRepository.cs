using Main.Domain;
using ReadyPlayerMe.Core;
using UnityEngine;

namespace Main.Infrastructure
{
    public static class AvatarDataRepository
    {
        private const string Key_UserId = "Avatar_UserId";
        private const string Key_UserEmail = "Avatar_UserEmail";
        private const string Key_UserName = "Avatar_UserName";
        private const string Key_UserToken = "Avatar_UserToken";
        private const string Key_AvatarId = "Avatar_AvatarId";
        private const string Key_Partner = "Avatar_Partner";
        private const string Key_IsDraft = "Avatar_IsDraft";
        private const string Key_BodyType = "Avatar_BodyType";
        private const string Key_OutfitGender = "Avatar_OutfitGender";

        public static bool HasSavedAvatar => PlayerPrefs.HasKey(Key_UserId);

        public static void Save(Domain.AvatarData data)
        {
            PlayerPrefs.SetString(Key_UserId, data.UserId);
            PlayerPrefs.SetString(Key_UserEmail, data.UserEmail);
            PlayerPrefs.SetString(Key_UserName, data.UserName);
            PlayerPrefs.SetString(Key_UserToken, data.UserToken);
            PlayerPrefs.SetString(Key_AvatarId, data.AvatarId);
            PlayerPrefs.SetString(Key_Partner, data.AvatarPartner);
            PlayerPrefs.SetInt(Key_IsDraft, data.AvatarIsDraft ? 1 : 0);
            PlayerPrefs.SetInt(Key_BodyType, (int)data.AvatarBodyType);
            PlayerPrefs.SetInt(Key_OutfitGender, (int)data.AvatarOutfitGender);
            PlayerPrefs.Save();
        }

        public static Domain.AvatarData Load()
        {
            if (!HasSavedAvatar)
                return null;

            return new Domain.AvatarData(
                userId: PlayerPrefs.GetString(Key_UserId),
                userEmail: PlayerPrefs.GetString(Key_UserEmail),
                userName: PlayerPrefs.GetString(Key_UserName),
                userToken: PlayerPrefs.GetString(Key_UserToken),
                avatarId: PlayerPrefs.GetString(Key_AvatarId),
                avatarPartner: PlayerPrefs.GetString(Key_Partner),
                avatarIsDraft: PlayerPrefs.GetInt(Key_IsDraft) == 1,
                avatarBodyType: (BodyType)PlayerPrefs.GetInt(Key_BodyType),
                avatarOutfitGender: (OutfitGender)PlayerPrefs.GetInt(Key_OutfitGender)
            );
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(Key_UserId);
            PlayerPrefs.DeleteKey(Key_UserEmail);
            PlayerPrefs.DeleteKey(Key_UserName);
            PlayerPrefs.DeleteKey(Key_UserToken);
            PlayerPrefs.DeleteKey(Key_AvatarId);
            PlayerPrefs.DeleteKey(Key_Partner);
            PlayerPrefs.DeleteKey(Key_IsDraft);
            PlayerPrefs.DeleteKey(Key_BodyType);
            PlayerPrefs.DeleteKey(Key_OutfitGender);
            PlayerPrefs.Save();
        }
    }
}
