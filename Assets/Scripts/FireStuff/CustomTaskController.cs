using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Base controller for custom tasks like fire alarms and NPC evacuations.
/// Separate from the main TaskController to avoid conflicts.
/// </summary>
public class CustomTaskController : MonoBehaviour
{
    [Header("Task Information")]
    public string taskName;
    public string taskDescription;
    
    [Tooltip("The dialogue lines to display when this task is completed.")]
    public string[] taskCompletionDialogue;
    
    [Header("Audio")]
    [Tooltip("The AudioSource component that will play the sound.")]
    public AudioSource audioSource;
    [Tooltip("The audio clip to play when the task is completed.")]
    public AudioClip successSound;

    // Task state tracking
    [HideInInspector] public int totalItems = 1; // Always 1 for custom tasks
    private int completedItems = 0;
    private bool isTaskCompleted = false;

    // Events to notify other scripts of progress and completion
    public UnityEvent<int, int> OnProgressUpdated;
    public UnityEvent OnTaskCompleted;

    /// <summary>
    /// Virtual method for initializing the task - override in derived classes
    /// </summary>
    public virtual void InitializeTask()
    {
        totalItems = 1;
    }

    /// <summary>
    /// Virtual method for starting task highlighting - override in derived classes
    /// </summary>
    public virtual void StartTask()
    {
        // Override in derived classes
    }

    /// <summary>
    /// Virtual method for ending task highlighting - override in derived classes
    /// </summary>
    public virtual void EndTask()
    {
        // Override in derived classes
    }

    /// <summary>
    /// Call this when the custom task is completed
    /// </summary>
    protected void CompleteTask()
    {
        if (!isTaskCompleted)
        {
            isTaskCompleted = true;
            completedItems = 1;
            
            // Update progress and notify completion
            OnProgressUpdated.Invoke(completedItems, totalItems);
            OnTaskCompleted.Invoke();
            
            // End highlighting
            EndTask();

            // Play success sound
            PlaySuccessSound();

            // Show completion dialogue
            ShowCompletionDialogue();
            
            Debug.Log($"Custom task '{taskName}' completed!");
        }
    }

    /// <summary>
    /// Play the success sound
    /// </summary>
    private void PlaySuccessSound()
    {
        if (audioSource != null && successSound != null)
        {
            audioSource.PlayOneShot(successSound);
        }
        else if (successSound != null)
        {
            // Fallback audio playback
            AudioSource foundAudioSource = GetComponentInChildren<AudioSource>();
            if (foundAudioSource != null)
            {
                foundAudioSource.PlayOneShot(successSound);
            }
            else
            {
                GameObject tempAudio = new GameObject("TempAudioSource");
                AudioSource tempSource = tempAudio.AddComponent<AudioSource>();
                tempSource.PlayOneShot(successSound);
                Destroy(tempAudio, successSound.length);
            }
        }
    }

    /// <summary>
    /// Show completion dialogue
    /// </summary>
    private void ShowCompletionDialogue()
    {
        VRDialogueSystem dialogueSystem = FindObjectOfType<VRDialogueSystem>();
        if (dialogueSystem != null && taskCompletionDialogue != null && taskCompletionDialogue.Length > 0)
        {
            dialogueSystem.StartDialog(taskCompletionDialogue);
        }
    }

    /// <summary>
    /// Check if task is completed
    /// </summary>
    public bool IsTaskCompleted()
    {
        return isTaskCompleted;
    }

    /// <summary>
    /// Called after UI is set up to update initial progress
    /// </summary>
    public virtual void UpdateInitialProgress()
    {
        OnProgressUpdated.Invoke(0, totalItems);
    }
}