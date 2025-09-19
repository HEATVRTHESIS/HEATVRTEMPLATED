using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Controls the UI for a single custom task entry.
/// Handles both PullDownTrigger and NPCInteraction tasks.
/// </summary>
public class CustomTaskEntryUI : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI taskDescriptionText;
    public TextMeshProUGUI progressText;
    public Image completedCheckmark;
    public Button selectTaskButton;

    // Reference to the associated custom task
    private CustomTaskController associatedTask;

    /// <summary>
    /// Set up the UI entry for a custom task
    /// </summary>
    public void SetTask(CustomTaskController task)
    {
        if (task == null)
        {
            Debug.LogError("SetTask was called with a null CustomTaskController.");
            return;
        }

        associatedTask = task;

        // Populate UI with task data
        taskDescriptionText.text = task.taskDescription;
        
        if (completedCheckmark != null)
        {
            completedCheckmark.gameObject.SetActive(false);
            completedCheckmark.enabled = false;
        }

        // Register for events
        associatedTask.OnProgressUpdated.AddListener(UpdateProgress);
        associatedTask.OnTaskCompleted.AddListener(MarkTaskAsCompleted);

        // Set up button click
        if (selectTaskButton != null)
        {
            selectTaskButton.onClick.AddListener(OnTaskEntryClicked);
        }
        else
        {
            Debug.LogError("Select Task Button is not assigned on the CustomTaskEntryUI prefab!");
        }

        // Update initial progress
        associatedTask.UpdateInitialProgress();
    }

    /// <summary>
    /// Update progress text
    /// </summary>
    private void UpdateProgress(int completed, int total)
    {
        if (progressText != null)
        {
            progressText.text = $"{completed}/{total}";
        }
    }

    /// <summary>
    /// Mark task as completed in UI
    /// </summary>
    private void MarkTaskAsCompleted()
    {
        // Hide progress text
        if (progressText != null)
        {
            progressText.gameObject.SetActive(false);
        }

        // Show checkmark
        if (completedCheckmark != null)
        {
            completedCheckmark.gameObject.SetActive(true);
            completedCheckmark.enabled = true;
        }

        // Disable button
        if (selectTaskButton != null)
        {
            selectTaskButton.interactable = false;
        }

        Debug.Log($"CustomTaskEntryUI: Task '{taskDescriptionText.text}' completed.");
        
        // End task highlighting
        if (associatedTask != null)
        {
            associatedTask.EndTask();
        }
    }

    /// <summary>
    /// Handle task entry button click
    /// </summary>
    private void OnTaskEntryClicked()
    {
        if (associatedTask != null && CustomTaskListManager.Instance != null)
        {
            CustomTaskListManager.Instance.SelectTask(associatedTask);
        }
    }
}
