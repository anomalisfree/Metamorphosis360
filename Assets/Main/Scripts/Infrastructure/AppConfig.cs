using UnityEngine;

namespace Main.Infrastructure
{
    [CreateAssetMenu(fileName = "AppConfig", menuName = "Main/App Config")]
    public sealed class AppConfig : ScriptableObject
    {
        [Header("Scenes")]
        public string splashScene = "Splash";
        public string authScene = "Auth";
        public string mapScene = "Map";
        public string arScene = "AR";

        [Header("Gameplay")]
        public float poiQueryRadiusMeters = 200f;
        public float enterArRadiusMeters = 30f;
    }
}
