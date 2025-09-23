using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;

/// <summary>
/// Unified TaskListManager that manages all types of tasks (TaskController, MaintenanceTaskController, StorageTaskController).
/// This is a Singleton, meaning there is only one instance of it in the scene.
/// </summary>
public class TaskListManager : MonoBehaviour
{
    // Singleton pattern
    public static TaskListManager Instance { get; private set; }

    [Header("UI References")]
    // The prefab for a single task entry
    public GameObject taskEntryUIPrefab;
    // The parent transform for all task entries
    public Transform taskEntryParentTransform;

    // The Canvas Group to control the visibility of the whole UI
    public CanvasGroup taskListCanvasGroup;

    // Lists to hold all the active tasks in the scene by type
    private List<TaskController> activeTasks = new List<TaskController>();
    private List<MaintenanceTaskController> activeMaintenanceTasks = new List<MaintenanceTaskController>();
    private List<StorageTaskController> activeStorageTasks = new List<StorageTaskController>();

    // Dictionaries to store references to the instantiated UI entries
    private Dictionary<TaskController, TaskEntryUI> taskUIMap = new Dictionary<TaskController, TaskEntryUI>();
    private Dictionary<MaintenanceTaskController, TaskEntryUI> maintenanceTaskUIMap = new Dictionary<MaintenanceTaskController, TaskEntryUI>();
    private Dictionary<StorageTaskController, TaskEntryUI> storageTaskUIMap = new Dictionary<StorageTaskController, TaskEntryUI>();

    // References to the currently selected/active tasks
    private TaskController currentSelectedTask;
    private MaintenanceTaskController currentSelectedMaintenanceTask;
    private StorageTaskController currentSelectedStorageTask;

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
        // Find all task controllers that are already in the scene at the start
        TaskController[] tasksInScene = FindObjectsOfType<TaskController>();
        MaintenanceTaskController[] maintenanceTasksInScene = FindObjectsOfType<MaintenanceTaskController>();
        StorageTaskController[] storageTasksInScene = FindObjectsOfType<StorageTaskController>();

        Debug.Log($"Found {tasksInScene.Length} TaskController(s) in scene:");
        Debug.Log($"Found {maintenanceTasksInScene.Length} MaintenanceTaskController(s) in scene:");
        Debug.Log($"Found {storageTasksInScene.Length} StorageTaskController(s) in scene:");

        // Register regular tasks
        foreach (var task in tasksInScene)
        {
            try
            {
                Debug.Log($"- Found: {task.GetType().Name} named '{task.taskName}' on GameObject '{task.gameObject.name}'");
                RegisterTask(task);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error processing task {task.GetType().Name}: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
            }
        }

        // Register maintenance tasks
        foreach (var task in maintenanceTasksInScene)
        {
            RegisterMaintenanceTask(task);
        }

        // Register storage tasks
        foreach (var task in storageTasksInScene)
        {
            RegisterStorageTask(task);
        }

        // Start all tasks to highlight everything initially
        StartAllTasks();
    }

    #region Regular Task Management

    /// <summary>
    /// Registers a new task and creates its corresponding UI entry.
    /// </summary>
    public void RegisterTask(TaskController task)
    {
        if (taskEntryUIPrefab == null || taskEntryParentTransform == null)
        {
            Debug.LogError("Task List UI references are not set in the TaskListManager!");
            return;
        }

        Debug.Log($"Registering new task: '{task.taskName}'.");

        // Add the task to our list of active tasks
        activeTasks.Add(task);

        // Instantiate the UI prefab and place it as a child of the `taskEntryParentTransform`.
        GameObject newEntry = Instantiate(taskEntryUIPrefab, taskEntryParentTransform);

        // Get the UI script from the new entry
        TaskEntryUI uiEntry = newEntry.GetComponent<TaskEntryUI>();
        if (uiEntry != null)
        {
            // This is the critical link that updates the UI.
            uiEntry.SetTask(task);
            
            // Add the newly created UI instance to the dictionary
            taskUIMap.Add(task, uiEntry);
        }

        // Initialize the task controller, which will in turn spawn its objects.
        task.InitializeTask();
    }

    /// <summary>
    /// Selects a new task, highlighting its objects and un-highlighting others.
    /// </summary>
    public void SelectTask(TaskController task)
    {
        // Turn off all highlights from all task types
        TurnOffAllHighlights();

        // Start highlighting only the selected task
        currentSelectedTask = task;
        currentSelectedTask.StartTask();
    }

    #endregion

    #region Maintenance Task Management

    /// <summary>
    /// Registers a new maintenance task and creates its corresponding UI entry.
    /// </summary>
    public void RegisterMaintenanceTask(MaintenanceTaskController task)
    {
        if (taskEntryUIPrefab == null || taskEntryParentTransform == null)
        {
            Debug.LogError("Task List UI references are not set in the TaskListManager!");
            return;
        }

        Debug.Log($"Registering new maintenance task: '{task.taskName}'.");

        // Add the task to our list of active tasks
        activeMaintenanceTasks.Add(task);

        // Instantiate the UI prefab and place it as a child of the `taskEntryParentTransform`.
        GameObject newEntry = Instantiate(taskEntryUIPrefab, taskEntryParentTransform);

        // Get the UI script from the new entry
        TaskEntryUI uiEntry = newEntry.GetComponent<TaskEntryUI>();
        if (uiEntry != null)
        {
            // This is the critical link that updates the UI.
            uiEntry.SetMaintenanceTask(task);
            
            // Add the newly created UI instance to the dictionary
            maintenanceTaskUIMap.Add(task, uiEntry);
        }

        // Initialize the task controller.
        task.InitializeTask();
    }

    /// <summary>
    /// Selects a new maintenance task, highlighting its objects and un-highlighting others.
    /// </summary>
    public void SelectMaintenanceTask(MaintenanceTaskController task)
    {
        // Turn off all highlights from all task types
        TurnOffAllHighlights();

        // Start highlighting only the selected maintenance task
        currentSelectedMaintenanceTask = task;
        currentSelectedMaintenanceTask.StartTask();
    }

    /// <summary>
    /// Checks if the given maintenance task is the currently selected task.
    /// </summary>
    public bool IsThisMaintenanceTaskSelected(MaintenanceTaskController task)
    {
        return currentSelectedMaintenanceTask == task;
    }

    /// <summary>
    /// Deselects the current maintenance task.
    /// </summary>
    public void DeselectMaintenanceTask(MaintenanceTaskController task)
    {
        if (currentSelectedMaintenanceTask == task)
        {
            // Simply clear the reference without restoring highlights
            currentSelectedMaintenanceTask = null;
        }
    }

    #endregion

    #region Storage Task Management

    /// <summary>
    /// Registers a new storage task and creates its corresponding UI entry.
    /// </summary>
    public void RegisterStorageTask(StorageTaskController task)
    {
        if (taskEntryUIPrefab == null || taskEntryParentTransform == null)
        {
            Debug.LogError("Task List UI references are not set in the TaskListManager!");
            return;
        }

        Debug.Log($"Registering new storage task: '{task.taskName}'.");

        // Add the task to our list of active tasks
        activeStorageTasks.Add(task);

        // Instantiate the UI prefab and place it as a child of the `taskEntryParentTransform`.
        GameObject newEntry = Instantiate(taskEntryUIPrefab, taskEntryParentTransform);

        // Get the UI script from the new entry
        TaskEntryUI uiEntry = newEntry.GetComponent<TaskEntryUI>();
        if (uiEntry != null)
        {
            // This is the critical link that updates the UI.
            uiEntry.SetStorageTask(task);
            
            // Add the newly created UI instance to the dictionary
            storageTaskUIMap.Add(task, uiEntry);
        }

        // Initialize the task controller.
        task.InitializeTask();
    }

    /// <summary>
    /// Selects a new storage task, highlighting its objects and un-highlighting others.
    /// </summary>
    public void SelectStorageTask(StorageTaskController task)
    {
        // Turn off all highlights from all task types
        TurnOffAllHighlights();

        // Start highlighting only the selected storage task
        currentSelectedStorageTask = task;
        currentSelectedStorageTask.StartTask();
    }

    #endregion

    #region Universal Task Management

    /// <summary>
    /// Turns off all highlights from all task types.
    /// </summary>
    public void TurnOffAllHighlights()
    {
        // Turn off regular tasks
        foreach (var activeTask in activeTasks)
        {
            activeTask.EndTask();
        }

        // Turn off maintenance tasks
        foreach (var activeTask in activeMaintenanceTasks)
        {
            activeTask.EndTask();
        }

        // Turn off storage tasks
        foreach (var activeTask in activeStorageTasks)
        {
            activeTask.EndTask();
        }

        // Clear all selected task references
        currentSelectedTask = null;
        currentSelectedMaintenanceTask = null;
        currentSelectedStorageTask = null;
    }

    /// <summary>
    /// Starts all tasks to show highlights.
    /// </summary>
    public void StartAllTasks()
    {
        // Start regular tasks
        foreach (var task in activeTasks)
        {
            task.StartTask();
        }

        // Start maintenance tasks
        foreach (var task in activeMaintenanceTasks)
        {
            task.StartTask();
        }

        // Start storage tasks
        foreach (var task in activeStorageTasks)
        {
            task.StartTask();
        }
    }

    /// <summary>
    /// Deselects the current task and shows all highlights again.
    /// </summary>
    public void DeselectTask()
    {
        // Clear all selected references
        currentSelectedTask = null;
        currentSelectedMaintenanceTask = null;
        currentSelectedStorageTask = null;
        
        // Turn on highlights for ALL tasks
        StartAllTasks();
    }

    /// <summary>
    /// Restores all highlights for all task types.
    /// </summary>
    public void RestoreAllHighlights()
    {
        StartAllTasks();
        
        // Clear all selected references
        currentSelectedTask = null;
        currentSelectedMaintenanceTask = null;
        currentSelectedStorageTask = null;
    }

    #endregion

    #region Legacy Compatibility Methods (for backwards compatibility)

    // These methods maintain compatibility with existing code that might reference the old manager instances

    /// <summary>
    /// Legacy method for maintenance task selection (maintains backwards compatibility).
    /// </summary>
    public void SelectTask(MaintenanceTaskController task)
    {
        SelectMaintenanceTask(task);
    }

    /// <summary>
    /// Legacy method for storage task selection (maintains backwards compatibility).
    /// </summary>
    public void SelectTask(StorageTaskController task)
    {
        SelectStorageTask(task);
    }

    /// <summary>
    /// Legacy method for checking if maintenance task is selected (maintains backwards compatibility).
    /// </summary>
    public bool IsThisTaskSelected(MaintenanceTaskController task)
    {
        return IsThisMaintenanceTaskSelected(task);
    }

    #endregion
}