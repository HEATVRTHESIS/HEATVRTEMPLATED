using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;

public class TaskController : MonoBehaviour
{
    public string taskName;
    public string taskDescription;

    [Tooltip("The dialogue lines to display when this task is completed.")]
    public string[] taskCompletionDialogue;

    [Header("Error Dialogue")]
    [Tooltip("The dialogue lines to display on first error.")]
    public string[] taskErrorDialogue;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip successSound;
    public AudioClip errorSound;

    private ObjectSpawner objectSpawner;
    private int completedItems = 0;
    
    [HideInInspector]
    public int totalItems = 0;
    private bool isTaskCompleted = false;

    public UnityEvent<int, int> OnProgressUpdated;
    public UnityEvent OnTaskCompleted;

    private TrashItem[] taskTargets;
    private HighlightableObject associatedBin;
    private HashSet<string> playedErrorDialogues = new HashSet<string>();

    void Awake()
    {
        objectSpawner = GetComponentInChildren<ObjectSpawner>();
        if (objectSpawner == null)
            Debug.LogError("TaskController requires a child ObjectSpawner component.");
        
        Bin bin = GetComponentInChildren<Bin>();
        if (bin != null)
        {
            associatedBin = bin.GetComponent<HighlightableObject>();
        }
        else
            Debug.LogError("TaskController requires a child Bin component.");
    }

    public void InitializeTask()
    {
        objectSpawner.SpawnObjects();
        taskTargets = GetComponentsInChildren<TrashItem>();
        
        if (taskTargets.Length > 0)
        {
            foreach (var trashItem in taskTargets)
                trashItem.parentTaskController = this;
        }
        else
            Debug.LogError("Task '" + taskName + "' was initialized but no trash items were found!");

        totalItems = taskTargets.Length;
        OnProgressUpdated.Invoke(completedItems, totalItems);
    }

    public void ItemDisposedOf()
    {
        if (isTaskCompleted) return;

        completedItems++;
        Debug.Log($"Task '{taskName}': Item disposed. Progress: {completedItems}/{totalItems}");
        OnProgressUpdated.Invoke(completedItems, totalItems);

        if (completedItems >= totalItems)
        {
            Debug.Log($"Task '{taskName}' is fully completed!");
            isTaskCompleted = true;
            OnTaskCompleted.Invoke();

            if (audioSource != null && successSound != null)
                audioSource.PlayOneShot(successSound);

            VRDialogueSystem dialogueSystem = FindObjectOfType<VRDialogueSystem>();
            if (dialogueSystem != null && taskCompletionDialogue != null && taskCompletionDialogue.Length > 0)
                dialogueSystem.StartDialog(taskCompletionDialogue);
        }
    }

    public void OnDisposalError(TrashItem.TrashType trashType, Bin.BinType binType)
    {
        if (isTaskCompleted) return;

        if (audioSource != null && errorSound != null)
            audioSource.PlayOneShot(errorSound);

        string errorKey = $"{trashType}_{binType}";
        bool shouldPlayDialogue = !playedErrorDialogues.Contains(errorKey);

        if (shouldPlayDialogue)
        {
            playedErrorDialogues.Add(errorKey);
            VRDialogueSystem dialogueSystem = FindObjectOfType<VRDialogueSystem>();
            if (dialogueSystem != null && taskErrorDialogue != null && taskErrorDialogue.Length > 0)
                dialogueSystem.StartDialog(taskErrorDialogue);
        }
    }

    public virtual void StartTask()
    {
        if (isTaskCompleted) return;

        if (taskTargets != null)
        {
            foreach (var item in taskTargets)
            {
                if (item != null)
                {
                    HighlightableObject highlightableObject = item.GetComponent<HighlightableObject>();
                    if (highlightableObject != null)
                        highlightableObject.SetHighlight(true);
                }
            }
        }
        
        if (associatedBin != null)
            associatedBin.SetHighlight(true);
    }

    public virtual void EndTask()
    {
        if (taskTargets != null)
        {
            foreach (var item in taskTargets)
            {
                if (item != null)
                {
                    HighlightableObject highlightableObject = item.GetComponent<HighlightableObject>();
                    if (highlightableObject != null)
                        highlightableObject.SetHighlight(false);
                }
            }
        }

        if (associatedBin != null)
            associatedBin.SetHighlight(false);
    }
}