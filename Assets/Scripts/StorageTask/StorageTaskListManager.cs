using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// Manages a task list specifically for StorageTaskControllers.
/// This is a separate Singleton to keep the disposal, maintenance, and storage tasks separate.
/// </summary>
public class StorageTaskListManager : MonoBehaviour
{
    // Singleton pattern
    public static StorageTaskListManager Instance { get; private set; }

    [Header("UI References")]
    // The prefab for a single task entry
    public GameObject taskEntryUIPrefab;
    // The parent transform for all task entries
    public Transform taskEntryParentTransform;

    // The Canvas Group to control the visibility of the whole UI
    public CanvasGroup taskListCanvasGroup;

    // A list to hold all the active storage tasks in the scene
    private List<StorageTaskController> activeTasks = new List<StorageTaskController>();

    // A DICTIONARY TO STORE REFERENCES TO THE INSTANTIATED UI ENTRIES
    private Dictionary<StorageTaskController, TaskEntryUI> taskUIMap = new Dictionary<StorageTaskController, TaskEntryUI>();

    // A reference to the currently selected/active task
    private StorageTaskController currentSelectedTask;

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
        // Find all StorageTaskControllers that are already in the scene at the start
        StorageTaskController[] tasksInScene = FindObjectsOfType<StorageTaskController>();
        foreach (var task in tasksInScene)
        {
            RegisterTask(task);
        }

        // Start all tasks to highlight everything initially
        foreach (var task in activeTasks)
        {
            task.StartTask();
        }
    }

    /// <summary>
    /// Registers a new storage task and creates its corresponding UI entry.
    /// </summary>
    public void RegisterTask(StorageTaskController task)
    {
        if (taskEntryUIPrefab == null || taskEntryParentTransform == null)
        {
            Debug.LogError("Task List UI references are not set in the StorageTaskListManager!");
            return;
        }

        Debug.Log($"Registering new storage task: '{task.taskName}'.");

        // Add the task to our list of active tasks
        activeTasks.Add(task);

        // Instantiate the UI prefab and place it as a child of the `taskEntryParentTransform`.
        GameObject newEntry = Instantiate(taskEntryUIPrefab, taskEntryParentTransform);

        // Get the UI script from the new entry
        TaskEntryUI uiEntry = newEntry.GetComponent<TaskEntryUI>();
        if (uiEntry != null)
        {
            // This is the critical link that updates the UI.
            uiEntry.SetStorageTask(task);
            
            // ADD THE NEWLY CREATED UI INSTANCE TO THE DICTIONARY
            taskUIMap.Add(task, uiEntry);
        }

        // Initialize the task controller.
        task.InitializeTask();
    }

    /// <summary>
    /// Selects a new task, highlighting its objects and un-highlighting others.
    /// This also communicates with other managers to turn off their highlights.
    /// </summary>
    public void SelectTask(StorageTaskController task)
    {
        // First, tell all other managers to turn off their highlights
        if (TaskListManager.Instance != null)
        {
            TaskListManager.Instance.TurnOffAllHighlights();
        }
        if (MaintenanceTaskListManager.Instance != null)
        {
            MaintenanceTaskListManager.Instance.TurnOffAllHighlights();
        }

        // Turn off highlights for ALL tasks in this manager
        foreach (var activeTask in activeTasks)
        {
            activeTask.EndTask();
        }

        // Start highlighting only the selected task
        currentSelectedTask = task;
        currentSelectedTask.StartTask();
    }

    /// <summary>
    /// Turns off all highlights in this manager. Called by other managers.
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
    /// Deselects the current task and shows all highlights again.
    /// This also tells other managers to restore their highlights.
    /// </summary>
    public void DeselectTask()
    {
        currentSelectedTask = null;
        
        // Turn on highlights for ALL tasks in this manager
        foreach (var activeTask in activeTasks)
        {
            activeTask.StartTask();
        }

        // Tell other managers to restore their highlights too
        if (TaskListManager.Instance != null)
        {
            TaskListManager.Instance.RestoreAllHighlights();
        }
        if (MaintenanceTaskListManager.Instance != null)
        {
            MaintenanceTaskListManager.Instance.RestoreAllHighlights();
        }
    }

    /// <summary>
    /// Restores all highlights in this manager. Called by other managers.
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