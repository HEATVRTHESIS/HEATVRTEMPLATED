using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Persistent data manager that stores score data between scenes.
/// Supports both ScoreTracker and FireScoreTracker.
/// </summary>
public class ScoreDataManager : MonoBehaviour
{
    public static ScoreDataManager Instance { get; private set; }

    [System.Serializable]
    public class LevelScoreData
    {
        public string levelName;
        public string levelType; // "Standard" or "Fire"
        public int completedTasks;
        public int totalTasks;
        public int finalScore;
        public float completionPercentage;
        public string timestamp;
        
        // Standard level specific
        public int perfectScore;
        public int mistakeCount;
        public float accuracyPercentage;
        
        // Fire level specific
        public int errorCount;
        public int safetyViolations;
        public float timeRemaining;
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

    public void SaveCurrentLevelData()
    {
        if (ScoreTracker.Instance == null) return;

        string levelName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        currentLevelData = new LevelScoreData
        {
            levelName = levelName,
            levelType = "Standard",
            completedTasks = ScoreTracker.Instance.GetCurrentScore() / 10,
            totalTasks = ScoreTracker.Instance.GetPerfectScore() / 10,
            finalScore = ScoreTracker.Instance.GetCurrentScore(),
            perfectScore = ScoreTracker.Instance.GetPerfectScore(),
            mistakeCount = ScoreTracker.Instance.GetMistakeCount(),
            completionPercentage = ScoreTracker.Instance.GetCompletionPercentage(),
            accuracyPercentage = ScoreTracker.Instance.GetAccuracyPercentage(),
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        levelScores.Add(currentLevelData);
        Debug.Log($"Standard level saved: {levelName}");
    }

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
        Debug.Log($"Fire level saved: {levelName}");
    }

    public LevelScoreData GetCurrentLevelData() => currentLevelData;
    public List<LevelScoreData> GetAllLevelData() => levelScores;
    public int GetCompletedLevelCount() => levelScores.Count;
    
    public void ClearAllData()
    {
        levelScores.Clear();
        currentLevelData = null;
    }
}