using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class DepartmentSceneLoader : MonoBehaviour
{
    [SerializeField]
    private string sceneToLoad;
    
    [Header("Department Settings")]
    [Tooltip("The department name to pass to the next scene (e.g., 'ER', 'MedTech', 'Dietary')")]
    [SerializeField]
    private string departmentName;

    [Header("Loading Screen")]
    [SerializeField]
    private GameObject loadingScreen; // Your loading screen UI panel
    
    [SerializeField]
    private Image progressBarImage; // The progress bar image (will fill left to right)

    public void LoadLevelWithDepartment()
    {
        // Store the department name before loading the scene
        if (!string.IsNullOrEmpty(departmentName))
        {
            DepartmentData.selectedDepartment = departmentName;
            Debug.Log($"Loading scene '{sceneToLoad}' with department: {departmentName}");
        }
        else
        {
            Debug.LogWarning("No department name set for scene loader!");
        }
        
        StartCoroutine(LoadSceneCoroutine());
    }
    
    /// <summary>
    /// Allows setting the department name via code
    /// </summary>
    public void SetDepartment(string dept)
    {
        departmentName = dept;
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

            // Start loading the scene but don't activate it yet
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

            // Wait for actual activation to complete
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            // Give the scene extra time after activation for initialization
            yield return new WaitForSeconds(0.2f);
            System.GC.Collect();
        }
    }

}

/// <summary>
/// Static class to persist data between scenes
/// </summary>
public static class DepartmentData
{
    public static string selectedDepartment = "";
}