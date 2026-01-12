using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    [SerializeField]
    private string sceneToLoad;

    [Header("Loading Screen")]
    [SerializeField]
    private GameObject loadingScreen; // Your loading screen UI panel
    
    [SerializeField]
    private Image progressBarImage; // The progress bar image (will fill left to right)

    public void LoadNewLevel()
    {
        StartCoroutine(LoadSceneCoroutine());
    }

    private IEnumerator LoadSceneCoroutine()
    {
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            // Show loading screen
            if (loadingScreen != null)
                loadingScreen.SetActive(true);

            // Set image to fill type
            if (progressBarImage != null)
            {
                progressBarImage.type = Image.Type.Filled;
                progressBarImage.fillMethod = Image.FillMethod.Horizontal;
                progressBarImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                progressBarImage.fillAmount = 0f;
            }

            // Start loading the scene
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad);
            asyncLoad.allowSceneActivation = false;

            // Update progress bar while loading
            while (asyncLoad.progress < 0.9f)
            {
                if (progressBarImage != null)
                    progressBarImage.fillAmount = asyncLoad.progress / 0.9f;
                
                yield return null;
            }

            // Set to full
            if (progressBarImage != null)
                progressBarImage.fillAmount = 1f;

            yield return new WaitForSeconds(0.1f);

            // Activate the scene
            asyncLoad.allowSceneActivation = true;

            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.2f);
            System.GC.Collect();
        }
    }
}