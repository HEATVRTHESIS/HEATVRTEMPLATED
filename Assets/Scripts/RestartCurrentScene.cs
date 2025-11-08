using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class RestartCurrentScene : MonoBehaviour
{
    // A public function that can be called by a button or another script.
    // This will reload the current active scene.
    public void RestartScene()
    {
        StartCoroutine(RestartSceneCoroutine());
    }

    private IEnumerator RestartSceneCoroutine()
    {
        // Get the current active scene
        Scene currentScene = SceneManager.GetActiveScene();
        
        // Load the current scene asynchronously
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(currentScene.name);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }
    
    // Alternative: Direct reload without coroutine (simpler but no loading control)
    public void RestartSceneImmediate()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}