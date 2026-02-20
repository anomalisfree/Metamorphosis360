using Main.Domain;

namespace Main.Infrastructure
{
    public static class ARSessionData
    {
        public static EventData CurrentEvent { get; private set; }
        public static ARGameResult LastGameResult { get; private set; }
        public static bool HasActiveSession => CurrentEvent != null;

        public static void StartSession(EventData eventData)
        {
            CurrentEvent = eventData;
            LastGameResult = null;
            
            UnityEngine.Debug.Log($"[ARSessionData] Session started for event: {eventData?.Title}");
        }

        public static void EndSession(ARGameResult result)
        {
            LastGameResult = result;
            
            UnityEngine.Debug.Log($"[ARSessionData] Session ended. Success: {result?.IsSuccess}, Score: {result?.Score}");
        }

        public static void Clear()
        {
            CurrentEvent = null;
            LastGameResult = null;
            
            UnityEngine.Debug.Log("[ARSessionData] Session cleared");
        }
    }

    public sealed class ARGameResult
    {
        public bool IsSuccess { get; }
        public int Score { get; }

        public float TimeElapsed { get; }

        public string EventId { get; }

        public ARGameResult(bool isSuccess, int score, float timeElapsed, string eventId)
        {
            IsSuccess = isSuccess;
            Score = score;
            TimeElapsed = timeElapsed;
            EventId = eventId;
        }
    }
}
