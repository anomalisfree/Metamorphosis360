using System.Collections;
using Main.Services;
using UnityEngine;

namespace Main.Core
{
    [DefaultExecutionOrder(-100)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField]
        private bool autoAdvanceToAuth = true;

        public static GameBootstrap Instance { get; private set; }

        public AppStateMachine StateMachine { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            StateMachine = new AppStateMachine();
            StateMachine.SetState(AppState.Boot);
            
            EnsurePersistentServices();
        }

        private IEnumerator Start()
        {
            if (autoAdvanceToAuth)
            {
                yield return null;
                StateMachine.SetState(AppState.Auth);
            }
        }

        private void EnsurePersistentServices()
        {
            // Порядок важен: Firebase → Auth → Avatar → Events
            
            if (FirebaseDatabaseService.Instance == null)
            {
                var obj = new GameObject("FirebaseDatabaseService");
                obj.transform.SetParent(transform);
                obj.AddComponent<FirebaseDatabaseService>();
            }

            if (AuthService.Instance == null)
            {
                var obj = new GameObject("AuthService");
                obj.transform.SetParent(transform);
                obj.AddComponent<AuthService>();
            }

            if (AvatarService.Instance == null)
            {
                var obj = new GameObject("AvatarService");
                obj.transform.SetParent(transform);
                obj.AddComponent<AvatarService>();
            }

            if (EventsService.Instance == null)
            {
                var obj = new GameObject("EventsService");
                obj.transform.SetParent(transform);
                obj.AddComponent<EventsService>();
            }
        }
    }
}
