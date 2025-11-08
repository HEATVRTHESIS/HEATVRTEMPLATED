using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class DepartmentSceneLoader : MonoBehaviour
{
    [SerializeField]
    private string sceneToLoad;
    
    [Header("Department Settings")]
    [Tooltip("The department name to pass to the next scene (e.g., 'ER', 'MedTech', 'Dietary')")]
    [SerializeField]
    private string departmentName;

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
            // Start loading the scene but don't activate it yet
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad);
            asyncLoad.allowSceneActivation = false;

            // Wait for scene to be almost ready (0.9 = 90%)
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

/// <summary>
/// Static class to persist data between scenes
/// </summary>
public static class DepartmentData
{
    public static string selectedDepartment = "";
}