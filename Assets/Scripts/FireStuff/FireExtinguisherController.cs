using UnityEngine;
using UnityEngine.InputSystem;

public class FireExtinguisherController : CustomTaskController
{
    [Header("Fire Extinguisher Settings")]
    [Tooltip("The fire extinguisher object to highlight")]
    public HighlightableObject targetObject;
    
    [Tooltip("How the task gets completed")]
    public CompletionType completionType = CompletionType.Manual;
    
    [Header("Completion Conditions")]
    [Tooltip("Time required to hold trigger (for TimeBased completion)")]
    public float requiredHoldTime = 3f;
    
    [Tooltip("Target object to aim at (for TargetBased completion)")]
    public Transform targetLocation;
    
    [Tooltip("Distance required to target (for TargetBased completion)")]
    public float requiredDistance = 5f;
    
    [Header("Input")]
    [Tooltip("Input action for interacting with fire extinguisher")]
    public InputActionProperty useAction;
    
    [Header("UI")]
    public PopupManager popupManager;
    
    // Private variables
    private float currentHoldTime = 0f;
    private bool isBeingUsed = false;
    private bool taskStarted = false;
    
    // Completion types
    public enum CompletionType
    {
        Manual,        // Complete via script call or context menu
        TimeBased,     // Hold input for X seconds
        TargetBased,   // Get close to target location
        Activation     // Simple one-time activation
    }
    
    void Start()
    {
        // Task controller setup
        if (targetObject == null)
        {
            targetObject = GetComponent<HighlightableObject>();
        }
        
        // Set totalItems to 1 for fire extinguisher task
        totalItems = 1;
        
        // Enable input action
        if (useAction.action != null)
        {
            useAction.action.Enable();
        }
    }
    
    void Update()
    {
        if (IsTaskCompleted()) return;
        
        // Handle different completion types
        switch (completionType)
        {
            case CompletionType.TimeBased:
                HandleTimeBasedCompletion();
                break;
            case CompletionType.TargetBased:
                HandleTargetBasedCompletion();
                break;
            case CompletionType.Activation:
                HandleActivationCompletion();
                break;
            // Manual completion is handled via public methods
        }
    }
    
    /// <summary>
    /// Handle time-based completion (hold input for X seconds)
    /// </summary>
    private void HandleTimeBasedCompletion()
    {
        bool isPressed = useAction.action != null && useAction.action.IsPressed();
        
        if (isPressed)
        {
            if (!isBeingUsed)
            {
                isBeingUsed = true;
                OnExtinguisherActivated();
            }
            
            currentHoldTime += Time.deltaTime;
            
            if (currentHoldTime >= requiredHoldTime)
            {
                CompleteTask();
                OnExtinguisherCompleted();
            }
        }
        else
        {
            if (isBeingUsed)
            {
                isBeingUsed = false;
                OnExtinguisherDeactivated();
            }
            // Optionally reset timer if not held continuously
            // currentHoldTime = 0f; // Uncomment if you want to reset on release
        }
    }
    
    /// <summary>
    /// Handle target-based completion (get close to target)
    /// </summary>
    private void HandleTargetBasedCompletion()
    {
        if (targetLocation == null) return;
        
        float distance = Vector3.Distance(transform.position, targetLocation.position);
        
        if (distance <= requiredDistance)
        {
            if (!taskStarted)
            {
                taskStarted = true;
                OnTargetReached();
            }
            
            // Check if using extinguisher at target
            bool isPressed = useAction.action != null && useAction.action.IsPressed();
            if (isPressed)
            {
                CompleteTask();
                OnExtinguisherCompleted();
            }
        }
        else
        {
            if (taskStarted)
            {
                taskStarted = false;
                OnTargetLeft();
            }
        }
    }
    
    /// <summary>
    /// Handle simple activation completion
    /// </summary>
    private void HandleActivationCompletion()
    {
        bool wasPressed = useAction.action != null && useAction.action.WasPressedThisFrame();
        
        if (wasPressed)
        {
            CompleteTask();
            OnExtinguisherCompleted();
        }
    }
    
    /// <summary>
    /// Override the InitializeTask method
    /// </summary>
    public override void InitializeTask()
    {
        base.InitializeTask();
        Debug.Log($"Fire extinguisher task '{taskName}' initialized. Completion type: {completionType}");
    }
    
    /// <summary>
    /// Override the StartTask method
    /// </summary>
    public override void StartTask()
    {
        if (IsTaskCompleted()) return;

        if (targetObject != null)
        {
            targetObject.SetHighlight(true);
        }

        Debug.Log($"Started fire extinguisher task '{taskName}'.");
    }

    /// <summary>
    /// Override the EndTask method
    /// </summary>
    public override void EndTask()
    {
        Debug.Log($"Ending fire extinguisher task '{taskName}' and turning off highlights.");

        if (targetObject != null)
        {
            targetObject.SetHighlight(false);
        }
    }
    
    // Event methods - override these or use them for custom behavior
    protected virtual void OnExtinguisherActivated()
    {
        Debug.Log("Fire extinguisher activated");
        if (popupManager != null)
        {
            popupManager.ShowMessage("Fire extinguisher activated!");
        }
    }
    
    protected virtual void OnExtinguisherDeactivated()
    {
        Debug.Log("Fire extinguisher deactivated");
    }
    
    protected virtual void OnExtinguisherCompleted()
    {
        Debug.Log("Fire extinguisher task completed");
        if (popupManager != null)
        {
            popupManager.ShowMessage("Fire extinguisher task completed!");
        }
    }
    
    protected virtual void OnTargetReached()
    {
        Debug.Log("Target location reached");
        if (popupManager != null)
        {
            popupManager.ShowMessage("Target reached! Use the fire extinguisher.");
        }
    }
    
    protected virtual void OnTargetLeft()
    {
        Debug.Log("Target location left");
    }
    
    // Public methods for manual completion or external triggers
    
    /// <summary>
    /// Manually complete the fire extinguisher task
    /// </summary>
    public void ManuallyCompleteTask()
    {
        if (!IsTaskCompleted())
        {
            CompleteTask();
            OnExtinguisherCompleted();
        }
    }
    
    /// <summary>
    /// Get current progress for time-based tasks
    /// </summary>
    public float GetProgress()
    {
        switch (completionType)
        {
            case CompletionType.TimeBased:
                return Mathf.Clamp01(currentHoldTime / requiredHoldTime);
            case CompletionType.TargetBased:
                if (targetLocation != null)
                {
                    float distance = Vector3.Distance(transform.position, targetLocation.position);
                    return Mathf.Clamp01(1f - (distance / requiredDistance));
                }
                break;
        }
        return 0f;
    }
    
    /// <summary>
    /// Check if extinguisher is currently being used
    /// </summary>
    public bool IsBeingUsed()
    {
        return isBeingUsed;
    }
    
    /// <summary>
    /// Force complete the task (for testing)
    /// </summary>
    [ContextMenu("Force Complete Task")]
    public void ForceCompleteTask()
    {
        ManuallyCompleteTask();
    }
    
    void OnDisable()
    {
        if (useAction.action != null)
        {
            useAction.action.Disable();
        }
    }
    
    // Visual debugging in Scene view
    void OnDrawGizmosSelected()
    {
        // Draw completion progress
        Gizmos.color = IsTaskCompleted() ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        
        // Draw target location for target-based completion
        if (completionType == CompletionType.TargetBased && targetLocation != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetLocation.position, requiredDistance);
            Gizmos.DrawLine(transform.position, targetLocation.position);
        }
        
        // Show progress for time-based completion
        if (completionType == CompletionType.TimeBased)
        {
            float progress = GetProgress();
            Gizmos.color = Color.Lerp(Color.red, Color.green, progress);
            Gizmos.DrawWireCube(transform.position + Vector3.up * 1f, Vector3.one * 0.5f * progress);
        }
    }
}