using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    // The [SerializeField] attribute exposes the private 'sceneToLoad' string to the Inspector.
    // This allows you to set the scene name directly in Unity's properties panel.
    [SerializeField]
    private string sceneToLoad;

    // A public function that can be called by a button or another script.
    public void LoadNewLevel()
    {
        StartCoroutine(LoadSceneCoroutine());
    }

    private IEnumerator LoadSceneCoroutine()
    {
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            // Start loading the scene but don't activate it yet
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad);
            asyncLoad.allowSceneActivation = false;

            while (asyncLoad.progress < 0.9f)
            {
                yield return null;
            }

            // Scene is loaded but not activated - give it a few frames
            // This helps heavy initialization spread across frames
            yield return new WaitForSeconds(0.1f);

            // Now activate the scene
            asyncLoad.allowSceneActivation = true;

            // Wait for actual activation to complete
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            // Give the scene extra time after activation for initialization
            // This is crucial for heavy scenes like yours with FlameEngine
            yield return new WaitForSeconds(0.2f);
            System.GC.Collect();
        }
    }
}