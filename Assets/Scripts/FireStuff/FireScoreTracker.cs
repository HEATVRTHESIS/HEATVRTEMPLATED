using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Fire training score tracker focusing on Task Accuracy, Speed & Efficiency, and Safety Compliance.
/// Tracks fire suppression tasks with specialized metrics for extinguisher usage and safety protocols.
/// </summary>
public class FireScoreTracker : MonoBehaviour
{
    // Singleton pattern
    public static FireScoreTracker Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI taskCountText;
    public TextMeshProUGUI scoreText;
    
    [Header("Results Screen")]
    public GameObject resultsCanvas;
    public TextMeshProUGUI resultsTimeLeftText;
    public TextMeshProUGUI resultsTasksCompletedText;
    public TextMeshProUGUI resultsErrorsText;
    public TextMeshProUGUI resultsFinalScoreText;
    
    [Header("Timer Reference")]
    public FireTimer fireTimer;
    
    [Header("Scoring Settings")]
    [Header("Task Accuracy")]
    [SerializeField] private int correctActionPoints = 10;      // +10 for correct extinguisher, PASS, hazard ID
    [SerializeField] private int errorPenalty = -5;            // -5 for task errors
    
    [Header("Speed & Efficiency")]
    [SerializeField] private int speedBonusPerSecond = 1;      // +1 pt/s under target
    [SerializeField] private float speedPenaltyPerSecond = -0.5f; // -0.5 pt/s over target
    [SerializeField] private float smallFireTarget = 10f;      // Target time for small fires (seconds)
    [SerializeField] private float mediumFireTarget = 20f;     // Target time for medium fires (seconds)
    
    [Header("Safety Compliance")]
    [SerializeField] private int safetyCompliancePoints = 15;  // +15 for alarm, door closure
    [SerializeField] private int safetyViolationPenalty = -10; // -10 for safety violations
    
    // Score tracking
    private int currentScore = 0;
    private int completedTasks = 0;
    private int totalTasks = 0;
    private int errorCount = 0;
    private int safetyViolations = 0;
    
    // Task timing tracking
    private Dictionary<CustomTaskController, float> taskStartTimes = new Dictionary<CustomTaskController, float>();
    private Dictionary<CustomTaskController, FireSize> taskFireSizes = new Dictionary<CustomTaskController, FireSize>();
    
    // Fire size enumeration
    public enum FireSize
    {
        Small,      // Target ≤10s
        Medium      // Target ≤20s
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Find and register all custom tasks
        FindAndRegisterAllTasks();
        
        // Initialize UI
        UpdateScoreDisplay();
        
        // Ensure results canvas is hidden
        if (resultsCanvas != null)
        {
            resultsCanvas.SetActive(false);
        }
        
        // Find timer if not assigned
        if (fireTimer == null)
        {
            fireTimer = FindObjectOfType<FireTimer>();
        }
        
        // Subscribe to timer events
        if (fireTimer != null)
        {
            fireTimer.OnTimerExpired += OnTimerExpired;
        }
        
        Debug.Log($"FireScoreTracker initialized. Found {totalTasks} fire suppression tasks.");
    }

    /// <summary>
    /// Find and register all fire-related tasks
    /// </summary>
    private void FindAndRegisterAllTasks()
    {
        // Register custom tasks (Fire Extinguisher Controllers, PullDownTriggers, etc.)
        CustomTaskController[] customTasks = FindObjectsOfType<CustomTaskController>();
        foreach (var task in customTasks)
        {
            RegisterTask(task);
        }
        
        Debug.Log($"Registered {totalTasks} fire suppression tasks");
    }

    /// <summary>
    /// Register a task for scoring
    /// </summary>
    private void RegisterTask(CustomTaskController task)
    {
        totalTasks++;
        
        // Subscribe to task completion event
        task.OnTaskCompleted.AddListener(() => OnTaskCompleted(task));
        
        // Set default fire size (can be overridden)
        taskFireSizes[task] = FireSize.Small;
        
        Debug.Log($"Registered task: {task.taskName}");
    }

    /// <summary>
    /// Start timing a specific task
    /// </summary>
    public void StartTaskTimer(CustomTaskController task, FireSize fireSize = FireSize.Small)
    {
        if (task != null)
        {
            taskStartTimes[task] = Time.time;
            taskFireSizes[task] = fireSize;
            Debug.Log($"Started timer for {task.taskName} ({fireSize} fire)");
        }
    }

    /// <summary>
    /// Called when a task is completed
    /// </summary>
    private void OnTaskCompleted(CustomTaskController task)
    {
        completedTasks++;
        
        // Calculate speed bonus/penalty
        float completionTime = CalculateTaskTime(task);
        int speedScore = CalculateSpeedScore(task, completionTime);
        
        // Base completion points
        int taskScore = correctActionPoints + speedScore;
        currentScore += taskScore;
        
        Debug.Log($"Task '{task.taskName}' completed in {completionTime:F1}s. " +
                 $"Speed score: {speedScore}, Total task score: {taskScore}");
        
        UpdateScoreDisplay();
        
        // Check if all tasks completed
        CheckForCompletion();
    }

    /// <summary>
    /// Calculate how long a task took to complete
    /// </summary>
    private float CalculateTaskTime(CustomTaskController task)
    {
        if (taskStartTimes.ContainsKey(task))
        {
            return Time.time - taskStartTimes[task];
        }
        return 0f;
    }

    /// <summary>
    /// Calculate speed score based on completion time
    /// </summary>
    private int CalculateSpeedScore(CustomTaskController task, float completionTime)
    {
        FireSize fireSize = taskFireSizes.ContainsKey(task) ? taskFireSizes[task] : FireSize.Small;
        float targetTime = fireSize == FireSize.Small ? smallFireTarget : mediumFireTarget;
        
        float timeDifference = completionTime - targetTime;
        
        if (timeDifference <= 0)
        {
            // Under or at target time - bonus points
            return Mathf.RoundToInt(Mathf.Abs(timeDifference) * speedBonusPerSecond);
        }
        else
        {
            // Over target time - penalty points
            return Mathf.RoundToInt(timeDifference * speedPenaltyPerSecond);
        }
    }

    /// <summary>
    /// Award points for correct actions (extinguisher choice, PASS technique, hazard ID)
    /// </summary>
    public void OnCorrectAction(string actionType)
    {
        currentScore += correctActionPoints;
        Debug.Log($"Correct action: {actionType}. +{correctActionPoints} points. Score: {currentScore}");
        UpdateScoreDisplay();
    }

    /// <summary>
    /// Deduct points for task errors
    /// </summary>
    public void OnTaskError(string errorType)
    {
        errorCount++;
        currentScore += errorPenalty; // errorPenalty is already negative
        currentScore = Mathf.Max(0, currentScore); // Don't go below 0
        
        Debug.Log($"Task error: {errorType}. {errorPenalty} points. Errors: {errorCount}, Score: {currentScore}");
        UpdateScoreDisplay();
    }

    /// <summary>
    /// Award points for safety compliance (alarm activation, door closure)
    /// </summary>
    public void OnSafetyCompliance(string safetyAction)
    {
        currentScore += safetyCompliancePoints;
        Debug.Log($"Safety compliance: {safetyAction}. +{safetyCompliancePoints} points. Score: {currentScore}");
        UpdateScoreDisplay();
    }

    /// <summary>
    /// Penalize safety violations
    /// </summary>
    public void OnSafetyViolation(string violationType)
    {
        safetyViolations++;
        currentScore += safetyViolationPenalty; // safetyViolationPenalty is already negative
        currentScore = Mathf.Max(0, currentScore); // Don't go below 0
        
        Debug.Log($"Safety violation: {violationType}. {safetyViolationPenalty} points. " +
                 $"Violations: {safetyViolations}, Score: {currentScore}");
        UpdateScoreDisplay();
    }

    /// <summary>
    /// Called when timer expires - penalize incomplete critical tasks
    /// </summary>
    private void OnTimerExpired()
    {
        Debug.Log("Timer expired! Applying penalties for incomplete critical tasks.");
        
        // Apply -10 penalty for each incomplete task (representing critical safety violations)
        int incompleteTasks = totalTasks - completedTasks;
        int timeExpiredPenalty = incompleteTasks * safetyViolationPenalty;
        
        currentScore += timeExpiredPenalty;
        safetyViolations += incompleteTasks;
        currentScore = Mathf.Max(0, currentScore);
        
        Debug.Log($"Time expired penalties: {timeExpiredPenalty} points for {incompleteTasks} incomplete tasks");
        
        ShowResultsScreen();
    }

    /// <summary>
    /// Check if all tasks are completed
    /// </summary>
    private void CheckForCompletion()
    {
        if (completedTasks >= totalTasks)
        {
            Debug.Log("All fire suppression tasks completed!");
            ShowResultsScreen();
        }
    }

    /// <summary>
    /// Show the results screen
    /// </summary>
    private void ShowResultsScreen()
    {
        if (resultsCanvas != null)
        {
            // Get time left from timer
            float timeLeft = fireTimer != null ? fireTimer.GetTimeRemaining() : 0f;
            
            // Update results UI
            if (resultsTimeLeftText != null)
            {
                resultsTimeLeftText.text = $"Time Left: {FormatTime(timeLeft)}";
            }
            
            if (resultsTasksCompletedText != null)
            {
                resultsTasksCompletedText.text = $"Tasks Completed: {completedTasks}/{totalTasks}";
            }
            
            if (resultsErrorsText != null)
            {
                resultsErrorsText.text = $"Errors: {errorCount + safetyViolations}";
            }
            
            if (resultsFinalScoreText != null)
            {
                resultsFinalScoreText.text = $"Final Score: {currentScore}";
            }
            
            // Show results canvas
            resultsCanvas.SetActive(true);
            
            // Pause the timer
            if (fireTimer != null)
            {
                fireTimer.PauseTimer();
            }
            
            Debug.Log($"Fire Training Complete! Final Score: {currentScore}, " +
                     $"Tasks: {completedTasks}/{totalTasks}, Errors: {errorCount + safetyViolations}");
        }
    }

    /// <summary>
    /// Update the score display UI
    /// </summary>
    private void UpdateScoreDisplay()
    {
        if (taskCountText != null)
        {
            taskCountText.text = $"Tasks: {completedTasks}/{totalTasks}";
        }
        
        if (scoreText != null)
        {
            scoreText.text = $"Score: {currentScore}";
        }
    }

    /// <summary>
    /// Format time as MM:SS
    /// </summary>
    private string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{minutes:00}:{secs:00}";
    }

    // Public getters for other systems
    public int GetCurrentScore() => currentScore;
    public int GetCompletedTasks() => completedTasks;
    public int GetTotalTasks() => totalTasks;
    public int GetErrorCount() => errorCount;
    public int GetSafetyViolations() => safetyViolations;
    public float GetCompletionPercentage() => totalTasks > 0 ? (float)completedTasks / totalTasks * 100f : 0f;

    void OnDestroy()
    {
        // Unsubscribe from timer events
        if (fireTimer != null)
        {
            fireTimer.OnTimerExpired -= OnTimerExpired;
        }
    }
}