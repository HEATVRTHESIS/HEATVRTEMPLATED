using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Manages the highlighting, progress, and question-answering for a specific maintenance task.
/// Attach this script to the root of your prefab for maintenance tasks.
/// </summary>
public class MaintenanceTaskController : MonoBehaviour
{
    // Public fields to define the task
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

    // Events to notify other scripts of progress and completion
    public UnityEvent<int, int> OnProgressUpdated;
    public UnityEvent OnTaskCompleted;

    // A private counter for completed items (always 1 for maintenance tasks)
    private int completedItems = 0;
    
    // A private total items (always 1 for maintenance tasks)
    private int totalItems = 1;
    private bool isTaskCompleted = false;

    // Maintenance task-specific fields
    [Header("Maintenance Task Fields")]
    public string questionText;
    public Sprite yesAnswerImage;
    public Sprite noAnswerImage;
    public bool isYesTheCorrectAnswer;
    public HighlightableObject targetObject; // The object to look at (e.g., the fire extinguisher)
    public GameObject magnifyingGlassIcon;

    // A reference to the PopupManager, consistent with your Bin.cs
    public PopupManager popupManager;

    // Add a public InputAction reference for the button you want to map.
    [Header("Input Mapping")]
    public InputActionProperty playerAction;
    
    void Awake()
    {
        // Initially hide the magnifying glass icon
        if (magnifyingGlassIcon != null)
        {
            magnifyingGlassIcon.SetActive(false);
        }
    }

    void OnEnable()
    {
        // Subscribe to the action's 'performed' event.
        // This method will be called whenever the button is pressed.
        playerAction.action.performed += OnPlayerActionPerformed;
        playerAction.action.Enable();
    }
    
    void OnDisable()
    {
        // Unsubscribe to prevent memory leaks and unexpected behavior.
        playerAction.action.performed -= OnPlayerActionPerformed;
        playerAction.action.Disable();
    }

    /// <summary>
    /// This is the entry point for the task, called by the TaskListManager.
    /// </summary>
    public void InitializeTask()
    {
        OnProgressUpdated.Invoke(0, totalItems);
    }
    
    // Public methods for VR interactions
    public void OnGazeEnter()
    {
        if (!isTaskCompleted && magnifyingGlassIcon != null)
        {
            magnifyingGlassIcon.SetActive(true);
        }
    }
    
    public void OnGazeExit()
    {
        if (magnifyingGlassIcon != null)
        {
            magnifyingGlassIcon.SetActive(false);
        }
    }

    /// <summary>
    /// This method is called by the InputAction when the button is pressed.
    /// </summary>
    private void OnPlayerActionPerformed(InputAction.CallbackContext context)
    {
        // Only show the question if the player is currently gazing at the object.
        if (magnifyingGlassIcon != null && magnifyingGlassIcon.activeSelf)
        {
            MaintenanceUIManager.Instance.ShowQuestion(this);
        }
    }

    public void AnswerQuestion(bool isYesAnswer)
    {
        bool isCorrect = (isYesAnswer == isYesTheCorrectAnswer);

        if (isCorrect)
        {
            Debug.Log($"Task '{taskName}': Answered correctly!");
            isTaskCompleted = true;
            completedItems = 1;
            OnProgressUpdated.Invoke(completedItems, totalItems);
            OnTaskCompleted.Invoke();
            EndTask();

            // Play the success sound
            if (audioSource != null && successSound != null)
            {
                audioSource.PlayOneShot(successSound);
            }

            // Get the VRDialogueSystem instance and display the completion dialogue
            // We find the object in the scene, assuming there's only one.
            VRDialogueSystem dialogueSystem = FindObjectOfType<VRDialogueSystem>();
            if (dialogueSystem != null && taskCompletionDialogue != null && taskCompletionDialogue.Length > 0)
            {
                // The correct method to call is StartDialog.
                dialogueSystem.StartDialog(taskCompletionDialogue);
            }
        }
        else
        {
            Debug.Log($"Task '{taskName}': Answered incorrectly.");
            if (popupManager != null)
            {
                popupManager.ShowMessage("That's not the right answer. Try again!");
            }
        }
    }

    /// <summary>
    /// Starts the highlighting for this task.
    /// </summary>
    public void StartTask()
    {
        if (isTaskCompleted) return;

        if (targetObject != null)
        {
            targetObject.SetHighlight(true);
        }
    }

    /// <summary>
    /// Ends the highlighting for this task.
    /// </summary>
    public void EndTask()
    {
        Debug.Log($"Ending task '{taskName}' and turning off highlights.");
        if (targetObject != null)
        {
            targetObject.SetHighlight(false);
        }
        if (magnifyingGlassIcon != null)
        {
            magnifyingGlassIcon.SetActive(false);
        }
    }
}