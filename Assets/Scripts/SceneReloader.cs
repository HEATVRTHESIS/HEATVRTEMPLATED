using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Reloads the current scene while preserving persistent data (ScoreDataManager and DepartmentData)
/// </summary>
public class SceneReloader : MonoBehaviour
{
    [Header("Department Persistence")]
    [Tooltip("The department will be stored here before reload")]
    private static string storedDepartment = "";
    
    /// <summary>
    /// Reloads the current scene from the beginning while keeping persistent objects and department data
    /// </summary>
    public void ReloadScene()
    {
        // Reset time scale in case it was paused
        Time.timeScale = 1f;
        
        // Store the current department before reloading
        // First check if there's already a department set
        if (!string.IsNullOrEmpty(DepartmentData.selectedDepartment))
        {
            storedDepartment = DepartmentData.selectedDepartment;
        }
        else
        {
            // Otherwise, get it from the EvacuationLevelManager
            EvacuationLevelManager levelManager = FindObjectOfType<EvacuationLevelManager>();
            if (levelManager != null)
            {
                storedDepartment = levelManager.selectedDepartment;
            }
        }
        
        // Restore the department data so it persists through reload
        DepartmentData.selectedDepartment = storedDepartment;
        
        // Get current scene name
        string currentSceneName = SceneManager.GetActiveScene().name;
        
        Debug.Log($"Reloading scene: {currentSceneName}");
        Debug.Log($"Preserving department: {DepartmentData.selectedDepartment}");
        
        // Note: SceneManager.LoadScene will preserve:
        // 1. DontDestroyOnLoad objects (ScoreDataManager)
        // 2. Static data (DepartmentData.selectedDepartment)
        // Both will persist through the reload!
        SceneManager.LoadScene(currentSceneName);
    }
    
    /// <summary>
    /// Alternative: Reload scene by build index
    /// </summary>
    public void ReloadSceneByIndex()
    {
        // Reset time scale
        Time.timeScale = 1f;
        
        // Store the current department before reloading
        if (!string.IsNullOrEmpty(DepartmentData.selectedDepartment))
        {
            storedDepartment = DepartmentData.selectedDepartment;
        }
        else
        {
            EvacuationLevelManager levelManager = FindObjectOfType<EvacuationLevelManager>();
            if (levelManager != null)
            {
                storedDepartment = levelManager.selectedDepartment;
            }
        }
        
        // Restore the department data
        DepartmentData.selectedDepartment = storedDepartment;
        
        // Get current scene build index
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        
        Debug.Log($"Reloading scene by index: {currentSceneIndex}");
        Debug.Log($"Preserving department: {DepartmentData.selectedDepartment}");
        
        // DontDestroyOnLoad objects and static data will persist
        SceneManager.LoadScene(currentSceneIndex);
    }
}