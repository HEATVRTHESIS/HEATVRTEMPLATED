using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using System.Collections.Generic;

public class StorageTaskController : MonoBehaviour
{
    public string taskName;
    public string taskDescription;
    
    private ObjectSpawner objectSpawner;

    [Tooltip("The dialogue lines to display when this task is completed.")]
    public string[] taskCompletionDialogue;

    [Header("Error Dialogue")]
    [Tooltip("The dialogue lines to display on first error.")]
    public string[] taskErrorDialogue;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip successSound;
    public AudioClip errorSound;

    private int completedItems = 0;
    private int totalItems = 0;
    private bool isTaskCompleted = false;

    public UnityEvent<int, int> OnProgressUpdated;
    public UnityEvent OnTaskCompleted;

    private StorableItem[] taskTargets;
    private HighlightableObject associatedContainer;
    private HashSet<string> playedErrorDialogues = new HashSet<string>();

    void Awake()
    {
        StorageContainer container = GetComponentInChildren<StorageContainer>();
        if (container != null)
        {
            associatedContainer = container.GetComponent<HighlightableObject>();
        }
        else
        {
            Debug.LogError("StorageTaskController requires a child StorageContainer component.");
        }

        objectSpawner = GetComponentInChildren<ObjectSpawner>();
        if (objectSpawner == null)
        {
            Debug.LogError("TaskController requires a child ObjectSpawner component.");
        }
    }

    public void InitializeTask()
    {
        objectSpawner.SpawnObjects();
        taskTargets = GetComponentsInChildren<StorableItem>();
        
        foreach (var storableItem in taskTargets)
        {
            storableItem.parentTaskController = this;
        }

        totalItems = taskTargets.Length;
        OnProgressUpdated.Invoke(completedItems, totalItems);
    }

    public void ItemStored()
    {
        if (isTaskCompleted) return;

        completedItems++;
        
        Debug.Log($"Task '{taskName}': Item stored. Progress: {completedItems}/{totalItems}");
        OnProgressUpdated.Invoke(completedItems, totalItems);

        if (completedItems >= totalItems)
        {
            Debug.Log($"Task '{taskName}' is fully completed!");
            isTaskCompleted = true;
            OnTaskCompleted.Invoke();
            EndTask();

            if (audioSource != null && successSound != null)
                audioSource.PlayOneShot(successSound);

            VRDialogueSystem dialogueSystem = FindObjectOfType<VRDialogueSystem>();
            if (dialogueSystem != null && taskCompletionDialogue != null && taskCompletionDialogue.Length > 0)
            {
                dialogueSystem.StartDialog(taskCompletionDialogue);
            }
        }
    }

    public void OnStorageError(string itemType, string containerType)
    {
        if (isTaskCompleted) return;

        if (audioSource != null && errorSound != null)
            audioSource.PlayOneShot(errorSound);

        string errorKey = $"{itemType}_{containerType}";
        bool shouldPlayDialogue = !playedErrorDialogues.Contains(errorKey);

        if (shouldPlayDialogue)
        {
            playedErrorDialogues.Add(errorKey);
            VRDialogueSystem dialogueSystem = FindObjectOfType<VRDialogueSystem>();
            if (dialogueSystem != null && taskErrorDialogue != null && taskErrorDialogue.Length > 0)
                dialogueSystem.StartDialog(taskErrorDialogue);
        }
    }

    public void StartTask()
    {
        if (isTaskCompleted) return;

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
        
        if (associatedContainer != null)
        {
            associatedContainer.SetHighlight(true);
        }
    }

    public void EndTask()
    {
        Debug.Log($"Ending task '{taskName}' and turning off highlights.");

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

        if (associatedContainer != null)
        {
            associatedContainer.SetHighlight(false);
        }
    }
}