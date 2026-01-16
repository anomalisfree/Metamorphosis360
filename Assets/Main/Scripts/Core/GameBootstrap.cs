using System.Collections;
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
        }

        private IEnumerator Start()
        {
            if (autoAdvanceToAuth)
            {
                yield return null;
                StateMachine.SetState(AppState.Auth);
            }
        }
    }
}
