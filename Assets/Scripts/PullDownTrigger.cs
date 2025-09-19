using UnityEngine;
using UnityEngine.Events;

public class PullDownTrigger : TaskController
{
    // Original PullDownTrigger fields
    private HingeJoint myHingeJoint; 
    public float successAngle = 92f; 
    public UnityEvent onPullSuccess;
    private bool _isPulled = false;

    // Additional task-specific fields
    [Header("Fire Alarm Settings")]
    [Tooltip("The fire alarm object to highlight (usually the main fire alarm GameObject)")]
    public HighlightableObject targetObject; // The fire alarm to highlight
    
    [Header("UI")]
    public PopupManager popupManager;


    private void Start()
    {
        // Original functionality
        myHingeJoint = GetComponent<HingeJoint>();
        if (myHingeJoint == null)
        {
            Debug.LogError("Hinge Joint not found on this GameObject. The script will not work.");
        }

        // Task controller functionality
        if (targetObject == null)
        {
            // Try to find HighlightableObject on this GameObject if not assigned
            targetObject = GetComponent<HighlightableObject>();
        }

        // Set totalItems to 1 for fire alarm task
        totalItems = 1;
    }

    /// <summary>
    /// Override the InitializeTask method from TaskController
    /// </summary>
    public new void InitializeTask()
    {
        // Don't call base.InitializeTask() since it tries to spawn objects
        // Instead, do fire alarm specific initialization
        
        // Set totalItems but DON'T invoke progress update yet
        // The UI will call UpdateInitialProgress after subscribing to events
        totalItems = 1;
        
        Debug.Log($"Fire alarm task '{taskName}' initialized. Required angle: {successAngle}°");
    }

    /// <summary>
    /// Called after UI is set up to update initial progress
    /// </summary>
    public void UpdateInitialProgress()
    {
        OnProgressUpdated.Invoke(0, totalItems);
    }

    private void Update()
    {
        // Check if the task hasn't been completed yet.
        if (!_isPulled && !IsTaskCompleted())
        {
            // Check if the current angle has reached the success threshold.
            if (myHingeJoint.angle >= successAngle - 5.0f)
            {
                // Original functionality
                onPullSuccess?.Invoke();
                _isPulled = true;

                // Complete the task
                CompleteFireAlarmTask();
            }
        }
    }

    /// <summary>
    /// Custom completion method for fire alarm task
    /// </summary>
    private void CompleteFireAlarmTask()
    {
        Debug.Log($"Fire alarm task '{taskName}' completed! Lever pulled to {myHingeJoint.angle:F1}°");
        
        // Mark as completed (using private field access)
        SetTaskCompleted();
        
        // Update progress to completed
        OnProgressUpdated.Invoke(1, totalItems);
        OnTaskCompleted.Invoke();
        
        // End highlighting
        EndTask();

        // Play the success sound
        if (audioSource != null && successSound != null)
        {
            audioSource.PlayOneShot(successSound);
        }
        else if (successSound != null)
        {
            // Fallback audio playback
            PlaySuccessSoundFallback();
        }

        // Show completion dialogue
        ShowCompletionDialogue();
        
        // Optional: Show completion message
        if (popupManager != null)
        {
            popupManager.ShowMessage($"Fire alarm activated! Lever pulled to {myHingeJoint.angle:F1}°");
        }
    }

    /// <summary>
    /// Sets the task as completed using reflection since isTaskCompleted is private
    /// </summary>
    private void SetTaskCompleted()
    {
        // Use reflection to set the private isTaskCompleted field
        var field = typeof(TaskController).GetField("isTaskCompleted", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(this, true);
        }
    }

    /// <summary>
    /// Check if task is completed using reflection since isTaskCompleted is private
    /// </summary>
    private bool IsTaskCompleted()
    {
        var field = typeof(TaskController).GetField("isTaskCompleted", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            return (bool)field.GetValue(this);
        }
        return false;
    }

    /// <summary>
    /// Fallback method to play success sound without AudioSource component
    /// </summary>
    private void PlaySuccessSoundFallback()
    {
        // Try to find an AudioSource on this GameObject or its children
        AudioSource foundAudioSource = GetComponentInChildren<AudioSource>();
        if (foundAudioSource != null)
        {
            foundAudioSource.PlayOneShot(successSound);
        }
        else
        {
            // Create a temporary AudioSource if none exists
            GameObject tempAudio = new GameObject("TempAudioSource");
            AudioSource tempSource = tempAudio.AddComponent<AudioSource>();
            tempSource.PlayOneShot(successSound);
            Destroy(tempAudio, successSound.length);
        }
    }

    /// <summary>
    /// Shows the completion dialogue using VRDialogueSystem
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
    /// Override the StartTask method from TaskController
    /// </summary>
    public override void StartTask()
{
    if (IsTaskCompleted()) return;

    if (targetObject != null)
    {
        targetObject.SetHighlight(true);
    }
    
    Debug.Log($"Started fire alarm task '{taskName}'. Current angle: {myHingeJoint.angle:F1}°");
}


    /// <summary>
    /// Override the EndTask method from TaskController
    /// </summary>
    public override void EndTask()
{
    Debug.Log($"Ending fire alarm task '{taskName}' and turning off highlights.");
    
    if (targetObject != null)
    {
        targetObject.SetHighlight(false);
    }
}

    // Utility methods specific to PullDownTrigger
    
    /// <summary>
    /// Get current pull progress (0-1)
    /// </summary>
    public float GetPullProgress()
    {
        if (myHingeJoint == null) return 0f;
        return Mathf.Clamp01(myHingeJoint.angle / successAngle);
    }

    /// <summary>
    /// Get current angle
    /// </summary>
    public float GetCurrentAngle()
    {
        return myHingeJoint != null ? myHingeJoint.angle : 0f;
    }

    /// <summary>
    /// Check if the lever has been pulled successfully
    /// </summary>
    public bool IsLeverPulled()
    {
        return _isPulled;
    }

    /// <summary>
    /// Force complete the task (for testing)
    /// </summary>
    [ContextMenu("Force Complete Task")]
    public void ForceCompleteTask()
    {
        _isPulled = true;
        CompleteFireAlarmTask();
    }

    // Visual debugging in Scene view
    void OnDrawGizmosSelected()
    {
        if (myHingeJoint != null)
        {
            // Draw pull progress visualization
            Gizmos.color = IsTaskCompleted() ? Color.green : (_isPulled ? Color.red : Color.yellow);
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            
            // Show pull progress
            float progress = GetPullProgress();
            Gizmos.color = Color.Lerp(Color.red, Color.green, progress);
            Gizmos.DrawWireCube(transform.position + Vector3.up * 1f, Vector3.one * 0.5f * progress);
        }
    }
}