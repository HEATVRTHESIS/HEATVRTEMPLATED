using UnityEngine;

/// <summary>
/// Attach this to the Training Results scene GameObject with FinalEvaluationScreen.
/// This automatically saves department completion data when training levels are completed.
/// DO NOT attach to Simulation Results scene - only Training Results!
/// </summary>
public class DepartmentCompletionHandler : MonoBehaviour
{
    [Header("Result Scene Type")]
    [SerializeField] private bool isTrainingResultScene = true; // Set to TRUE for training results, FALSE for simulation results
    
    [Header("Auto-Detect Department")]
    [Tooltip("Leave empty to auto-detect from completed levels")]
    [SerializeField] private string departmentNameOverride = ""; // Optional: manually specify department name
    
    private FinalEvaluationScreen evaluationScreen;
    private bool completionSaved = false;

    void Awake()
    {
        evaluationScreen = GetComponent<FinalEvaluationScreen>();
        if (evaluationScreen == null)
        {
            Debug.LogError("DepartmentCompletionHandler requires FinalEvaluationScreen component!");
        }
    }

    void Start()
    {
        // Only save completion for Training result scenes, not Simulation results
        if (!isTrainingResultScene)
        {
            Debug.Log("This is a Simulation result scene - skipping department unlock save");
            return;
        }
        
        // Small delay to ensure FinalEvaluationScreen has calculated results
        Invoke(nameof(SaveDepartmentCompletion), 0.5f);
    }

    private void SaveDepartmentCompletion()
    {
        if (completionSaved) return;
        
        if (DepartmentSaveData.Instance == null)
        {
            Debug.LogError("DepartmentSaveData.Instance not found! Make sure it exists in the scene.");
            return;
        }

        if (ScoreDataManager.Instance == null)
        {
            Debug.LogError("ScoreDataManager.Instance not found!");
            return;
        }

        // Get all level data to calculate total score and completion
        var allLevels = ScoreDataManager.Instance.GetAllLevelData();
        
        if (allLevels.Count == 0)
        {
            Debug.LogWarning("No level data found to save!");
            return;
        }

        // Auto-detect department name from the level scene names
        string detectedDepartment = DetectDepartmentName(allLevels);
        
        // Use override if provided, otherwise use detected name
        string departmentName = string.IsNullOrEmpty(departmentNameOverride) ? detectedDepartment : departmentNameOverride;
        
        if (string.IsNullOrEmpty(departmentName))
        {
            Debug.LogError("Could not determine department name! Set departmentNameOverride in inspector.");
            return;
        }

        // Calculate totals
        int totalScore = 0;
        int totalPerfectScore = 0;
        int completedTasks = 0;
        int totalTasks = 0;

        foreach (var level in allLevels)
        {
            totalScore += level.finalScore;
            completedTasks += level.completedTasks;
            totalTasks += level.totalTasks;
            
            // Calculate perfect score based on level type
            if (level.levelType == "Standard")
            {
                totalPerfectScore += level.perfectScore;
            }
            else if (level.levelType == "Fire")
            {
                totalPerfectScore += level.completedTasks * 10; // Fire levels: 10 points per task
            }
            else if (level.levelType == "Fire Evacuation")
            {
                totalPerfectScore += 45; // Fire evacuation has max 45 points
            }
        }

        // Calculate percentages
        float scorePercentage = totalPerfectScore > 0 ? ((float)totalScore / totalPerfectScore) * 100f : 0f;
        float completionPercentage = totalTasks > 0 ? ((float)completedTasks / totalTasks) * 100f : 0f;

        // Use the average for the final completion metric
        float finalCompletionMetric = (scorePercentage + completionPercentage) / 2f;

        Debug.Log($"===== SAVING DEPARTMENT COMPLETION =====");
        Debug.Log($"Department: {departmentName}");
        Debug.Log($"Total Score: {totalScore}/{totalPerfectScore} ({scorePercentage:F1}%)");
        Debug.Log($"Tasks: {completedTasks}/{totalTasks} ({completionPercentage:F1}%)");
        Debug.Log($"Final Completion: {finalCompletionMetric:F1}%");

        // Save to DepartmentSaveData
        DepartmentSaveData.Instance.CompleteTrainingLevel(
            departmentName,
            totalScore,
            totalPerfectScore,
            finalCompletionMetric
        );

        completionSaved = true;

        // Optional: Show feedback to player
        bool isUnlocked = DepartmentSaveData.Instance.IsSimulationUnlocked(departmentName);
        if (isUnlocked)
        {
            Debug.Log($"🎉 {departmentName} Simulation Mode UNLOCKED!");
        }
        else
        {
            Debug.Log($"Keep trying! Complete with higher score/completion to unlock {departmentName} Simulation.");
        }
    }

    /// <summary>
    /// Auto-detect department name from level scene names
    /// Looks for common patterns like "MedTech", "ER", "Dietary" in scene names
    /// </summary>
    private string DetectDepartmentName(System.Collections.Generic.List<ScoreDataManager.LevelScoreData> levels)
    {
        if (levels.Count == 0) return "";

        // Get the first level's name to analyze
        string firstLevelName = levels[0].levelName;

        // Common department name patterns
        if (firstLevelName.Contains("MedTech") || firstLevelName.Contains("medtech"))
            return "MedTech";
        
        if (firstLevelName.Contains("ER") || firstLevelName.Contains("Emergency"))
            return "ER";
        
        if (firstLevelName.Contains("Dietary") || firstLevelName.Contains("dietary"))
            return "Dietary";

        // If no pattern found, try to extract from scene name
        // Example: "MedTechDepartment" -> "MedTech"
        // Example: "ERDeptTraining" -> "ER"
        
        Debug.LogWarning($"Could not auto-detect department from scene name: {firstLevelName}");
        return "";
    }

    // Optional: Call this manually if you want to trigger the save at a specific time
    public void ManualSaveTrigger()
    {
        completionSaved = false;
        SaveDepartmentCompletion();
    }

    // Helper method to get the detected or overridden department name
    public string GetDepartmentName()
    {
        if (!string.IsNullOrEmpty(departmentNameOverride))
            return departmentNameOverride;

        if (ScoreDataManager.Instance != null)
        {
            var levels = ScoreDataManager.Instance.GetAllLevelData();
            return DetectDepartmentName(levels);
        }

        return "";
    }

    // Helper to check if simulation was unlocked
    public bool WasSimulationUnlocked()
    {
        if (DepartmentSaveData.Instance == null) return false;
        string deptName = GetDepartmentName();
        if (string.IsNullOrEmpty(deptName)) return false;
        return DepartmentSaveData.Instance.IsSimulationUnlocked(deptName);
    }
}