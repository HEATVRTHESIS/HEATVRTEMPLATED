using UnityEngine;
using TMPro; // Required for TextMeshPro
using UnityEngine.UI; // Required for UI components like Image and Button

/// <summary>
/// Controls the UI for a single task list entry.
/// It can handle TaskController, MaintenanceTaskController, and StorageTaskController.
/// Now works with the unified TaskListManager.
/// </summary>
public class TaskEntryUI : MonoBehaviour
{
    // The UI components to display the task information
    public TextMeshProUGUI taskDescriptionText;
    public TextMeshProUGUI progressText;
    public Image completedCheckmark;
    public Button selectTaskButton;

    // References to the different types of task controllers
    private TaskController associatedTask;
    private MaintenanceTaskController associatedMaintenanceTask;
    private StorageTaskController associatedStorageTask;

    /// <summary>
    /// This method is called by the TaskListManager to set up the UI entry for a standard task.
    /// </summary>
    public void SetTask(TaskController task)
    {
        if (task == null)
        {
            Debug.LogError("SetTask was called with a null TaskController. Check your TaskListManager instantiation logic.");
            return;
        }
        
        associatedTask = task;
        SetupUI(task.taskDescription);

        // Register for events from the task controller
        associatedTask.OnProgressUpdated.AddListener(UpdateProgress);
        associatedTask.OnTaskCompleted.AddListener(MarkTaskAsCompleted);
    }

    /// <summary>
    /// This method is called by the TaskListManager to set up the UI entry for a maintenance task.
    /// </summary>
    public void SetMaintenanceTask(MaintenanceTaskController task)
    {
        if (task == null)
        {
            Debug.LogError("SetMaintenanceTask was called with a null MaintenanceTaskController.");
            return;
        }
        
        associatedMaintenanceTask = task;
        SetupUI(task.taskDescription);

        // Register for events from the task controller
        associatedMaintenanceTask.OnProgressUpdated.AddListener(UpdateProgress);
        associatedMaintenanceTask.OnTaskCompleted.AddListener(MarkTaskAsCompleted);
    }

    /// <summary>
    /// This method is called by the TaskListManager to set up the UI entry for a storage task.
    /// </summary>
    public void SetStorageTask(StorageTaskController task)
    {
        if (task == null)
        {
            Debug.LogError("SetStorageTask was called with a null StorageTaskController.");
            return;
        }
        
        associatedStorageTask = task;
        SetupUI(task.taskDescription);

        // Register for events from the task controller
        associatedStorageTask.OnProgressUpdated.AddListener(UpdateProgress);
        associatedStorageTask.OnTaskCompleted.AddListener(MarkTaskAsCompleted);
    }

    /// <summary>
    /// Common UI setup logic for all task types.
    /// </summary>
    private void SetupUI(string taskDescription)
    {
        // Populate the UI with data from the task controller
        taskDescriptionText.text = taskDescription;
        
        if (completedCheckmark != null)
        {
            completedCheckmark.gameObject.SetActive(false);
            completedCheckmark.enabled = false;
        }

        // Subscribe to the button click event
        if (selectTaskButton != null)
        {
            selectTaskButton.onClick.AddListener(OnTaskEntryClicked);
        }
        else
        {
            Debug.LogError("Select Task Button is not assigned on the TaskEntryUI prefab!");
        }
    }

    /// <summary>
    /// This is called when the task's progress changes.
    /// </summary>
    private void UpdateProgress(int completed, int total)
    {
        if (progressText != null)
        {
            progressText.text = $"{completed}/{total}";
        }
    }

    /// <summary>
    /// This is called when the task is fully completed.
    /// </summary>
    private void MarkTaskAsCompleted()
    {
        // Update the UI elements
        if (progressText != null)
        {
            progressText.gameObject.SetActive(false);
        }

        if (completedCheckmark != null)
        {
            completedCheckmark.gameObject.SetActive(true);
            completedCheckmark.enabled = true;
        }

        // Disable the button to prevent further interaction
        if (selectTaskButton != null)
        {
            selectTaskButton.interactable = false;
        }

        Debug.Log($"TaskEntryUI received task completed event for '{taskDescriptionText.text}'. Attempting to update UI.");
        
        // Call EndTask() on the correct associated task
        if (associatedTask != null)
        {
            associatedTask.EndTask();
        }
        else if (associatedMaintenanceTask != null)
        {
            associatedMaintenanceTask.EndTask();
        }
        else if (associatedStorageTask != null)
        {
            associatedStorageTask.EndTask();
        }
    }

    /// <summary>
    /// This method is called when the player clicks on this task entry.
    /// Now uses the unified TaskListManager for all task types.
    /// </summary>
    private void OnTaskEntryClicked()
    {
        // Check if TaskListManager instance exists
        if (TaskListManager.Instance == null)
        {
            Debug.LogError("TaskListManager.Instance is null! Make sure TaskListManager exists in the scene.");
            return;
        }

        // Use the unified TaskListManager to handle all task types
        if (associatedTask != null)
        {
            TaskListManager.Instance.SelectTask(associatedTask);
        }
        else if (associatedMaintenanceTask != null)
        {
            TaskListManager.Instance.SelectMaintenanceTask(associatedMaintenanceTask);
        }
        else if (associatedStorageTask != null)
        {
            TaskListManager.Instance.SelectStorageTask(associatedStorageTask);
        }
        else
        {
            Debug.LogError("TaskEntryUI has no associated task! This should not happen.");
        }
    }

    /// <summary>
    /// Clean up event listeners when this UI element is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (associatedTask != null)
        {
            associatedTask.OnProgressUpdated.RemoveListener(UpdateProgress);
            associatedTask.OnTaskCompleted.RemoveListener(MarkTaskAsCompleted);
        }
        
        if (associatedMaintenanceTask != null)
        {
            associatedMaintenanceTask.OnProgressUpdated.RemoveListener(UpdateProgress);
            associatedMaintenanceTask.OnTaskCompleted.RemoveListener(MarkTaskAsCompleted);
        }
        
        if (associatedStorageTask != null)
        {
            associatedStorageTask.OnProgressUpdated.RemoveListener(UpdateProgress);
            associatedStorageTask.OnTaskCompleted.RemoveListener(MarkTaskAsCompleted);
        }

        // Unsubscribe from button click
        if (selectTaskButton != null)
        {
            selectTaskButton.onClick.RemoveListener(OnTaskEntryClicked);
        }
    }
}