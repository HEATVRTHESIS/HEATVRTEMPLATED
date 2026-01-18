using UnityEngine;
using UnityEngine.InputSystem;

public class FireExtinguisherController : CustomTaskController
{
    [Header("Fire Extinguisher Settings")]
    public HighlightableObject targetObject;
    
    [Header("Success Monitor")]
    public FireExtinguishSuccessMonitor successMonitor;
    
    [Header("Input")]
    public InputActionProperty useAction;
    
    [Header("Error Dialogue")]
    [Tooltip("The dialogue lines to display if task incomplete when timer expires.")]
    public string[] taskErrorDialogue;

    [Tooltip("The dialogue lines to display when wrong extinguisher is used.")]
    public string[] wrongExtinguisherDialogue;

    [Header("Audio")]
    public AudioClip errorSound;
    public AudioClip wrongExtinguisherSound;
    
    [Header("UI")]
    public PopupManager popupManager;
    
    private bool isBeingUsed = false;
    private bool hasSubscribedToSuccessMonitor = false;
    private bool taskCompletedByThisController = false;
    private bool hasPlayedWrongExtinguisherDialogue = false;
    
    void Start()
    {
        if (targetObject == null)
        {
            targetObject = GetComponent<HighlightableObject>();
        }
        
        totalItems = 1;
        
        if (useAction.action != null)
        {
            useAction.action.Enable();
        }
        
        if (successMonitor == null)
        {
            Debug.LogError($"FireExtinguisherController on {gameObject.name}: No FireExtinguishSuccessMonitor assigned!");
        }
        else
        {
            var allControllers = FindObjectsOfType<FireExtinguisherController>();
            foreach (var controller in allControllers)
            {
                if (controller != this && controller.successMonitor == successMonitor)
                {
                    Debug.LogError($"DUPLICATE MONITOR DETECTED! Both {gameObject.name} and {controller.gameObject.name} using same monitor!");
                }
            }
        }
        
        SubscribeToSuccessMonitor();
    }
    
    void Update()
    {
        if (IsTaskCompleted()) return;
        
        HandleFireExtinguisherUsage();
        
        if (!hasSubscribedToSuccessMonitor)
        {
            SubscribeToSuccessMonitor();
        }
    }
    
    private void SubscribeToSuccessMonitor()
    {
        if (successMonitor != null && !hasSubscribedToSuccessMonitor)
        {
            hasSubscribedToSuccessMonitor = true;
        }
        else if (successMonitor == null)
        {
            Debug.LogWarning($"{gameObject.name}: FireExtinguishSuccessMonitor not found!");
        }
    }
    
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
    
    public override void InitializeTask()
    {
        base.InitializeTask();
        
        if (successMonitor == null)
        {
            Debug.LogError($"{gameObject.name}: No success monitor assigned during initialization!");
        }
    }
    
    public override void StartTask()
    {
        if (IsTaskCompleted()) return;

        if (targetObject != null)
        {
            targetObject.SetHighlight(true);
        }

        if (popupManager != null)
        {
            popupManager.ShowMessage("Use the fire extinguisher to put out all fires!");
        }
    }

    public override void EndTask()
    {
        if (targetObject != null)
        {
            targetObject.SetHighlight(false);
        }
    }
    
    public void OnAllFiresExtinguished()
    {
        if (taskCompletedByThisController || IsTaskCompleted())
        {
            return;
        }
        
        taskCompletedByThisController = true;
        CompleteTask();
        
        if (FireScoreTracker.Instance != null)
        {
            FireScoreTracker.Instance.OnFireExtinguisherCompleted();
        }
        
        OnExtinguisherCompleted();
    }

    /// <summary>
    /// Called when timer expires and this task is incomplete
    /// </summary>
    public void OnTimerExpiredIncomplete()
    {
        if (IsTaskCompleted()) return;

        if (ErrorTracker.Instance != null)
            ErrorTracker.Instance.RecordFireExtinguisherError();

        if (audioSource != null && errorSound != null)
            audioSource.PlayOneShot(errorSound);

        VRDialogueSystem dialogueSystem = FindObjectOfType<VRDialogueSystem>();
        if (dialogueSystem != null && taskErrorDialogue != null && taskErrorDialogue.Length > 0)
        {
            dialogueSystem.StartDialog(taskErrorDialogue);
        }
    }

    /// <summary>
    /// Called when wrong extinguisher type is used
    /// </summary>
    public void OnWrongExtinguisherUsed()
    {
        if (ErrorTracker.Instance != null)
            ErrorTracker.Instance.RecordFireWrongExtinguisherError();

        if (audioSource != null && wrongExtinguisherSound != null)
            audioSource.PlayOneShot(wrongExtinguisherSound);

        if (!hasPlayedWrongExtinguisherDialogue)
        {
            hasPlayedWrongExtinguisherDialogue = true;
            VRDialogueSystem dialogueSystem = FindObjectOfType<VRDialogueSystem>();
            if (dialogueSystem != null && wrongExtinguisherDialogue != null && wrongExtinguisherDialogue.Length > 0)
            {
                dialogueSystem.StartDialog(wrongExtinguisherDialogue);
            }
        }

        if (popupManager != null)
        {
            popupManager.ShowMessage("Wrong extinguisher type! Check the fire class.");
        }
    }
    
    protected virtual void OnExtinguisherActivated()
    {
        if (popupManager != null)
        {
            popupManager.ShowMessage("Fire extinguisher activated!");
        }
    }
    
    protected virtual void OnExtinguisherDeactivated()
    {
    }
    
    protected virtual void OnExtinguisherCompleted()
    {
        if (popupManager != null)
        {
            popupManager.ShowMessage("All fires extinguished! Task completed!");
        }
    }
    
    public bool IsBeingUsed()
    {
        return isBeingUsed;
    }
    
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
    
    [ContextMenu("Reset Task")]
    public void ResetTask()
    {
        taskCompletedByThisController = false;
        
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
        
        if (successMonitor != null)
        {
            successMonitor.ResetMonitor();
        }
        
        if (targetObject != null && !IsTaskCompleted())
        {
            targetObject.SetHighlight(true);
        }
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
        hasSubscribedToSuccessMonitor = false;
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = IsTaskCompleted() ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        
        if (successMonitor != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, successMonitor.transform.position);
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.3f);
        }
        
        if (isBeingUsed)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 1f, Vector3.one * 0.5f);
        }
    }
}