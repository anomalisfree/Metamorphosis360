using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Main.Infrastructure
{
    public sealed class SceneLoader : MonoBehaviour
    {
        public IEnumerator LoadSceneAsync(string sceneName)
        {
            var op = SceneManager.LoadSceneAsync(sceneName);
            while (!op.isDone)
            {
                yield return null;
            }
        }
    }
}
