using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class MaintenanceTaskController : MonoBehaviour
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

    public UnityEvent<int, int> OnProgressUpdated;
    public UnityEvent OnTaskCompleted;

    private int completedItems = 0;
    private int totalItems = 1;
    private bool isTaskCompleted = false;
    private bool isHovering = false;
    private bool hasPlayedErrorDialogue = false;

    [Header("Maintenance Task Fields")]
    public string questionText;
    public Sprite yesAnswerImage;
    public Sprite noAnswerImage;
    public bool isYesTheCorrectAnswer;
    public HighlightableObject targetObject;
    public GameObject targetGameObject;
    public GameObject magnifyingGlassIcon;

    public PopupManager popupManager;

    [Header("Input Mapping")]
    public InputActionProperty playerAction;

    void Awake()
    {
        if (magnifyingGlassIcon != null)
        {
            magnifyingGlassIcon.SetActive(false);
        }
        
        if (targetGameObject == null && targetObject != null)
        {
            targetGameObject = targetObject.gameObject;
        }
    }

    void OnEnable()
    {
        playerAction.action.performed += OnPlayerActionPerformed;
        playerAction.action.Enable();
    }
    
    void OnDisable()
    {
        playerAction.action.performed -= OnPlayerActionPerformed;
        playerAction.action.Disable();
    }

    public void InitializeTask()
    {
        OnProgressUpdated.Invoke(0, totalItems);
    }
    
    public void OnGazeEnter()
    {
        if (!isTaskCompleted && magnifyingGlassIcon != null)
        {
            isHovering = true;
            magnifyingGlassIcon.SetActive(true);
            if (TaskListManager.Instance != null)
            {
                TaskListManager.Instance.SelectMaintenanceTask(this);
            }
        }
    }
    
    public void OnGazeExit()
    {
        isHovering = false;
        if (magnifyingGlassIcon != null)
        {
            magnifyingGlassIcon.SetActive(false);
        }
    }

    private void OnPlayerActionPerformed(InputAction.CallbackContext context)
    {
        if (isHovering && 
            TaskListManager.Instance != null && TaskListManager.Instance.IsThisMaintenanceTaskSelected(this))
        {
            QuestionUIManager.Instance.ShowQuestion(this);
        }
    }

    public void AnswerQuestion(bool isCorrect)
    {
        if (isCorrect)
        {
            Debug.Log($"Task '{taskName}': Answered correctly!");
            isTaskCompleted = true;
            completedItems = 1;
            OnProgressUpdated.Invoke(completedItems, totalItems);
            OnTaskCompleted.Invoke();
            EndTask();

            if (audioSource != null && successSound != null)
            {
                audioSource.PlayOneShot(successSound);
            }

            VRDialogueSystem dialogueSystem = FindObjectOfType<VRDialogueSystem>();
            if (dialogueSystem != null && taskCompletionDialogue != null && taskCompletionDialogue.Length > 0)
            {
                dialogueSystem.StartDialog(taskCompletionDialogue);
            }
        }
        else
        {
            Debug.Log($"Task '{taskName}': Answered incorrectly.");
            
            if (popupManager != null)
            {
                popupManager.ShowMessage("That's not the right answer. Try again!");
                ScoreTracker.Instance.OnTaskError();
            }

            if (ErrorTracker.Instance != null)
                ErrorTracker.Instance.RecordMaintenanceError();

            if (audioSource != null && errorSound != null)
                audioSource.PlayOneShot(errorSound);

            if (!hasPlayedErrorDialogue)
            {
                hasPlayedErrorDialogue = true;
                VRDialogueSystem dialogueSystem = FindObjectOfType<VRDialogueSystem>();
                if (dialogueSystem != null && taskErrorDialogue != null && taskErrorDialogue.Length > 0)
                {
                    dialogueSystem.StartDialog(taskErrorDialogue);
                }
            }
        }
    }

    public void StartTask()
    {
        if (isTaskCompleted) return;

        if (targetObject != null && targetObject.enabled)
        {
            targetObject.SetHighlight(true);
        }
    }

    public void EndTask()
    {
        Debug.Log($"Ending task '{taskName}' and turning off highlights.");
        if (targetObject != null && targetObject.enabled)
        {
            targetObject.SetHighlight(false);
        }
    }
}