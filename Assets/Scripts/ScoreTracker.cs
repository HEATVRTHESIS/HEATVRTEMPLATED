using UnityEngine;
using TMPro; // Required for TextMeshPro

/// <summary>
/// Enhanced centralized score tracking system for Phase 1: Risk Identification and Mitigation
/// Focus: Task Accuracy & Safety Compliance
/// +10 points for correct actions, -5 points for errors
/// Now includes detailed categorization for BFP evaluation metrics
/// </summary>
public class ScoreTracker : MonoBehaviour
{
    // Singleton pattern
    public static ScoreTracker Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI scoreValueText; // New: separate text object for the numerical score
    
    [Header("Summary Screen")]
    public GameObject summaryCanvas; // Canvas to show when level is complete
    public TextMeshProUGUI summaryCompletedTasksText; // Shows completed tasks count
    public TextMeshProUGUI summaryMistakesText; // Shows mistakes count
    public TextMeshProUGUI summaryAccuracyText; // Shows accuracy percentage

    [Header("Scoring Settings")]
    [SerializeField] private int pointsPerCompletion = 10;
    [SerializeField] private int pointsPerError = -5;
    [SerializeField] private int safetyViolationPenalty = -10;

    // The score counters
    private int completedTasks = 0;
    private int totalTasks = 0;
    private int currentScore = 0;
    private int perfectScore = 0; // Will be calculated as totalTasks * pointsPerCompletion
    private int mistakeCount = 0; // Track number of mistakes made
    private int safetyViolations = 0; // Track safety protocol violations
    
    // Enhanced tracking for BFP metrics
    private int storageTasksCompleted = 0;
    private int maintenanceTasksCompleted = 0;
    private int disposalTasksCompleted = 0;
    private int storageErrors = 0;
    private int maintenanceErrors = 0;
    private int disposalErrors = 0;

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
        // Find and register all tasks at the start of the game.
        FindAndRegisterAllTasks();

        // Calculate perfect score
        perfectScore = totalTasks * pointsPerCompletion;

        // Initialize the score display
        UpdateScoreDisplay();
        
        // Ensure summary canvas is hidden at start
        if (summaryCanvas != null)
        {
            summaryCanvas.SetActive(false);
        }
        
        Debug.Log($"ScoreTracker initialized. Total tasks: {totalTasks}, Perfect score: {perfectScore}");
    }

    /// <summary>
    /// Finds all task controllers in the scene and registers them.
    /// </summary>
    private void FindAndRegisterAllTasks()
    {
        // Find all standard tasks
        TaskController[] standardTasks = FindObjectsOfType<TaskController>();
        foreach (var task in standardTasks)
        {
            RegisterTask(task.OnTaskCompleted);
        }

        // Find all maintenance tasks
        MaintenanceTaskController[] maintenanceTasks = FindObjectsOfType<MaintenanceTaskController>();
        foreach (var task in maintenanceTasks)
        {
            RegisterTask(task.OnTaskCompleted);
        }

        // Find all storage tasks
        StorageTaskController[] storageTasks = FindObjectsOfType<StorageTaskController>();
        foreach (var task in storageTasks)
        {
            RegisterTask(task.OnTaskCompleted);
        }
    }

    /// <summary>
    /// Registers a single task's completion event to the score tracker.
    /// </summary>
    private void RegisterTask(UnityEngine.Events.UnityEvent completionEvent)
    {
        totalTasks++;
        completionEvent.AddListener(OnTaskCompleted);
    }

    /// <summary>
    /// This method is called whenever any registered task is completed.
    /// Adds +10 points for task completion.
    /// </summary>
    private void OnTaskCompleted()
    {
        completedTasks++;
        currentScore += pointsPerCompletion;
        
        Debug.Log($"Task completed! +{pointsPerCompletion} points. Current score: {currentScore} ({completedTasks}/{totalTasks} tasks)");
        UpdateScoreDisplay();
        
        // Check if all tasks are completed
        if (completedTasks >= totalTasks)
        {
            ShowSummaryScreen();
        }
    }
    
    /// <summary>
    /// Track storage task completion
    /// </summary>
    public void OnStorageTaskCompleted()
    {
        storageTasksCompleted++;
        OnTaskCompleted();
    }
    
    /// <summary>
    /// Track maintenance task completion
    /// </summary>
    public void OnMaintenanceTaskCompleted()
    {
        maintenanceTasksCompleted++;
        OnTaskCompleted();
    }
    
    /// <summary>
    /// Track disposal task completion
    /// </summary>
    public void OnDisposalTaskCompleted()
    {
        disposalTasksCompleted++;
        OnTaskCompleted();
    }

    /// <summary>
    /// Public method to be called by other scripts when an error occurs.
    /// Deducts -5 points for mistakes.
    /// </summary>
    public void OnTaskError()
    {
        mistakeCount++; // Increment mistake counter
        currentScore += pointsPerError; // pointsPerError is already negative (-5)
        
        // Ensure score doesn't go below 0
        currentScore = Mathf.Max(0, currentScore);
        
        Debug.Log($"Task error! {pointsPerError} points. Mistakes: {mistakeCount}. Current score: {currentScore}");
        UpdateScoreDisplay();
    }
    
    /// <summary>
    /// Track storage-specific errors
    /// </summary>
    public void OnStorageError()
    {
        storageErrors++;
        OnTaskError();
    }
    
    /// <summary>
    /// Track maintenance-specific errors
    /// </summary>
    public void OnMaintenanceError()
    {
        maintenanceErrors++;
        OnTaskError();
    }
    
    /// <summary>
    /// Track disposal-specific errors
    /// </summary>
    public void OnDisposalError()
    {
        disposalErrors++;
        OnTaskError();
    }
    
    /// <summary>
    /// Called when a safety protocol is violated
    /// </summary>
    public void OnSafetyViolation(string violationType)
    {
        safetyViolations++;
        currentScore += safetyViolationPenalty;
        currentScore = Mathf.Max(0, currentScore);
        
        Debug.Log($"Safety violation: {violationType}. {safetyViolationPenalty} points. Total violations: {safetyViolations}");
        UpdateScoreDisplay();
    }

    /// <summary>
    /// Updates the UI text to display the current score and progress.
    /// </summary>
    private void UpdateScoreDisplay()
    {
        // Update the main score text (tasks completed)
        if (scoreText != null)
        {
            scoreText.text = $"Tasks: {completedTasks} / {totalTasks}";
        }

        // Update the numerical score display
        if (scoreValueText != null)
        {
            scoreValueText.text = $"Score: {currentScore}";
        }
    }

    /// <summary>
    /// Shows the summary screen when all tasks are completed.
    /// </summary>
    private void ShowSummaryScreen()
    {
        if (summaryCanvas != null)
        {
            // Update summary text elements
            if (summaryCompletedTasksText != null)
            {
                summaryCompletedTasksText.text = $"Tasks Completed: {completedTasks}/{totalTasks}";
            }
            
            if (summaryMistakesText != null)
            {
                summaryMistakesText.text = $"Mistakes: {mistakeCount}";
            }
            
            if (summaryAccuracyText != null)
            {
                float accuracy = GetAccuracyPercentage();
                summaryAccuracyText.text = $"Accuracy: {accuracy:F1}%";
            }
            
            // Show the summary canvas
            summaryCanvas.SetActive(true);
            
            // Automatically save score data when level is complete
            if (ScoreDataManager.Instance != null)
            {
                ScoreDataManager.Instance.SaveCurrentLevelData();
            }
            
            Debug.Log($"===== PHASE 1 COMPLETE =====");
            Debug.Log($"Tasks: {completedTasks}/{totalTasks}");
            Debug.Log($"Mistakes: {mistakeCount}");
            Debug.Log($"Safety Violations: {safetyViolations}");
            Debug.Log($"Final Score: {currentScore}/{perfectScore}");
            Debug.Log($"Accuracy: {GetAccuracyPercentage():F1}%");
            Debug.Log($"============================");
        }
    }

    // PUBLIC GETTERS for evaluation system
    public int GetCurrentScore() => currentScore;
    public int GetPerfectScore() => perfectScore;
    public int GetMistakeCount() => mistakeCount;
    public int GetSafetyViolations() => safetyViolations;
    public int GetCompletedTasks() => completedTasks;
    public int GetTotalTasks() => totalTasks;
    
    public float GetCompletionPercentage()
    {
        if (totalTasks == 0) return 0f;
        return (float)completedTasks / totalTasks * 100f;
    }
    
    public float GetAccuracyPercentage()
    {
        if (perfectScore == 0) return 100f;
        return (float)currentScore / perfectScore * 100f;
    }
    
    // Detailed task breakdown getters
    public int GetStorageTasksCompleted() => storageTasksCompleted;
    public int GetMaintenanceTasksCompleted() => maintenanceTasksCompleted;
    public int GetDisposalTasksCompleted() => disposalTasksCompleted;
    public int GetStorageErrors() => storageErrors;
    public int GetMaintenanceErrors() => maintenanceErrors;
    public int GetDisposalErrors() => disposalErrors;
}