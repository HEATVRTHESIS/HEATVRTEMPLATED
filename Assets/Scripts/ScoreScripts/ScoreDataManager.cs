using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Enhanced persistent data manager that stores score data between scenes.
/// Now uses ErrorTracker for accurate error counting.
/// </summary>
public class ScoreDataManager : MonoBehaviour
{
    public static ScoreDataManager Instance { get; private set; }

    [System.Serializable]
    public class LevelScoreData
    {
        public string levelName;
        public string levelType;
        public int completedTasks;
        public int totalTasks;
        public int finalScore;
        public int perfectScore;
        public float completionPercentage;
        public float accuracyPercentage;
        public string timestamp;
        
        // Phase 1 errors (from ErrorTracker)
        public int storageErrors;
        public int maintenanceErrors;
        public int disposalErrors;
        
        // Fire phase errors (from ErrorTracker)
        public int fireNPCErrors;
        public int fireLeverErrors;
        public int fireSmokeDoorErrors;
        public int fireExtinguisherErrors;
        public int fireWrongExtinguisherErrors;
        
        // Fire evacuation errors (from ErrorTracker)
        public int evacuationTimeExpiredErrors;
        public int evacuationFireObstacleErrors;
        public int evacuationOxygenErrors;
        public int evacuationNPCLeftBehindErrors;
        public int evacuationNPCNotRescuedErrors;
        public int evacuationNoWetClothErrors;
        
        // Fire level specific
        public float timeRemaining;
        public bool fireExtinguisherUsed;
        public bool fireAlarmPulled;
        public bool fireDoorClosed;
        public bool npcEvacuated;
        
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
    }

    /// <summary>
    /// Save data from standard ScoreTracker levels (Phase 1)
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
            completionPercentage = ScoreTracker.Instance.GetCompletionPercentage(),
            accuracyPercentage = ScoreTracker.Instance.GetAccuracyPercentage(),
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        // Pull errors from ErrorTracker
        if (ErrorTracker.Instance != null)
        {
            currentLevelData.storageErrors = ErrorTracker.Instance.storageErrors;
            currentLevelData.maintenanceErrors = ErrorTracker.Instance.maintenanceErrors;
            currentLevelData.disposalErrors = ErrorTracker.Instance.disposalErrors;
        }

        levelScores.Add(currentLevelData);
        
        Debug.Log($"===== STANDARD LEVEL SAVED =====");
        Debug.Log($"Score: {currentLevelData.finalScore}/{currentLevelData.perfectScore}");
        Debug.Log($"Tasks: {currentLevelData.completedTasks}/{currentLevelData.totalTasks}");
        Debug.Log($"Storage Errors: {currentLevelData.storageErrors}");
        Debug.Log($"Maintenance Errors: {currentLevelData.maintenanceErrors}");
        Debug.Log($"Disposal Errors: {currentLevelData.disposalErrors}");
    }

    /// <summary>
    /// Save data from FireScoreTracker levels (Phase 2)
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
            perfectScore = FireScoreTracker.Instance.GetTotalTasks() * 10,
            completionPercentage = FireScoreTracker.Instance.GetCompletionPercentage(),
            accuracyPercentage = FireScoreTracker.Instance.GetTotalTasks() > 0
                ? (float)FireScoreTracker.Instance.GetCurrentScore() / (FireScoreTracker.Instance.GetTotalTasks() * 10) * 100f
                : 0f,
            timeRemaining = timer != null ? timer.GetTimeRemaining() : 0f,
            fireExtinguisherUsed = FireScoreTracker.Instance.WasFireExtinguisherUsed(),
            fireAlarmPulled = FireScoreTracker.Instance.WasFireAlarmPulled(),
            fireDoorClosed = FireScoreTracker.Instance.WasFireDoorClosed(),
            npcEvacuated = FireScoreTracker.Instance.WasNPCEvacuated(),
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        // Pull errors from ErrorTracker
        if (ErrorTracker.Instance != null)
        {
            currentLevelData.fireNPCErrors = ErrorTracker.Instance.fireNPCErrors;
            currentLevelData.fireLeverErrors = ErrorTracker.Instance.fireLeverErrors;
            currentLevelData.fireSmokeDoorErrors = ErrorTracker.Instance.fireSmokeDoorErrors;
            currentLevelData.fireExtinguisherErrors = ErrorTracker.Instance.fireExtinguisherErrors;
            currentLevelData.fireWrongExtinguisherErrors = ErrorTracker.Instance.fireWrongExtinguisherErrors;
        }

        levelScores.Add(currentLevelData);
        Debug.Log($"===== FIRE LEVEL SAVED =====");
    }

    /// <summary>
    /// Save data from FireEvacuationScoreTracker levels (Phase 3)
    /// </summary>
    public void SaveFireEvacuationData(FireEvacuationScoreTracker scoreTracker)
    {
        if (scoreTracker == null) return;

        string levelName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        FireEvacuationTimer timer = FindObjectOfType<FireEvacuationTimer>();
        
        currentLevelData = new LevelScoreData
        {
            levelName = levelName,
            levelType = "Fire Evacuation",
            completedTasks = scoreTracker.GetCompletedTasks(),
            totalTasks = scoreTracker.GetTotalTasks(),
            finalScore = scoreTracker.GetCurrentScore(),
            perfectScore = scoreTracker.GetTotalTasks() * 15,
            completionPercentage = scoreTracker.GetCompletionPercentage(),
            accuracyPercentage = scoreTracker.GetTotalTasks() > 0
                ? (float)scoreTracker.GetCurrentScore() / (scoreTracker.GetTotalTasks() * 15) * 100f
                : 0f,
            timeRemaining = timer != null ? timer.GetTimeRemaining() : 0f,
            usedWetCloth = scoreTracker.HasUsedWetCloth(),
            rescuedNPC = scoreTracker.HasRescuedNPC(),
            completedOnTime = scoreTracker.CompletedOnTime(),
            evacuationTime = timer != null ? timer.GetElapsedTime() : 0f,
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        // Pull errors from ErrorTracker
        if (ErrorTracker.Instance != null)
        {
            currentLevelData.evacuationTimeExpiredErrors = ErrorTracker.Instance.evacuationTimeExpiredErrors;
            currentLevelData.evacuationFireObstacleErrors = ErrorTracker.Instance.evacuationFireObstacleErrors;
            currentLevelData.evacuationOxygenErrors = ErrorTracker.Instance.evacuationOxygenErrors;
            currentLevelData.evacuationNPCLeftBehindErrors = ErrorTracker.Instance.evacuationNPCLeftBehindErrors;
            currentLevelData.evacuationNPCNotRescuedErrors = ErrorTracker.Instance.evacuationNPCNotRescuedErrors;
            currentLevelData.evacuationNoWetClothErrors = ErrorTracker.Instance.evacuationNoWetClothErrors;
        }

        levelScores.Add(currentLevelData);
        Debug.Log($"===== FIRE EVACUATION LEVEL SAVED =====");
    }

    // PUBLIC GETTERS
    public LevelScoreData GetCurrentLevelData() => currentLevelData;
    public List<LevelScoreData> GetAllLevelData() => levelScores;
    public int GetCompletedLevelCount() => levelScores.Count;
    
    public void ClearAllData()
    {
        levelScores.Clear();
        currentLevelData = null;
    }

    public int ClearLevelDataByType(string levelType)
    {
        if (string.IsNullOrEmpty(levelType)) return 0;

        int removedCount = 0;
        for (int i = levelScores.Count - 1; i >= 0; i--)
        {
            if (levelScores[i].levelType == levelType)
            {
                levelScores.RemoveAt(i);
                removedCount++;
            }
        }

        if (currentLevelData != null && currentLevelData.levelType == levelType)
        {
            currentLevelData = null;
        }

        if (removedCount > 0)
        {
            Debug.Log($"Cleared {removedCount} saved level entries for type '{levelType}'");
        }

        return removedCount;
    }
    
    public int GetTotalScore()
    {
        int total = 0;
        foreach (var data in levelScores)
        {
            total += data.finalScore;
        }
        return total;
    }

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

    public int GetTotalMistakes()
    {
        int total = 0;
        foreach (var data in levelScores)
        {
            if (data.levelType == "Standard")
            {
                total += data.storageErrors + data.maintenanceErrors + data.disposalErrors;
            }
            else if (data.levelType == "Fire")
            {
                total += data.fireNPCErrors + data.fireLeverErrors + data.fireSmokeDoorErrors + 
                         data.fireExtinguisherErrors + data.fireWrongExtinguisherErrors;
            }
            else if (data.levelType == "Fire Evacuation")
            {
                total += data.evacuationTimeExpiredErrors + data.evacuationFireObstacleErrors + 
                         data.evacuationOxygenErrors + data.evacuationNPCLeftBehindErrors + 
                         data.evacuationNPCNotRescuedErrors + data.evacuationNoWetClothErrors;
            }
        }
        return total;
    }

    // Categorized error getters for Phase 1
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

    // Fire phase error getters
    public int GetTotalFireNPCErrors()
    {
        int total = 0;
        foreach (var data in levelScores)
        {
            if (data.levelType == "Fire")
            {
                total += data.fireNPCErrors;
            }
        }
        return total;
    }

    public int GetTotalFireLeverErrors()
    {
        int total = 0;
        foreach (var data in levelScores)
        {
            if (data.levelType == "Fire")
            {
                total += data.fireLeverErrors;
            }
        }
        return total;
    }

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

    // Fire evacuation error getters
    public int GetTotalEvacuationErrors()
    {
        int total = 0;
        foreach (var data in levelScores)
        {
            if (data.levelType == "Fire Evacuation")
            {
                total += data.evacuationTimeExpiredErrors + data.evacuationFireObstacleErrors + 
                         data.evacuationOxygenErrors + data.evacuationNPCLeftBehindErrors + 
                         data.evacuationNPCNotRescuedErrors + data.evacuationNoWetClothErrors;
            }
        }
        return total;
    }

    public Dictionary<string, int> GetComprehensiveErrorBreakdown()
    {
        return new Dictionary<string, int>
        {
            // Phase 1 errors
            { "Storage", GetTotalStorageErrors() },
            { "Maintenance", GetTotalMaintenanceErrors() },
            { "Disposal", GetTotalDisposalErrors() },
            
            // Phase 2 errors
            { "Fire NPC", GetTotalFireNPCErrors() },
            { "Fire Lever", GetTotalFireLeverErrors() },
            { "Fire Extinguisher", GetTotalFireExtinguisherErrors() },
            
            // Phase 3 errors
            { "Evacuation", GetTotalEvacuationErrors() }
        };
    }
    public void ClearAllLevelData()
{
    levelScores.Clear();
    Debug.Log("Cleared all level data");
}
}