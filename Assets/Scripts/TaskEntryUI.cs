using UnityEngine;
using TMPro; // Required for TextMeshPro
using UnityEngine.UI; // Required for UI components like Image and Button

public class TaskEntryUI : MonoBehaviour
{
    // The UI components to display the task information
    public TextMeshProUGUI taskDescriptionText;
    public TextMeshProUGUI progressText;
    public Image completedCheckmark;
    public Button selectTaskButton;

    // A reference to the TaskController for this specific task
    private TaskController associatedTask;

    /// <summary>
    /// This method is called by the TaskListManager to set up the UI entry.
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
        completedCheckmark.enabled = false;

        // Register for events from the task controller
        // This is how the UI gets updated automatically!
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
// Inside TaskEntryUI.cs
/// <summary>
/// This is called when the task is fully completed.
/// </summary>
private void MarkTaskAsCompleted()
{
    // Update the UI elements


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

    Debug.Log($"TaskEntryUI received task completed event for '{associatedTask.taskName}'. Attempting to update UI.");
    
    // Call EndTask() here after the UI has been updated
    associatedTask.EndTask();
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
    }
}
