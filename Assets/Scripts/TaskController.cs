using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages the highlighting and progress for a specific task prefab.
/// Attach this script to the root of your prefab.
/// </summary>
public class TaskController : MonoBehaviour
{
    // Public fields to define the task
    public string taskName;
    public string taskDescription;

    // The spawner is now a child of this prefab.
    private ObjectSpawner objectSpawner;

    // A private counter for completed items
    private int completedItems = 0;
    
    [HideInInspector]
    public int totalItems = 0;
    private bool isTaskCompleted = false;

    // Events to notify other scripts of progress and completion
    public UnityEvent<int, int> OnProgressUpdated;
    public UnityEvent OnTaskCompleted;

    // An array to hold references to the trash items within this prefab.
    private TrashItem[] taskTargets;
    
    // A reference to the bin associated with this task.
    private HighlightableObject associatedBin;

    void Awake()
    {
        // Find the Object Spawner that is a child of this same prefab.
        objectSpawner = GetComponentInChildren<ObjectSpawner>();
        if (objectSpawner == null)
        {
            Debug.LogError("TaskController requires a child ObjectSpawner component.");
        }
        
        // Find the bin that is a child of this prefab and get its HighlightableObject component.
        Bin bin = GetComponentInChildren<Bin>();
        if (bin != null)
        {
            associatedBin = bin.GetComponent<HighlightableObject>();
        }
        else
        {
            Debug.LogError("TaskController requires a child Bin component.");
        }
    }

    /// <summary>
    /// This is the entry point for the task. It's called by the TaskListManager.
    /// </summary>
    public void InitializeTask()
    {
        // Tell the spawner to spawn the objects for this task.
        objectSpawner.SpawnObjects();

        // Find all TrashItem components that were just spawned under this object.
        taskTargets = GetComponentsInChildren<TrashItem>();
        
        if (taskTargets.Length > 0)
        {
            // Set the parent task controller for each trash item.
            // This is the CRITICAL link between the trash and the task.
            foreach (var trashItem in taskTargets)
            {
                trashItem.parentTaskController = this;
            }
        }
        else
        {
            Debug.LogError("Task '" + taskName + "' was initialized but no trash items were found!");
        }

        // Set the total items
        totalItems = taskTargets.Length;
        OnProgressUpdated.Invoke(completedItems, totalItems);
    }

    /// <summary>
    /// Called by a TrashItem when it's correctly disposed of.
    /// </summary>
    public void ItemDisposedOf()
    {
        if (isTaskCompleted) return;

        completedItems++;
        
        Debug.Log($"Task '{taskName}': Item disposed. Progress: {completedItems}/{totalItems}");
        
        // Notify the UI to update the progress text.
        OnProgressUpdated.Invoke(completedItems, totalItems);

        // Check if the task is complete.
        if (completedItems >= totalItems)
        {
            Debug.Log($"Task '{taskName}' is fully completed! Invoking event.");
            isTaskCompleted = true;
            OnTaskCompleted.Invoke();
        
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
        
        // Highlight the associated bin
        if (associatedBin != null)
        {
            associatedBin.SetHighlight(true);
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

        // Un-highlight the associated bin
        if (associatedBin != null)
        {
            associatedBin.SetHighlight(false);
        }
    }
}
