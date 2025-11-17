using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Enhanced Fire training score tracker with detailed task-specific error tracking.
/// Tracks which specific tasks were completed/failed for better feedback.
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
    
    // NEW: Specific task completion tracking
    private bool fireExtinguisherUsed = false;
    private bool fireAlarmPulled = false;
    private bool fireDoorClosed = false;
    private bool npcEvacuated = false;
    
    // NEW: Specific error type tracking
    private int wrongNPCResponseErrors = 0;
    private int fireExtinguisherErrors = 0;
    private int passMethodErrors = 0;
    
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

    // NEW: Specific task completion tracking methods
    
    /// <summary>
    /// Call this when fire extinguisher task is completed
    /// </summary>
    public void OnFireExtinguisherCompleted()
    {
        fireExtinguisherUsed = true;
        Debug.Log("Fire extinguisher task marked as completed");
    }
    
    /// <summary>
    /// Call this when fire alarm is pulled
    /// </summary>
    public void OnFireAlarmPulled()
    {
        fireAlarmPulled = true;
        Debug.Log("Fire alarm task marked as completed");
    }
    
    /// <summary>
    /// Call this when fire door is closed
    /// </summary>
    public void OnFireDoorClosed()
    {
        fireDoorClosed = true;
        Debug.Log("Fire door task marked as completed");
    }
    
    /// <summary>
    /// Call this when NPC evacuation is completed
    /// </summary>
    public void OnNPCEvacuated()
    {
        npcEvacuated = true;
        Debug.Log("NPC evacuation task marked as completed");
    }
    
    // NEW: Specific error tracking methods
    
    /// <summary>
    /// Call this when player gives wrong NPC response
    /// </summary>
    public void OnWrongNPCResponse()
    {
        wrongNPCResponseErrors++;
        OnTaskError("Wrong NPC Response");
        Debug.Log($"Wrong NPC response error. Total NPC errors: {wrongNPCResponseErrors}");
    }
    
    /// <summary>
    /// Call this when player uses wrong fire extinguisher type
    /// </summary>
    public void OnWrongExtinguisherType()
    {
        fireExtinguisherErrors++;
        OnTaskError("Wrong Extinguisher Type");
        Debug.Log($"Wrong extinguisher type error. Total extinguisher errors: {fireExtinguisherErrors}");
    }
    
    /// <summary>
    /// Call this when player uses incorrect PASS method
    /// </summary>
    public void OnIncorrectPASSMethod()
    {
        passMethodErrors++;
        OnTaskError("Incorrect PASS Method");
        Debug.Log($"Incorrect PASS method error. Total PASS errors: {passMethodErrors}");
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
    /// Called when timer expires - track which specific tasks were incomplete
    /// </summary>
    private void OnTimerExpired()
    {
        Debug.Log("===== TIMER EXPIRED =====");
        Debug.Log("Checking which tasks were incomplete...");
        
        // Track which specific tasks were not completed
        if (!fireExtinguisherUsed)
        {
            Debug.Log("❌ Fire extinguisher was NOT used!");
            safetyViolations++;
        }
        
        if (!fireAlarmPulled)
        {
            Debug.Log("❌ Fire alarm was NOT pulled!");
            safetyViolations++;
        }
        
        if (!fireDoorClosed)
        {
            Debug.Log("❌ Fire door was NOT closed!");
            safetyViolations++;
        }
        
        if (!npcEvacuated)
        {
            Debug.Log("❌ NPC was NOT evacuated!");
            safetyViolations++;
        }
        
        // Apply penalties for incomplete tasks
        int incompleteTasks = totalTasks - completedTasks;
        int timeExpiredPenalty = incompleteTasks * safetyViolationPenalty;
        
        currentScore += timeExpiredPenalty;
        currentScore = Mathf.Max(0, currentScore);
        
        Debug.Log($"Time expired penalties: {timeExpiredPenalty} points for {incompleteTasks} incomplete tasks");
        Debug.Log($"Total safety violations: {safetyViolations}");
        
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
            
            // Automatically save fire level data when level is complete
            if (ScoreDataManager.Instance != null)
            {
                ScoreDataManager.Instance.SaveFireLevelData();
            }
            
            // Log detailed results
            Debug.Log($"===== FIRE TRAINING COMPLETE =====");
            Debug.Log($"Final Score: {currentScore}");
            Debug.Log($"Tasks: {completedTasks}/{totalTasks}");
            Debug.Log($"Total Errors: {errorCount}");
            Debug.Log($"Safety Violations: {safetyViolations}");
            Debug.Log($"--- Task Completion Status ---");
            Debug.Log($"Fire Extinguisher Used: {fireExtinguisherUsed}");
            Debug.Log($"Fire Alarm Pulled: {fireAlarmPulled}");
            Debug.Log($"Fire Door Closed: {fireDoorClosed}");
            Debug.Log($"NPC Evacuated: {npcEvacuated}");
            Debug.Log($"--- Error Breakdown ---");
            Debug.Log($"Wrong NPC Responses: {wrongNPCResponseErrors}");
            Debug.Log($"Fire Extinguisher Errors: {fireExtinguisherErrors}");
            Debug.Log($"PASS Method Errors: {passMethodErrors}");
            Debug.Log($"==================================");
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
    
    // NEW: Getters for specific task completion status
    public bool WasFireExtinguisherUsed() => fireExtinguisherUsed;
    public bool WasFireAlarmPulled() => fireAlarmPulled;
    public bool WasFireDoorClosed() => fireDoorClosed;
    public bool WasNPCEvacuated() => npcEvacuated;
    
    // NEW: Getters for specific error counts
    public int GetWrongNPCResponseErrors() => wrongNPCResponseErrors;
    public int GetFireExtinguisherErrors() => fireExtinguisherErrors;
    public int GetPASSMethodErrors() => passMethodErrors;
    
    /// <summary>
    /// Get a breakdown of all incomplete tasks
    /// </summary>
    public Dictionary<string, bool> GetTaskCompletionBreakdown()
    {
        return new Dictionary<string, bool>
        {
            { "Fire Extinguisher", fireExtinguisherUsed },
            { "Fire Alarm", fireAlarmPulled },
            { "Fire Door", fireDoorClosed },
            { "NPC Evacuation", npcEvacuated }
        };
    }
    
    /// <summary>
    /// Get a breakdown of all error types
    /// </summary>
    public Dictionary<string, int> GetErrorBreakdown()
    {
        return new Dictionary<string, int>
        {
            { "Wrong NPC Response", wrongNPCResponseErrors },
            { "Fire Extinguisher Errors", fireExtinguisherErrors },
            { "PASS Method Errors", passMethodErrors },
            { "Safety Violations", safetyViolations }
        };
    }

    void OnDestroy()
    {
        // Unsubscribe from timer events
        if (fireTimer != null)
        {
            fireTimer.OnTimerExpired -= OnTimerExpired;
        }
    }
}