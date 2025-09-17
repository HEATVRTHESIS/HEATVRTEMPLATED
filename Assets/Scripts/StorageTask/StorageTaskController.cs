using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Manages the highlighting and progress for a "storage" task.
/// Attach this script to the root of your task prefab.
/// </summary>
public class StorageTaskController : MonoBehaviour
{
    public string taskName;
    public string taskDescription;

    // A public array for the dialogue lines to display on task completion.
    // This allows you to set the dialogue directly in the Unity Inspector.
    [Tooltip("The dialogue lines to display when this task is completed.")]
    public string[] taskCompletionDialogue;

    // Public fields for the success sound clip.
    [Header("Audio")]
    [Tooltip("The AudioSource component that will play the sound.")]
    public AudioSource audioSource;
    [Tooltip("The audio clip to play when the task is completed.")]
    public AudioClip successSound;

    private int completedItems = 0;
    private int totalItems = 0;
    private bool isTaskCompleted = false;

    // Events to notify other scripts of progress and completion
    public UnityEvent<int, int> OnProgressUpdated;
    public UnityEvent OnTaskCompleted;

    // References to the objects managed by this task
    private StorableItem[] taskTargets;
    private HighlightableObject associatedContainer;

    void Awake()
    {
        // Find the storage container that is a child of this prefab.
        StorageContainer container = GetComponentInChildren<StorageContainer>();
        if (container != null)
        {
            associatedContainer = container.GetComponent<HighlightableObject>();
        }
        else
        {
            Debug.LogError("StorageTaskController requires a child StorageContainer component.");
        }
    }

    /// <summary>
    /// This is the entry point for the task. It's called by the MaintenanceTaskListManager.
    /// </summary>
    public void InitializeTask()
    {
        // Find all StorableItem components under this object.
        taskTargets = GetComponentsInChildren<StorableItem>();
        
        // Set the parent task controller for each storable item.
        foreach (var storableItem in taskTargets)
        {
            storableItem.parentTaskController = this;
        }

        // Set the total items
        totalItems = taskTargets.Length;
        OnProgressUpdated.Invoke(completedItems, totalItems);
    }

    /// <summary>
    /// Called by a StorableItem when it's correctly placed in the container.
    /// </summary>
    public void ItemStored()
    {
        if (isTaskCompleted) return;

        completedItems++;
        
        Debug.Log($"Task '{taskName}': Item stored. Progress: {completedItems}/{totalItems}");
        
        // Notify the UI to update the progress text.
        OnProgressUpdated.Invoke(completedItems, totalItems);

        // Check if the task is complete.
        if (completedItems >= totalItems)
        {
            Debug.Log($"Task '{taskName}' is fully completed! Invoking event.");
            isTaskCompleted = true;
            OnTaskCompleted.Invoke();
            EndTask();

            // Play the success sound
            if (audioSource != null && successSound != null)
            {
                audioSource.PlayOneShot(successSound);
            }

            // Get the VRDialogueSystem instance and display the completion dialogue
            VRDialogueSystem dialogueSystem = FindObjectOfType<VRDialogueSystem>();
            if (dialogueSystem != null && taskCompletionDialogue != null && taskCompletionDialogue.Length > 0)
            {
                dialogueSystem.StartDialog(taskCompletionDialogue);
            }
        }
    }

    /// <summary>
    /// Starts the highlighting for this task.
    /// </summary>
    public void StartTask()
    {
        if (isTaskCompleted) return;

        // Highlight the items
        foreach (var item in taskTargets)
        {
            if (item != null)
            {
                HighlightableObject highlightableObject = item.GetComponent<HighlightableObject>();
                if (highlightableObject != null)
                {
                    highlightableObject.SetHighlight(true);
                }
            }
        }
        
        // Highlight the associated container
        if (associatedContainer != null)
        {
            associatedContainer.SetHighlight(true);
        }
    }

    /// <summary>
    /// Ends the highlighting for this task.
    /// </summary>
    public void EndTask()
    {
        Debug.Log($"Ending task '{taskName}' and turning off highlights.");

        // Un-highlight the items
        foreach (var item in taskTargets)
        {
            if (item != null)
            {
                HighlightableObject highlightableObject = item.GetComponent<HighlightableObject>();
                if (highlightableObject != null)
                {
                    highlightableObject.SetHighlight(false);
                }
            }
        }

        // Un-highlight the associated container
        if (associatedContainer != null)
        {
            associatedContainer.SetHighlight(false);
        }
    }
}
