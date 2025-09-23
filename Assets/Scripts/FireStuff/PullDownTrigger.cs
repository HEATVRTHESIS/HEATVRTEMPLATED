using UnityEngine;
using UnityEngine.Events;

public class PullDownTrigger : CustomTaskController
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
    public override void InitializeTask()
    {
        base.InitializeTask();
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
                CompleteTask();
                FireScoreTracker.Instance.OnSafetyCompliance("Fire alarm activated");
            }
        }
    }
    
    /// <summary>
    /// Override the StartTask method from TaskController
    /// </summary>
    public override void StartTask()
{
    Debug.Log($"PullDownTrigger.StartTask() called. IsCompleted: {IsTaskCompleted()}");
    Debug.Log($"Lever targetObject is: {(targetObject != null ? targetObject.name : "null")}");
    
    if (IsTaskCompleted()) 
    {
        Debug.Log("Lever task already completed, returning without highlighting");
        return;
    }

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
        Debug.Log($"PullDownTrigger.EndTask() called for '{taskName}'");
        Debug.Log($"targetObject is: {(targetObject != null ? targetObject.name : "null")}");

        if (targetObject != null)
        {
            Debug.Log($"Calling SetHighlight(false) on {targetObject.name}");
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
        CompleteTask();

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