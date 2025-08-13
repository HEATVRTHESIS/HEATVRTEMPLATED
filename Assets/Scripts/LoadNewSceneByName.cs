using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // The [SerializeField] attribute exposes the private 'sceneToLoad' string to the Inspector.
    // This allows you to set the scene name directly in Unity's properties panel.
    [SerializeField] 
    private string sceneToLoad;

    // A public function that can be called by a button or another script.
    public void LoadNewLevel()
    {
        // It's a good practice to check if the scene name is not null or empty.
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            // This is the correct way to load a scene using the variable.
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError("The 'Scene To Load' field is empty! Please enter a scene name in the Inspector.");
        }
    }
}