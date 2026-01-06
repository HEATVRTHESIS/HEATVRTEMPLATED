using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Enhanced persistent data manager that stores score data between scenes.
/// Supports ScoreTracker, FireScoreTracker, and FireEvacuationScoreTracker.
/// Updated with additional metrics for comprehensive BFP evaluation.
/// NOW INCLUDES: Categorized error tracking for ALL phases (storage, maintenance, disposal, fire tasks)
/// </summary>
public class ScoreDataManager : MonoBehaviour
{
    public static ScoreDataManager Instance { get; private set; }

    [System.Serializable]
    public class LevelScoreData
    {
        public string levelName;
        public string levelType; // "Standard", "Fire", or "Fire Evacuation"
        public int completedTasks;
        public int totalTasks;
        public int finalScore;
        public float completionPercentage;
        public string timestamp;
        
        // Standard level specific
        public int perfectScore;
        public int mistakeCount;
        public float accuracyPercentage;
        public int safetyViolations;
        
        // NEW: Categorized error tracking for Standard levels
        public int storageErrors;
        public int maintenanceErrors;
        public int disposalErrors;
        
        // Fire level specific
        public int errorCount;
        public float timeRemaining;
        
        // NEW: Fire phase task completion tracking
        public bool fireExtinguisherUsed;
        public bool fireAlarmPulled;
        public bool fireDoorClosed;
        public bool npcEvacuated;
        
        // NEW: Fire phase error type tracking
        public int wrongNPCResponseErrors;
        public int fireExtinguisherErrors;
        public int passMethodErrors;
        
        // Fire Evacuation specific
        public bool usedWetCloth;
        public bool rescuedNPC;
        public bool completedOnTime;
        public float evacuationTime;
    }

    private List<LevelScoreData> levelScores = new List<LevelScoreData>();
    private LevelScoreData currentLevelData;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("ScoreDataManager initialized and persisted");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        Debug.Log($"===== SCENE LOADED: {scene.name} =====");
        Debug.Log($"Levels completed: {levelScores.Count}");
        
        if (currentLevelData != null)
        {
            Debug.Log($"Last Level: {currentLevelData.levelName} ({currentLevelData.levelType})");
            Debug.Log($"Score: {currentLevelData.finalScore} | Tasks: {currentLevelData.completedTasks}/{currentLevelData.totalTasks}");
        }
        else
        {
            Debug.LogWarning("No level data saved yet");
        }
    }

    /// <summary>
    /// Save data from standard ScoreTracker levels (Phase 1)
    /// NOW INCLUDES: Categorized error tracking
    /// </summary>
    public void SaveCurrentLevelData()
    {
        if (ScoreTracker.Instance == null) return;

        string levelName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        currentLevelData = new LevelScoreData
        {
            levelName = levelName,
            levelType = "Standard",
            completedTasks = ScoreTracker.Instance.GetCompletedTasks(),
            totalTasks = ScoreTracker.Instance.GetTotalTasks(),
            finalScore = ScoreTracker.Instance.GetCurrentScore(),
            perfectScore = ScoreTracker.Instance.GetPerfectScore(),
            mistakeCount = ScoreTracker.Instance.GetMistakeCount(),
            safetyViolations = ScoreTracker.Instance.GetSafetyViolations(),
            completionPercentage = ScoreTracker.Instance.GetCompletionPercentage(),
            accuracyPercentage = ScoreTracker.Instance.GetAccuracyPercentage(),
            
            // NEW: Save categorized errors
            storageErrors = ScoreTracker.Instance.GetStorageErrors(),
            maintenanceErrors = ScoreTracker.Instance.GetMaintenanceErrors(),
            disposalErrors = ScoreTracker.Instance.GetDisposalErrors(),
            
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        levelScores.Add(currentLevelData);
        Debug.Log($"===== STANDARD LEVEL SAVED =====");
        Debug.Log($"Level: {levelName}");
        Debug.Log($"Score: {currentLevelData.finalScore}/{currentLevelData.perfectScore}");
        Debug.Log($"Tasks: {currentLevelData.completedTasks}/{currentLevelData.totalTasks}");
        Debug.Log($"Mistakes: {currentLevelData.mistakeCount}");
        Debug.Log($"Safety Violations: {currentLevelData.safetyViolations}");
        Debug.Log($"--- Error Breakdown ---");
        Debug.Log($"Storage Errors: {currentLevelData.storageErrors}");
        Debug.Log($"Maintenance Errors: {currentLevelData.maintenanceErrors}");
        Debug.Log($"Disposal Errors: {currentLevelData.disposalErrors}");
        Debug.Log($"Accuracy: {currentLevelData.accuracyPercentage:F1}%");
        Debug.Log($"================================");
    }

    /// <summary>
    /// Save data from FireScoreTracker levels (Phase 2)
    /// NOW INCLUDES: Task completion status and categorized error tracking
    /// </summary>
    public void SaveFireLevelData()
    {
        if (FireScoreTracker.Instance == null) return;

        string levelName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        FireTimer timer = FindObjectOfType<FireTimer>();
        
        currentLevelData = new LevelScoreData
        {
            levelName = levelName,
            levelType = "Fire",
            completedTasks = FireScoreTracker.Instance.GetCompletedTasks(),
            totalTasks = FireScoreTracker.Instance.GetTotalTasks(),
            finalScore = FireScoreTracker.Instance.GetCurrentScore(),
            errorCount = FireScoreTracker.Instance.GetErrorCount(),
            safetyViolations = FireScoreTracker.Instance.GetSafetyViolations(),
            completionPercentage = FireScoreTracker.Instance.GetCompletionPercentage(),
            timeRemaining = timer != null ? timer.GetTimeRemaining() : 0f,
            
            // NEW: Save task completion status
            fireExtinguisherUsed = FireScoreTracker.Instance.WasFireExtinguisherUsed(),
            fireAlarmPulled = FireScoreTracker.Instance.WasFireAlarmPulled(),
            fireDoorClosed = FireScoreTracker.Instance.WasFireDoorClosed(),
            npcEvacuated = FireScoreTracker.Instance.WasNPCEvacuated(),
            
            // NEW: Save categorized errors
            wrongNPCResponseErrors = FireScoreTracker.Instance.GetWrongNPCResponseErrors(),
            fireExtinguisherErrors = FireScoreTracker.Instance.GetFireExtinguisherErrors(),
            passMethodErrors = FireScoreTracker.Instance.GetPASSMethodErrors(),
            
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        levelScores.Add(currentLevelData);
        Debug.Log($"===== FIRE LEVEL SAVED =====");
        Debug.Log($"Level: {levelName}");
        Debug.Log($"Score: {currentLevelData.finalScore}");
        Debug.Log($"Tasks: {currentLevelData.completedTasks}/{currentLevelData.totalTasks}");
        Debug.Log($"Errors: {currentLevelData.errorCount}");
        Debug.Log($"Safety Violations: {currentLevelData.safetyViolations}");
        Debug.Log($"Time Remaining: {currentLevelData.timeRemaining:F1}s");
        Debug.Log($"--- Task Completion Status ---");
        Debug.Log($"Fire Extinguisher Used: {currentLevelData.fireExtinguisherUsed}");
        Debug.Log($"Fire Alarm Pulled: {currentLevelData.fireAlarmPulled}");
        Debug.Log($"Fire Door Closed: {currentLevelData.fireDoorClosed}");
        Debug.Log($"NPC Evacuated: {currentLevelData.npcEvacuated}");
        Debug.Log($"--- Error Breakdown ---");
        Debug.Log($"Wrong NPC Responses: {currentLevelData.wrongNPCResponseErrors}");
        Debug.Log($"Fire Extinguisher Errors: {currentLevelData.fireExtinguisherErrors}");
        Debug.Log($"PASS Method Errors: {currentLevelData.passMethodErrors}");
        Debug.Log($"============================");
    }

    /// <summary>
    /// Save data from FireEvacuationScoreTracker levels (Phase 3)
    /// </summary>
    public void SaveFireEvacuationData(FireEvacuationScoreTracker scoreTracker)
    {
        Debug.Log("===== SaveFireEvacuationData() CALLED =====");
        
        if (scoreTracker == null)
        {
            Debug.LogWarning("FireEvacuationScoreTracker is NULL!");
            return;
        }

        // Get scene name immediately
        string levelName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Debug.Log($"Saving evacuation data for scene: {levelName}");
        
        FireEvacuationTimer timer = FindObjectOfType<FireEvacuationTimer>();
        
        currentLevelData = new LevelScoreData
        {
            levelName = levelName,
            levelType = "Fire Evacuation",
            completedTasks = scoreTracker.CompletedOnTime() ? 1 : 0,
            totalTasks = 1,
            finalScore = scoreTracker.GetCurrentScore(),
            errorCount = scoreTracker.GetErrorCount(),
            safetyViolations = scoreTracker.GetSafetyViolations(),
            completionPercentage = scoreTracker.CompletedOnTime() ? 100f : 0f,
            timeRemaining = timer != null ? timer.GetTimeRemaining() : 0f,
            usedWetCloth = scoreTracker.HasUsedWetCloth(),
            rescuedNPC = scoreTracker.HasRescuedNPC(),
            completedOnTime = scoreTracker.CompletedOnTime(),
            evacuationTime = timer != null ? timer.GetElapsedTime() : 0f,
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        levelScores.Add(currentLevelData);
        
        Debug.Log($"===== FIRE EVACUATION LEVEL SAVED =====");
        Debug.Log($"Level: {levelName}");
        Debug.Log($"Final Score: {currentLevelData.finalScore}");
        Debug.Log($"Completed On Time: {currentLevelData.completedOnTime}");
        Debug.Log($"Used Wet Cloth: {currentLevelData.usedWetCloth}");
        Debug.Log($"Rescued NPC: {currentLevelData.rescuedNPC}");
        Debug.Log($"Safety Violations: {currentLevelData.safetyViolations}");
        Debug.Log($"Errors: {currentLevelData.errorCount}");
        Debug.Log($"Evacuation Time: {currentLevelData.evacuationTime:F1}s");
        Debug.Log($"Total levels saved: {levelScores.Count}");
        Debug.Log($"======================================");
    }

    // PUBLIC GETTERS
    public LevelScoreData GetCurrentLevelData() => currentLevelData;
    public List<LevelScoreData> GetAllLevelData() => levelScores;
    public int GetCompletedLevelCount() => levelScores.Count;
    
    public void ClearAllData()
    {
        levelScores.Clear();
        currentLevelData = null;
        Debug.Log("All score data cleared");
    }
    
    /// <summary>
    /// Get total score across all completed levels
    /// </summary>
    public int GetTotalScore()
    {
        int total = 0;
        foreach (var data in levelScores)
        {
            total += data.finalScore;
        }
        return total;
    }
    
    /// <summary>
    /// Get overall completion percentage across all levels
    /// </summary>
    public float GetOverallCompletionPercentage()
    {
        if (levelScores.Count == 0) return 0f;
        
        float totalPercentage = 0f;
        foreach (var data in levelScores)
        {
            totalPercentage += data.completionPercentage;
        }
        return totalPercentage / levelScores.Count;
    }
    
    /// <summary>
    /// Get total mistakes across all levels
    /// </summary>
    public int GetTotalMistakes()
    {
        int total = 0;
        foreach (var data in levelScores)
        {
            if (data.levelType == "Standard")
            {
                total += data.mistakeCount;
            }
            else
            {
                total += data.errorCount;
            }
        }
        return total;
    }
    
    /// <summary>
    /// Get total safety violations across all levels
    /// </summary>
    public int GetTotalSafetyViolations()
    {
        int total = 0;
        foreach (var data in levelScores)
        {
            total += data.safetyViolations;
        }
        return total;
    }
    
    // NEW: Categorized error getters across all STANDARD levels
    
    /// <summary>
    /// Get total storage errors across all standard levels
    /// </summary>
    public int GetTotalStorageErrors()
    {
        int total = 0;
        foreach (var data in levelScores)
        {
            if (data.levelType == "Standard")
            {
                total += data.storageErrors;
            }
        }
        return total;
    }
    
    /// <summary>
    /// Get total maintenance errors across all standard levels
    /// </summary>
    public int GetTotalMaintenanceErrors()
    {
        int total = 0;
        foreach (var data in levelScores)
        {
            if (data.levelType == "Standard")
            {
                total += data.maintenanceErrors;
            }
        }
        return total;
    }
    
    /// <summary>
    /// Get total disposal errors across all standard levels
    /// </summary>
    public int GetTotalDisposalErrors()
    {
        int total = 0;
        foreach (var data in levelScores)
        {
            if (data.levelType == "Standard")
            {
                total += data.disposalErrors;
            }
        }
        return total;
    }
    
    // NEW: Fire phase specific getters
    
    /// <summary>
    /// Get total wrong NPC response errors across all fire levels
    /// </summary>
    public int GetTotalWrongNPCResponseErrors()
    {
        int total = 0;
        foreach (var data in levelScores)
        {
            if (data.levelType == "Fire")
            {
                total += data.wrongNPCResponseErrors;
            }
        }
        return total;
    }
    
    /// <summary>
    /// Get total fire extinguisher errors across all fire levels
    /// </summary>
    public int GetTotalFireExtinguisherErrors()
    {
        int total = 0;
        foreach (var data in levelScores)
        {
            if (data.levelType == "Fire")
            {
                total += data.fireExtinguisherErrors;
            }
        }
        return total;
    }
    
    /// <summary>
    /// Get total PASS method errors across all fire levels
    /// </summary>
    public int GetTotalPASSMethodErrors()
    {
        int total = 0;
        foreach (var data in levelScores)
        {
            if (data.levelType == "Fire")
            {
                total += data.passMethodErrors;
            }
        }
        return total;
    }
    
    /// <summary>
    /// Get a comprehensive breakdown of all error types across all levels
    /// </summary>
    public Dictionary<string, int> GetComprehensiveErrorBreakdown()
    {
        return new Dictionary<string, int>
        {
            // Phase 1 errors
            { "Storage", GetTotalStorageErrors() },
            { "Maintenance", GetTotalMaintenanceErrors() },
            { "Disposal", GetTotalDisposalErrors() },
            
            // Phase 2 errors
            { "Wrong NPC Response", GetTotalWrongNPCResponseErrors() },
            { "Fire Extinguisher", GetTotalFireExtinguisherErrors() },
            { "PASS Method", GetTotalPASSMethodErrors() },
            
            // Overall
            { "Safety Violations", GetTotalSafetyViolations() }
        };
    }
}