using System.Collections;
using UnityEngine;
using Main.Infrastructure;

namespace Main.Core
{
    public sealed class AppFlowController : MonoBehaviour
    {
        [SerializeField] private AppConfig config;
        [SerializeField] private SceneLoader sceneLoader;

        private GameBootstrap _bootstrap;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            
            _bootstrap = GameBootstrap.Instance;

            if (_bootstrap != null)
            {
                _bootstrap.StateMachine.OnStateChanged += HandleStateChanged;
                Debug.Log("[AppFlowController] Subscribed to StateMachine");
            }
            else
            {
                Debug.LogError("[AppFlowController] GameBootstrap instance not found!");
            }
        }

        private void OnDestroy()
        {
            if (_bootstrap != null)
            {
                _bootstrap.StateMachine.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(AppState prev, AppState next)
        {
            Debug.Log($"[AppFlowController] State changed: {prev} -> {next}");
            
            switch (next)
            {
                case AppState.Boot:
                    // Boot scene already loaded; nothing to do.
                    break;
                case AppState.Auth:
                    StartCoroutine(Load(config.authScene));
                    break;
                case AppState.Map:
                    StartCoroutine(Load(config.mapScene));
                    break;
                case AppState.AR:
                    StartCoroutine(Load(config.arScene));
                    break;
            }
        }

        private IEnumerator Load(string sceneName)
        {
            Debug.Log($"[AppFlowController] Loading scene: {sceneName}");
            
            if (sceneLoader == null)
            {
                Debug.LogError("[AppFlowController] SceneLoader is null!");
                yield break;
            }

            yield return sceneLoader.LoadSceneAsync(sceneName);
        }
    }
}
