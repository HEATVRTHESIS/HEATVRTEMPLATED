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
        StartCoroutine(LoadLevelCoroutine());
    }

    private IEnumerator LoadLevelCoroutine()
    {
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad);

            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }
    }
}