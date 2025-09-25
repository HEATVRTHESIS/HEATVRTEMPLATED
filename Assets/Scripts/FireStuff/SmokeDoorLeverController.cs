using UnityEngine;
using UnityEngine.Events;

public class SmokeDoorLeverController : CustomTaskController
{
    [Header("Lever Settings")]
    private HingeJoint myHingeJoint; 
    public float successAngle = 92f; 
    public UnityEvent onPullSuccess;
    private bool _isPulled = false;

    [Header("Smoke Door Settings")]
    [Tooltip("The door lever object to highlight")]
    public HighlightableObject targetObject;
    
    [Header("Task Configuration")]
    [Tooltip("Number of items required to complete this task")]
    public int requiredItems = 2;
    
    [Header("UI")]
    public PopupManager popupManager;

    private void Start()
    {
        // Lever functionality
        myHingeJoint = GetComponent<HingeJoint>();
        if (myHingeJoint == null)
        {
            Debug.LogError("Hinge Joint not found on this GameObject. The script will not work.");
        }

        // Task controller functionality
        if (targetObject == null)
        {
            targetObject = GetComponent<HighlightableObject>();
        }
    }

    /// <summary>
    /// Override the InitializeTask method from CustomTaskController
    /// </summary>
    public override void InitializeTask()
    {
        totalItems = requiredItems;
        base.InitializeTask();
        Debug.Log($"Smoke door task '{taskName}' initialized. Required items: {requiredItems}");
    }

    /// <summary>
    /// Override UpdateInitialProgress to ensure proper UI display
    /// </summary>
    public override void UpdateInitialProgress()
    {
        Debug.Log($"SmokeDoorLever UpdateInitialProgress called. totalItems = {totalItems}");
        OnProgressUpdated.Invoke(0, totalItems);
    }

    private void Update()
    {
        // Check if the task hasn't been completed yet
        if (!_isPulled && !IsTaskCompleted())
        {
            // Check if the current angle has reached the success threshold
            if (myHingeJoint.angle >= successAngle - 5.0f)
            {
                // Original functionality
                onPullSuccess?.Invoke();
                _isPulled = true;

                // Complete the task
                CompleteTask();
            }
        }
    }
    
    /// <summary>
    /// Override the StartTask method from CustomTaskController
    /// </summary>
    public override void StartTask()
    {
        Debug.Log($"SmokeDoorLever.StartTask() called. IsCompleted: {IsTaskCompleted()}");
        Debug.Log($"Door lever targetObject is: {(targetObject != null ? targetObject.name : "null")}");
        
        if (IsTaskCompleted()) 
        {
            Debug.Log("Smoke door task already completed, returning without highlighting");
            return;
        }

        if (targetObject != null)
        {
            targetObject.SetHighlight(true);
        }

        Debug.Log($"Started smoke door task '{taskName}'. Current angle: {myHingeJoint.angle:F1}°");
    }

    /// <summary>
    /// Override the EndTask method from CustomTaskController
    /// </summary>
    public override void EndTask()
    {
        Debug.Log($"SmokeDoorLever.EndTask() called for '{taskName}'");
        Debug.Log($"targetObject is: {(targetObject != null ? targetObject.name : "null")}");

        if (targetObject != null)
        {
            Debug.Log($"Calling SetHighlight(false) on {targetObject.name}");
            targetObject.SetHighlight(false);
        }
    }

    // Utility methods specific to SmokeDoorLeverController
    
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