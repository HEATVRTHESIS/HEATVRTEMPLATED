using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;

/// <summary>
/// Unified TaskListManager that manages all types of tasks (TaskController, MaintenanceTaskController, StorageTaskController).
/// This is a Singleton, meaning there is only one instance of it in the scene.
/// Now includes filtering functionality for different task types.
/// </summary>
public class TaskListManager : MonoBehaviour
{
    // Enum for different filter types
    public enum TaskFilter
    {
        All,
        Disposal,
        Maintenance,
        Storage
    }

    // Singleton pattern
    public static TaskListManager Instance { get; private set; }

    [Header("UI References")]
    // The prefab for a single task entry
    public GameObject taskEntryUIPrefab;
    // The parent transform for all task entries
    public Transform taskEntryParentTransform;

    // The Canvas Group to control the visibility of the whole UI
    public CanvasGroup taskListCanvasGroup;

    [Header("Filter Buttons")]
    // Filter buttons
    public Button allTasksButton;
    public Button disposalTasksButton;
    public Button maintenanceTasksButton;
    public Button storageTasksButton;

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

    // Current filter state
    private TaskFilter currentFilter = TaskFilter.All;

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
        // Setup filter button listeners
        SetupFilterButtons();

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
        
        // Apply initial filter (show all by default)
        ApplyFilter(TaskFilter.All);
    }

    #region Filter Management

    /// <summary>
    /// Sets up the filter button listeners.
    /// </summary>
    private void SetupFilterButtons()
    {
        if (allTasksButton != null)
            allTasksButton.onClick.AddListener(() => ApplyFilter(TaskFilter.All));
            
        if (disposalTasksButton != null)
            disposalTasksButton.onClick.AddListener(() => ApplyFilter(TaskFilter.Disposal));
            
        if (maintenanceTasksButton != null)
            maintenanceTasksButton.onClick.AddListener(() => ApplyFilter(TaskFilter.Maintenance));
            
        if (storageTasksButton != null)
            storageTasksButton.onClick.AddListener(() => ApplyFilter(TaskFilter.Storage));
    }

    /// <summary>
    /// Applies the specified filter to show only tasks of that type.
    /// </summary>
    public void ApplyFilter(TaskFilter filter)
    {
        currentFilter = filter;
        
        // Update button visual states
        UpdateFilterButtonStates();
        
        // Show/hide UI entries based on filter
        switch (filter)
        {
            case TaskFilter.All:
                ShowAllTaskEntries();
                break;
            case TaskFilter.Disposal:
                ShowOnlyDisposalTaskEntries();
                break;
            case TaskFilter.Maintenance:
                ShowOnlyMaintenanceTaskEntries();
                break;
            case TaskFilter.Storage:
                ShowOnlyStorageTaskEntries();
                break;
        }
        
        Debug.Log($"Applied filter: {filter}");
    }

    /// <summary>
    /// Updates the visual states of filter buttons to show which one is active.
    /// </summary>
    private void UpdateFilterButtonStates()
    {
        // Reset all buttons to normal state
        if (allTasksButton != null)
            allTasksButton.interactable = currentFilter != TaskFilter.All;
            
        if (disposalTasksButton != null)
            disposalTasksButton.interactable = currentFilter != TaskFilter.Disposal;
            
        if (maintenanceTasksButton != null)
            maintenanceTasksButton.interactable = currentFilter != TaskFilter.Maintenance;
            
        if (storageTasksButton != null)
            storageTasksButton.interactable = currentFilter != TaskFilter.Storage;
    }

    /// <summary>
    /// Shows all task entries.
    /// </summary>
    private void ShowAllTaskEntries()
    {
        // Show all regular task entries
        foreach (var kvp in taskUIMap)
        {
            if (kvp.Value != null && kvp.Value.gameObject != null)
                kvp.Value.gameObject.SetActive(true);
        }

        // Show all maintenance task entries
        foreach (var kvp in maintenanceTaskUIMap)
        {
            if (kvp.Value != null && kvp.Value.gameObject != null)
                kvp.Value.gameObject.SetActive(true);
        }

        // Show all storage task entries
        foreach (var kvp in storageTaskUIMap)
        {
            if (kvp.Value != null && kvp.Value.gameObject != null)
                kvp.Value.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Shows only disposal task entries (regular TaskController tasks).
    /// </summary>
    private void ShowOnlyDisposalTaskEntries()
    {
        // Show regular task entries (disposal tasks)
        foreach (var kvp in taskUIMap)
        {
            if (kvp.Value != null && kvp.Value.gameObject != null)
                kvp.Value.gameObject.SetActive(true);
        }

        // Hide maintenance task entries
        foreach (var kvp in maintenanceTaskUIMap)
        {
            if (kvp.Value != null && kvp.Value.gameObject != null)
                kvp.Value.gameObject.SetActive(false);
        }

        // Hide storage task entries
        foreach (var kvp in storageTaskUIMap)
        {
            if (kvp.Value != null && kvp.Value.gameObject != null)
                kvp.Value.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Shows only maintenance task entries.
    /// </summary>
    private void ShowOnlyMaintenanceTaskEntries()
    {
        // Hide regular task entries
        foreach (var kvp in taskUIMap)
        {
            if (kvp.Value != null && kvp.Value.gameObject != null)
                kvp.Value.gameObject.SetActive(false);
        }

        // Show maintenance task entries
        foreach (var kvp in maintenanceTaskUIMap)
        {
            if (kvp.Value != null && kvp.Value.gameObject != null)
                kvp.Value.gameObject.SetActive(true);
        }

        // Hide storage task entries
        foreach (var kvp in storageTaskUIMap)
        {
            if (kvp.Value != null && kvp.Value.gameObject != null)
                kvp.Value.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Shows only storage task entries.
    /// </summary>
    private void ShowOnlyStorageTaskEntries()
    {
        // Hide regular task entries
        foreach (var kvp in taskUIMap)
        {
            if (kvp.Value != null && kvp.Value.gameObject != null)
                kvp.Value.gameObject.SetActive(false);
        }

        // Hide maintenance task entries
        foreach (var kvp in maintenanceTaskUIMap)
        {
            if (kvp.Value != null && kvp.Value.gameObject != null)
                kvp.Value.gameObject.SetActive(false);
        }

        // Show storage task entries
        foreach (var kvp in storageTaskUIMap)
        {
            if (kvp.Value != null && kvp.Value.gameObject != null)
                kvp.Value.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Gets the current active filter.
    /// </summary>
    public TaskFilter GetCurrentFilter()
    {
        return currentFilter;
    }

    #endregion

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
        
        // Apply current filter to the new entry
        ApplyCurrentFilterToNewEntry(newEntry, TaskFilter.Disposal);
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
        
        // Apply current filter to the new entry
        ApplyCurrentFilterToNewEntry(newEntry, TaskFilter.Maintenance);
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
        
        // Apply current filter to the new entry
        ApplyCurrentFilterToNewEntry(newEntry, TaskFilter.Storage);
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

    #region Helper Methods

    /// <summary>
    /// Applies the current filter state to a newly created entry.
    /// </summary>
    private void ApplyCurrentFilterToNewEntry(GameObject newEntry, TaskFilter entryType)
    {
        if (newEntry == null) return;

        bool shouldShow = currentFilter == TaskFilter.All || currentFilter == entryType;
        newEntry.SetActive(shouldShow);
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