using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Main.Infrastructure
{
    public sealed class SceneLoader : MonoBehaviour
    {
        public IEnumerator LoadSceneAsync(string sceneName)
        {
            Debug.Log($"[SceneLoader] Starting load: {sceneName}");
            
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[SceneLoader] Scene name is null or empty!");
                yield break;
            }
            
            var op = SceneManager.LoadSceneAsync(sceneName);
            
            if (op == null)
            {
                Debug.LogError($"[SceneLoader] Failed to start loading scene: {sceneName}");
                yield break;
            }
            
            while (!op.isDone)
            {
                yield return null;
            }
            
            Debug.Log($"[SceneLoader] Scene loaded: {sceneName}");
        }
    }
}
