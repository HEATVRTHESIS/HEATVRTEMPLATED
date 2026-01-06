using UnityEngine;
using UnityEngine.InputSystem;

public class FireExtinguisherController : CustomTaskController
{
    [Header("Fire Extinguisher Settings")]
    [Tooltip("The fire extinguisher object to highlight")]
    public HighlightableObject targetObject;
    
    [Header("Success Monitor")]
    [Tooltip("Reference to the FireExtinguishSuccessMonitor that will determine when task is complete")]
    public FireExtinguishSuccessMonitor successMonitor;
    
    [Header("Input")]
    [Tooltip("Input action for interacting with fire extinguisher")]
    public InputActionProperty useAction;
    
    [Header("UI")]
    public PopupManager popupManager;
    
    // Private variables
    private bool isBeingUsed = false;
    private bool hasSubscribedToSuccessMonitor = false;
    private bool taskCompletedByThisController = false;
    
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
        
        // IMPORTANT: Do NOT auto-find success monitor - it must be manually assigned
        // to prevent multiple controllers from sharing the same monitor
        if (successMonitor == null)
        {
            Debug.LogError($"FireExtinguisherController on {gameObject.name}: No FireExtinguishSuccessMonitor assigned! " +
                          "Each fire extinguisher controller MUST have its own dedicated success monitor assigned in the inspector.");
        }
        else
        {
            // Verify this monitor isn't already used by another controller
            var allControllers = FindObjectsOfType<FireExtinguisherController>();
            foreach (var controller in allControllers)
            {
                if (controller != this && controller.successMonitor == successMonitor)
                {
                    Debug.LogError($"DUPLICATE MONITOR DETECTED! Both {gameObject.name} and {controller.gameObject.name} " +
                                  "are using the same FireExtinguishSuccessMonitor. Each controller needs its own monitor!");
                }
            }
        }
        
        // Subscribe to success monitor
        SubscribeToSuccessMonitor();
    }
    
    void Update()
    {
        if (IsTaskCompleted()) return;
        
        // Handle fire extinguisher usage
        HandleFireExtinguisherUsage();
        
        // Ensure we're subscribed to success monitor
        if (!hasSubscribedToSuccessMonitor)
        {
            SubscribeToSuccessMonitor();
        }
    }
    
    /// <summary>
    /// Subscribe to the success monitor's completion event
    /// </summary>
    private void SubscribeToSuccessMonitor()
    {
        if (successMonitor != null && !hasSubscribedToSuccessMonitor)
        {
            hasSubscribedToSuccessMonitor = true;
            Debug.Log($"{gameObject.name}: Subscribed to FireExtinguishSuccessMonitor on {successMonitor.gameObject.name}");
        }
        else if (successMonitor == null)
        {
            Debug.LogWarning($"{gameObject.name}: FireExtinguishSuccessMonitor not found! Task completion will not work properly.");
        }
    }
    
    /// <summary>
    /// Handle fire extinguisher input and usage
    /// </summary>
    private void HandleFireExtinguisherUsage()
    {
        bool isPressed = useAction.action != null && useAction.action.IsPressed();
        
        if (isPressed && !isBeingUsed)
        {
            isBeingUsed = true;
            OnExtinguisherActivated();
        }
        else if (!isPressed && isBeingUsed)
        {
            isBeingUsed = false;
            OnExtinguisherDeactivated();
        }
    }
    
    /// <summary>
    /// Override the InitializeTask method
    /// </summary>
    public override void InitializeTask()
    {
        base.InitializeTask();
        Debug.Log($"Fire extinguisher task '{taskName}' on {gameObject.name} initialized.");
        
        // Do NOT auto-find - must be manually assigned
        if (successMonitor == null)
        {
            Debug.LogError($"{gameObject.name}: No success monitor assigned during initialization!");
        }
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

        Debug.Log($"Started fire extinguisher task '{taskName}' on {gameObject.name}.");
        
        if (popupManager != null)
        {
            popupManager.ShowMessage("Use the fire extinguisher to put out all fires!");
        }
    }

    /// <summary>
    /// Override the EndTask method
    /// </summary>
    public override void EndTask()
    {
        Debug.Log($"Ending fire extinguisher task '{taskName}' on {gameObject.name} and turning off highlights.");

        if (targetObject != null)
        {
            targetObject.SetHighlight(false);
        }
    }
    
    /// <summary>
    /// This method should be called by the FireExtinguishSuccessMonitor when all fires are extinguished
    /// </summary>
    public void OnAllFiresExtinguished()
    {
        // Prevent double-completion
        if (taskCompletedByThisController || IsTaskCompleted())
        {
            Debug.Log($"{gameObject.name}: Already completed, ignoring duplicate completion call.");
            return;
        }
        
        taskCompletedByThisController = true;
        CompleteTask();
        
        // Notify fire score tracker
        if (FireScoreTracker.Instance != null)
        {
            FireScoreTracker.Instance.OnFireExtinguisherCompleted();
        }
        
        OnExtinguisherCompleted();
    }
    
    // Event methods
    protected virtual void OnExtinguisherActivated()
    {
        Debug.Log($"{gameObject.name}: Fire extinguisher activated");
        if (popupManager != null)
        {
            popupManager.ShowMessage("Fire extinguisher activated!");
        }
    }
    
    protected virtual void OnExtinguisherDeactivated()
    {
        Debug.Log($"{gameObject.name}: Fire extinguisher deactivated");
    }
    
    protected virtual void OnExtinguisherCompleted()
    {
        Debug.Log($"{gameObject.name}: Fire extinguisher task completed - all fires extinguished!");
        if (popupManager != null)
        {
            popupManager.ShowMessage("All fires extinguished! Task completed!");
        }
    }
    
    /// <summary>
    /// Check if extinguisher is currently being used
    /// </summary>
    public bool IsBeingUsed()
    {
        return isBeingUsed;
    }
    
    /// <summary>
    /// Manually complete the task (for testing)
    /// </summary>
    [ContextMenu("Force Complete Task")]
    public void ForceCompleteTask()
    {
        if (!IsTaskCompleted() && !taskCompletedByThisController)
        {
            taskCompletedByThisController = true;
            CompleteTask();
            OnExtinguisherCompleted();
            if (FireScoreTracker.Instance != null)
            {
                FireScoreTracker.Instance.OnCorrectAction("Correct extinguisher selected");
            }
        }
    }
    
    /// <summary>
    /// Reset the task (useful for testing or restarting scenarios)
    /// </summary>
    [ContextMenu("Reset Task")]
    public void ResetTask()
    {
        // Reset completion flag
        taskCompletedByThisController = false;
        
        // Reset base task
        try
        {
            var resetMethod = GetType().BaseType.GetMethod("ResetTask", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (resetMethod != null)
            {
                resetMethod.Invoke(this, null);
            }
            else
            {
                InitializeTask();
            }
        }
        catch
        {
            InitializeTask();
        }
        
        // Reset success monitor if available
        if (successMonitor != null)
        {
            successMonitor.ResetMonitor();
        }
        
        // Restart highlighting if this was the active task
        if (targetObject != null && !IsTaskCompleted())
        {
            targetObject.SetHighlight(true);
        }
        
        Debug.Log($"Fire extinguisher task '{taskName}' on {gameObject.name} reset.");
    }
    
    void OnDisable()
    {
        if (useAction.action != null)
        {
            useAction.action.Disable();
        }
    }
    
    void OnDestroy()
    {
        // Clean up any subscriptions if needed
        hasSubscribedToSuccessMonitor = false;
    }
    
    // Visual debugging in Scene view
    void OnDrawGizmosSelected()
    {
        // Draw task state
        Gizmos.color = IsTaskCompleted() ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        
        // Draw connection to success monitor
        if (successMonitor != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, successMonitor.transform.position);
        }
        else
        {
            // Draw warning if no monitor assigned
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.3f);
        }
        
        // Show extinguisher usage state
        if (isBeingUsed)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 1f, Vector3.one * 0.5f);
        }
    }
}