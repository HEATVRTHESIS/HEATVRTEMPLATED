using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages department completion status and unlocks.
/// Saves progress to PlayerPrefs for persistence between sessions.
/// </summary>
public class DepartmentSaveData : MonoBehaviour
{
    public static DepartmentSaveData Instance { get; private set; }

    [System.Serializable]
    public class DepartmentProgress
    {
        public string departmentName;
        public bool trainingCompleted;
        public bool simulationUnlocked;
        public int bestScore;
        public float bestCompletionPercentage;
        
        public DepartmentProgress(string name)
        {
            departmentName = name;
            trainingCompleted = false;
            simulationUnlocked = false;
            bestScore = 0;
            bestCompletionPercentage = 0f;
        }
    }

    private Dictionary<string, DepartmentProgress> departmentProgress = new Dictionary<string, DepartmentProgress>();

    [Header("Unlock Requirements")]
    [SerializeField] private int minimumScoreRequired = 70; // Minimum score percentage to unlock simulation
    [SerializeField] private float minimumCompletionRequired = 80f; // Minimum completion percentage

    [Header("Debug / Build Overrides")]
    [Tooltip("If enabled, all department simulations are treated as unlocked regardless of saved progress.")]
    [SerializeField] private bool bypassSimulationLocks = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAllProgress();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Call this after completing a training level to check if simulation should unlock
    /// </summary>
    public void CompleteTrainingLevel(string departmentName, int finalScore, int perfectScore, float completionPercentage)
    {
        if (!departmentProgress.ContainsKey(departmentName))
        {
            departmentProgress[departmentName] = new DepartmentProgress(departmentName);
        }

        DepartmentProgress progress = departmentProgress[departmentName];
        
        // Update best score if this is better
        if (finalScore > progress.bestScore)
        {
            progress.bestScore = finalScore;
        }

        // Update best completion percentage
        if (completionPercentage > progress.bestCompletionPercentage)
        {
            progress.bestCompletionPercentage = completionPercentage;
        }

        // Calculate score percentage
        float scorePercentage = perfectScore > 0 ? ((float)finalScore / perfectScore) * 100f : 0f;

        // Mark training as completed
        progress.trainingCompleted = true;

        // Check if requirements are met to unlock simulation
        if (scorePercentage >= minimumScoreRequired && completionPercentage >= minimumCompletionRequired)
        {
            progress.simulationUnlocked = true;
            Debug.Log($"✓ {departmentName} Simulation UNLOCKED! Score: {scorePercentage:F1}%, Completion: {completionPercentage:F1}%");
        }
        else
        {
            Debug.Log($"✗ {departmentName} Simulation still locked. Score: {scorePercentage:F1}% (need {minimumScoreRequired}%), Completion: {completionPercentage:F1}% (need {minimumCompletionRequired}%)");
        }

        SaveProgress(departmentName);
    }

    /// <summary>
    /// Check if a department's simulation mode is unlocked
    /// </summary>
    public bool IsSimulationUnlocked(string departmentName)
    {
        if (bypassSimulationLocks)
        {
            return true;
        }

        if (!departmentProgress.ContainsKey(departmentName))
        {
            return false;
        }
        return departmentProgress[departmentName].simulationUnlocked;
    }

    /// <summary>
    /// Check if a department's training has been completed
    /// </summary>
    public bool IsTrainingCompleted(string departmentName)
    {
        if (!departmentProgress.ContainsKey(departmentName))
        {
            return false;
        }
        return departmentProgress[departmentName].trainingCompleted;
    }

    /// <summary>
    /// Get the best score for a department
    /// </summary>
    public int GetBestScore(string departmentName)
    {
        if (!departmentProgress.ContainsKey(departmentName))
        {
            return 0;
        }
        return departmentProgress[departmentName].bestScore;
    }

    /// <summary>
    /// Save progress for a specific department to PlayerPrefs
    /// </summary>
    private void SaveProgress(string departmentName)
    {
        if (!departmentProgress.ContainsKey(departmentName)) return;

        DepartmentProgress progress = departmentProgress[departmentName];
        
        string prefix = $"Dept_{departmentName}_";
        PlayerPrefs.SetInt(prefix + "TrainingCompleted", progress.trainingCompleted ? 1 : 0);
        PlayerPrefs.SetInt(prefix + "SimulationUnlocked", progress.simulationUnlocked ? 1 : 0);
        PlayerPrefs.SetInt(prefix + "BestScore", progress.bestScore);
        PlayerPrefs.SetFloat(prefix + "BestCompletion", progress.bestCompletionPercentage);
        PlayerPrefs.Save();

        Debug.Log($"Saved progress for {departmentName}");
    }

    /// <summary>
    /// Load progress for all known departments from PlayerPrefs
    /// </summary>
    private void LoadAllProgress()
    {
        // You can expand this list or make it dynamic based on your departments
        string[] knownDepartments = { "MedTech", "ER", "Dietary" };

        foreach (string deptName in knownDepartments)
        {
            LoadProgress(deptName);
        }

        Debug.Log($"Loaded progress for {departmentProgress.Count} departments");
    }

    /// <summary>
    /// Load progress for a specific department from PlayerPrefs
    /// </summary>
    private void LoadProgress(string departmentName)
    {
        string prefix = $"Dept_{departmentName}_";
        
        if (PlayerPrefs.HasKey(prefix + "TrainingCompleted"))
        {
            DepartmentProgress progress = new DepartmentProgress(departmentName)
            {
                trainingCompleted = PlayerPrefs.GetInt(prefix + "TrainingCompleted") == 1,
                simulationUnlocked = PlayerPrefs.GetInt(prefix + "SimulationUnlocked") == 1,
                bestScore = PlayerPrefs.GetInt(prefix + "BestScore"),
                bestCompletionPercentage = PlayerPrefs.GetFloat(prefix + "BestCompletion")
            };

            departmentProgress[departmentName] = progress;
            Debug.Log($"Loaded {departmentName}: Training={progress.trainingCompleted}, Simulation={progress.simulationUnlocked}");
        }
    }

    /// <summary>
    /// Clear all saved progress (useful for testing or reset button)
    /// </summary>
    public void ResetAllProgress()
    {
        PlayerPrefs.DeleteAll();
        departmentProgress.Clear();
        Debug.Log("All department progress has been reset");
    }

    /// <summary>
    /// Manually unlock a simulation (for testing)
    /// </summary>
    public void UnlockSimulation(string departmentName)
    {
        if (!departmentProgress.ContainsKey(departmentName))
        {
            departmentProgress[departmentName] = new DepartmentProgress(departmentName);
        }

        departmentProgress[departmentName].simulationUnlocked = true;
        departmentProgress[departmentName].trainingCompleted = true;
        SaveProgress(departmentName);
        Debug.Log($"Manually unlocked simulation for {departmentName}");
    }

    public int GetMinimumScoreRequired() => minimumScoreRequired;
    public float GetMinimumCompletionRequired() => minimumCompletionRequired;
}