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
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad);

            while (!asyncLoad.isDone)
            {
                yield return null;
            }
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