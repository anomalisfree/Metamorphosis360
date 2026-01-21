using System;

namespace Main.Domain
{
    [Serializable]
    public sealed class EventData
    {
        public string Id;
        public string Title;
        public string Description;
        public string Type;
        public double Latitude;
        public double Longitude;
        public long StartTime;
        public long EndTime;
        public long CreatedAt;
        public long UpdatedAt;
        public bool IsActive;
        public string ImageUrl;
        public string ExternalLink;
        public int Radius; // activation radius in meters
        public string CreatorId;

        public EventData() { }

        public EventData(string id, string title, string description, string type,
            double latitude, double longitude, long startTime, long endTime)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            Id = id;
            Title = title;
            Description = description;
            Type = type;
            Latitude = latitude;
            Longitude = longitude;
            StartTime = startTime;
            EndTime = endTime;
            CreatedAt = now;
            UpdatedAt = now;
            IsActive = true;
            Radius = 50;    // default radius
        }

        public bool IsCurrentlyActive()
        {
            if (!IsActive)
                return false;

            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return now >= StartTime && now <= EndTime;
        }

        public bool IsUpcoming()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return now < StartTime;
        }

        public bool IsExpired()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return now > EndTime;
        }

        public DateTime GetStartDateTime()
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(StartTime).LocalDateTime;
        }

        public DateTime GetEndDateTime()
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(EndTime).LocalDateTime;
        }

        public void SetStartDateTime(DateTime dateTime)
        {
            StartTime = new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
        }

        public void SetEndDateTime(DateTime dateTime)
        {
            EndTime = new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
        }

        public void MarkUpdated()
        {
            UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }

    public static class EventType
    {
        public const string Quest = "quest";
        public const string Battle = "battle";
        public const string Social = "social";
        public const string Treasure = "treasure";
        public const string Boss = "boss";
        public const string Special = "special";
    }
}
