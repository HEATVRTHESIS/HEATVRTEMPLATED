using UnityEngine;
using UnityEngine.Events;
using System.Linq;

/// <summary>
/// Manages the highlighting and progress for a "storage" task.
/// Attach this script to the root of your task prefab.
/// </summary>
public class StorageTaskController : MonoBehaviour
{
    public string taskName;
    public string taskDescription;

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

        if (taskTargets.Length > 0)
        {
            // Set the parent task controller for each storable item.
            foreach (var item in taskTargets)
            {
                item.parentTaskController = this;
            }
        }
        else
        {
            Debug.LogError("Storage task '" + taskName + "' was initialized but no storable items were found!");
        }

        totalItems = taskTargets.Length;
        OnProgressUpdated.Invoke(completedItems, totalItems);
    }

    /// <summary>
    /// Called by a StorableItem when it's correctly stored.
    /// </summary>
    public void ItemStored()
    {
        if (isTaskCompleted) return;

        completedItems++;
        
        Debug.Log($"Task '{taskName}': Item stored. Progress: {completedItems}/{totalItems}");
        
        OnProgressUpdated.Invoke(completedItems, totalItems);

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