using System;

namespace Main.Domain
{
    [Serializable]
    public sealed class PlayerLocationData
    {
        public string UserId;
        public string UserName;
        public string AvatarId;
        public string AvatarGender;
        public double Latitude;
        public double Longitude;
        public long CreatedAt;
        public long UpdatedAt;
        public bool IsOnline;

        public PlayerLocationData() { }

        public PlayerLocationData(AvatarData avatarData, double latitude, double longitude)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            UserId = avatarData.UserId;
            UserName = avatarData.UserName;
            AvatarId = avatarData.AvatarId;
            AvatarGender = avatarData.AvatarOutfitGender.ToString();
            Latitude = latitude;
            Longitude = longitude;
            CreatedAt = now;
            UpdatedAt = now;
            IsOnline = true;
        }

        public void UpdateLocation(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
            UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public bool IsStale(long maxAgeMilliseconds)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return now - UpdatedAt > maxAgeMilliseconds;
        }
    }
}
