using UnityEngine;
using TMPro; // Required for TextMeshPro

/// <summary>
/// A centralized score tracking system that listens for task completion events
/// and updates a score display with accuracy-based scoring.
/// Focus: Task Accuracy & Safety Compliance
/// +10 points for correct actions, -5 points for errors
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

    [Header("Scoring Settings")]
    [SerializeField] private int pointsPerCompletion = 10;
    [SerializeField] private int pointsPerError = -5;

    // The score counters
    private int completedTasks = 0;
    private int totalTasks = 0;
    private int currentScore = 0;
    private int perfectScore = 0; // Will be calculated as totalTasks * pointsPerCompletion
    private int mistakeCount = 0; // Track number of mistakes made

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
        // This is done to get a total count and subscribe to completion events.
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
            
            // Show the summary canvas
            summaryCanvas.SetActive(true);
            
            Debug.Log($"Level Complete! Tasks: {completedTasks}/{totalTasks}, Mistakes: {mistakeCount}, Final Score: {currentScore}/{perfectScore}");
        }
    }

    /// <summary>
    /// Get the current score value (useful for other systems)
    /// </summary>
    public int GetCurrentScore()
    {
        return currentScore;
    }

    /// <summary>
    /// Get the perfect score possible
    /// </summary>
    public int GetPerfectScore()
    {
        return perfectScore;
    }

    /// <summary>
    /// Get the completion percentage
    /// </summary>
    public float GetCompletionPercentage()
    {
        if (totalTasks == 0) return 0f;
        return (float)completedTasks / totalTasks * 100f;
    }

    /// <summary>
    /// Get the accuracy percentage (score vs perfect score)
    /// </summary>
    public float GetAccuracyPercentage()
    {
        if (perfectScore == 0) return 100f;
        return (float)currentScore / perfectScore * 100f;
    }

    /// <summary>
    /// Get the total number of mistakes made
    /// </summary>
    public int GetMistakeCount()
    {
        return mistakeCount;
    }
}