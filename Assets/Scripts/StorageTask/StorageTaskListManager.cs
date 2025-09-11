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

        // Select the first task by default
        if (activeTasks.Count > 0)
        {
            SelectTask(activeTasks[0]);
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
    /// </summary>
    public void SelectTask(StorageTaskController task)
    {
        // Stop highlighting the previous task
        if (currentSelectedTask != null)
        {
            currentSelectedTask.EndTask();
        }

        // Start highlighting the new task
        currentSelectedTask = task;
        currentSelectedTask.StartTask();
    }
}