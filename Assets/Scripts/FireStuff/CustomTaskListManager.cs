using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// Manages custom tasks like fire alarms and NPC evacuations.
/// Separate from other task managers to avoid conflicts.
/// </summary>
public class CustomTaskListManager : MonoBehaviour
{
    // Singleton pattern
    public static CustomTaskListManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject taskEntryUIPrefab;
    public Transform taskEntryParentTransform;
    public CanvasGroup taskListCanvasGroup;

    // List to hold all active custom tasks
    private List<CustomTaskController> activeTasks = new List<CustomTaskController>();
    
    // Dictionary to store UI entries
    private Dictionary<CustomTaskController, CustomTaskEntryUI> taskUIMap = new Dictionary<CustomTaskController, CustomTaskEntryUI>();

    // Currently selected task
    private CustomTaskController currentSelectedTask;

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
        // Find all CustomTaskControllers in the scene
        CustomTaskController[] tasksInScene = FindObjectsOfType<CustomTaskController>();
        Debug.Log($"Found {tasksInScene.Length} custom task(s) in scene:");
        
        foreach (var task in tasksInScene)
        {
            Debug.Log($"- Found: {task.GetType().Name} named '{task.taskName}' on GameObject '{task.gameObject.name}'");
            RegisterTask(task);
        }

        // Start all tasks to highlight everything initially
        foreach (var task in activeTasks)
        {
            task.StartTask();
        }
    }

    /// <summary>
    /// Registers a new custom task and creates its UI entry
    /// </summary>
    public void RegisterTask(CustomTaskController task)
    {
        if (taskEntryUIPrefab == null || taskEntryParentTransform == null)
        {
            Debug.LogError("Custom Task List UI references are not set!");
            return;
        }

        Debug.Log($"Registering new custom task: '{task.taskName}'.");

        // Add to active tasks list
        activeTasks.Add(task);

        // Create UI entry
        GameObject newEntry = Instantiate(taskEntryUIPrefab, taskEntryParentTransform);
        CustomTaskEntryUI uiEntry = newEntry.GetComponent<CustomTaskEntryUI>();
        
        if (uiEntry != null)
        {
            uiEntry.SetTask(task);
            taskUIMap.Add(task, uiEntry);
        }

        // Initialize the task
        task.InitializeTask();
    }

    /// <summary>
    /// Select a task for highlighting
    /// </summary>
    public void SelectTask(CustomTaskController task)
{
    Debug.Log($"CustomTaskListManager.SelectTask() called for: {task.taskName}");
    
    // Tell other managers to turn off their highlights
    TurnOffOtherManagerHighlights();

    // Turn off highlights for all tasks in this manager
    Debug.Log("Turning off highlights for all custom tasks...");
    foreach (var activeTask in activeTasks)
    {
        Debug.Log($"Calling EndTask() on: {activeTask.taskName}");
        activeTask.EndTask();
    }

    // Highlight only the selected task
    currentSelectedTask = task;
    Debug.Log($"Now highlighting selected task: {task.taskName}");
    currentSelectedTask.StartTask();
}

    /// <summary>
    /// Turn off highlights in other task managers
    /// </summary>
    private void TurnOffOtherManagerHighlights()
    {
        if (TaskListManager.Instance != null)
        {
            TaskListManager.Instance.TurnOffAllHighlights();
        }
        if (MaintenanceTaskListManager.Instance != null)
        {
            MaintenanceTaskListManager.Instance.TurnOffAllHighlights();
        }
        // Add other managers as needed
    }

    /// <summary>
    /// Turn off all highlights - called by other managers
    /// </summary>
    public void TurnOffAllHighlights()
    {
        foreach (var activeTask in activeTasks)
        {
            activeTask.EndTask();
        }
        currentSelectedTask = null;
    }

    /// <summary>
    /// Deselect current task and show all highlights
    /// </summary>
    public void DeselectTask()
    {
        currentSelectedTask = null;
        
        // Turn on highlights for all tasks
        foreach (var activeTask in activeTasks)
        {
            activeTask.StartTask();
        }

        // Tell other managers to restore highlights
        RestoreOtherManagerHighlights();
    }

    /// <summary>
    /// Restore highlights in other managers
    /// </summary>
    private void RestoreOtherManagerHighlights()
    {
        if (TaskListManager.Instance != null)
        {
            TaskListManager.Instance.RestoreAllHighlights();
        }
        if (MaintenanceTaskListManager.Instance != null)
        {
            MaintenanceTaskListManager.Instance.RestoreAllHighlights();
        }
        // Add other managers as needed
    }

    /// <summary>
    /// Restore all highlights - called by other managers
    /// </summary>
    public void RestoreAllHighlights()
    {
        foreach (var activeTask in activeTasks)
        {
            activeTask.StartTask();
        }
        currentSelectedTask = null;
    }
}