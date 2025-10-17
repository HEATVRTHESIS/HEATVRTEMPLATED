using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Enhanced persistent data manager that stores score data between scenes.
/// Supports ScoreTracker, FireScoreTracker, and FireEvacuationScoreTracker.
/// Updated with additional metrics for comprehensive BFP evaluation.
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
        public int safetyViolations; // Added for Phase 1
        
        // Fire level specific
        public int errorCount;
        public float timeRemaining;
        
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
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        levelScores.Add(currentLevelData);
        Debug.Log($"===== STANDARD LEVEL SAVED =====");
        Debug.Log($"Level: {levelName}");
        Debug.Log($"Score: {currentLevelData.finalScore}/{currentLevelData.perfectScore}");
        Debug.Log($"Tasks: {currentLevelData.completedTasks}/{currentLevelData.totalTasks}");
        Debug.Log($"Mistakes: {currentLevelData.mistakeCount}");
        Debug.Log($"Safety Violations: {currentLevelData.safetyViolations}");
        Debug.Log($"Accuracy: {currentLevelData.accuracyPercentage:F1}%");
        Debug.Log($"================================");
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
            errorCount = FireScoreTracker.Instance.GetErrorCount(),
            safetyViolations = FireScoreTracker.Instance.GetSafetyViolations(),
            completionPercentage = FireScoreTracker.Instance.GetCompletionPercentage(),
            timeRemaining = timer != null ? timer.GetTimeRemaining() : 0f,
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
}