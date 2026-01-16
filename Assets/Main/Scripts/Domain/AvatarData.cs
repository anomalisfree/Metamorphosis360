using ReadyPlayerMe.Core;

namespace Main.Domain
{
    public sealed class AvatarData
    {
        public string UserId { get; }
        public string UserEmail { get; }
        public string UserName { get; }
        public string UserToken { get; }
        public string AvatarId { get; }
        public string AvatarPartner { get; }
        public bool AvatarIsDraft { get; }
        public BodyType AvatarBodyType { get; }
        public OutfitGender AvatarOutfitGender { get; }

        public AvatarData(string userId, string userEmail = "", string userName = "", string userToken = "",
            string avatarId = "", string avatarPartner = "", bool avatarIsDraft = false,
            BodyType avatarBodyType = BodyType.FullBody, OutfitGender avatarOutfitGender = OutfitGender.Neutral)
        {
            UserId = userId;
            UserEmail = userEmail;
            UserName = userName;
            UserToken = userToken;
            AvatarId = avatarId;
            AvatarPartner = avatarPartner;
            AvatarIsDraft = avatarIsDraft;
            AvatarBodyType = avatarBodyType;
            AvatarOutfitGender = avatarOutfitGender;
        }
    }
}
