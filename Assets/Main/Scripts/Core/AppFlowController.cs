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
            _bootstrap = GameBootstrap.Instance;

            if (_bootstrap != null)
            {
                _bootstrap.StateMachine.OnStateChanged += HandleStateChanged;
            }
            else
            {
                Debug.LogError("AppFlowController: GameBootstrap instance not found!");
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
            if (sceneLoader == null)
            {
                yield break;
            }

            yield return sceneLoader.LoadSceneAsync(sceneName);
        }
    }
}
