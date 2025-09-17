using UnityEngine;
using TMPro; // Required for TextMeshPro

/// <summary>
/// A centralized score tracking system that listens for task completion events
/// and updates a score display.
/// </summary>
public class ScoreTracker : MonoBehaviour
{
    // Singleton pattern
    public static ScoreTracker Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI scoreText;

    // The score counters
    private int completedTasks = 0;
    private int totalTasks = 0;

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

        // Initialize the score display
        UpdateScoreDisplay();
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
    /// </summary>
    private void OnTaskCompleted()
    {
        completedTasks++;
        Debug.Log($"Task completed! Current score: {completedTasks}/{totalTasks}");
        UpdateScoreDisplay();
    }

    /// <summary>
    /// Updates the UI text to display the current score.
    /// </summary>
    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {completedTasks} / {totalTasks}";
        }
    }
}