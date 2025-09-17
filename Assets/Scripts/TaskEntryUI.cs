using UnityEngine;
using TMPro; // Required for TextMeshPro
using UnityEngine.UI; // Required for UI components like Image and Button

/// <summary>
/// Controls the UI for a single task list entry.
/// It can handle both standard TaskController and MaintenanceTaskController.
/// </summary>
public class TaskEntryUI : MonoBehaviour
{
    // The UI components to display the task information
    public TextMeshProUGUI taskDescriptionText;
    public TextMeshProUGUI progressText;
    public Image completedCheckmark;
    public Button selectTaskButton;

    // A reference to the TaskController for this specific task
    private TaskController associatedTask;
    // A reference for the maintenance task
    private MaintenanceTaskController associatedMaintenanceTask;
    // A reference for the new storage task
    private StorageTaskController associatedStorageTask;


    /// <summary>
    /// This method is called by the TaskListManager to set up the UI entry for a standard task.
    /// It's the most important method in this script.
    /// </summary>
    public void SetTask(TaskController task)
{
    if (task == null)
    {
        Debug.LogError("SetTask was called with a null TaskController. Check your TaskListManager instantiation logic.");
        return;
    }
    associatedTask = task;

    // Populate the UI with data from the task controller
    taskDescriptionText.text = task.taskDescription;
    if (completedCheckmark != null)
    {
        completedCheckmark.gameObject.SetActive(false);
        completedCheckmark.enabled = false;
    }

    // Register for events from the task controller
    associatedTask.OnProgressUpdated.AddListener(UpdateProgress);
    associatedTask.OnTaskCompleted.AddListener(MarkTaskAsCompleted);
    
    // Subscribe to the button click event
    if (selectTaskButton != null)
    {
        selectTaskButton.onClick.AddListener(OnTaskEntryClicked);
    }
    else
    {
        Debug.LogError("Select Task Button is not assigned on the TaskEntryUI prefab!");
    }

    // NEW: Update initial progress after events are subscribed
    // Check if this is a PullDownTrigger and call its special method
    PullDownTrigger pullDownTask = task as PullDownTrigger;
    if (pullDownTask != null)
    {
        pullDownTask.UpdateInitialProgress();
    }
}

    /// <summary>
    /// This method is called by the MaintenanceTaskListManager to set up the UI entry for a maintenance task.
    /// </summary>
    public void SetMaintenanceTask(MaintenanceTaskController task)
    {
        if (task == null)
        {
            Debug.LogError("SetMaintenanceTask was called with a null MaintenanceTaskController.");
            return;
        }
        associatedMaintenanceTask = task;

        // Populate the UI with data from the task controller
        taskDescriptionText.text = task.taskDescription;
        if (completedCheckmark != null)
        {
            completedCheckmark.gameObject.SetActive(false);
            completedCheckmark.enabled = false;
        }

        // Register for events from the task controller
        associatedMaintenanceTask.OnProgressUpdated.AddListener(UpdateProgress);
        associatedMaintenanceTask.OnTaskCompleted.AddListener(MarkTaskAsCompleted);
        
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
    /// This method is called by the StorageTaskListManager to set up the UI entry for a storage task.
    /// </summary>
    public void SetStorageTask(StorageTaskController task)
    {
        if (task == null)
        {
            Debug.LogError("SetStorageTask was called with a null StorageTaskController.");
            return;
        }
        associatedStorageTask = task;

        // Populate the UI with data from the task controller
        taskDescriptionText.text = task.taskDescription;
        if (completedCheckmark != null)
        {
            completedCheckmark.gameObject.SetActive(false);
            completedCheckmark.enabled = false;
        }

        // Register for events from the task controller
        associatedStorageTask.OnProgressUpdated.AddListener(UpdateProgress);
        associatedStorageTask.OnTaskCompleted.AddListener(MarkTaskAsCompleted);
        
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
    /// </summary>
    private void OnTaskEntryClicked()
    {
        // This is where you would trigger the highlighting logic.
        if (associatedTask != null)
        {
            // First, tell the TaskListManager to handle the highlighting
            TaskListManager.Instance.SelectTask(associatedTask);
        }
        else if (associatedMaintenanceTask != null)
        {
            // Use the maintenance manager if it's that type of task
            MaintenanceTaskListManager.Instance.SelectTask(associatedMaintenanceTask);
        }
        else if (associatedStorageTask != null)
        {
            // Use the storage manager if it's that type of task
            StorageTaskListManager.Instance.SelectTask(associatedStorageTask);
        }
    }
}