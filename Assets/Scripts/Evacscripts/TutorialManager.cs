using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages the entire tutorial sequence for the VR game.
/// Handles progression through various tutorial stages with conditions and triggers.
/// NOW WITH SAVE FUNCTIONALITY - skips tutorial if already completed.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    [Header("System References")]
    [Tooltip("Reference to the VRDialogueSystem")]
    [SerializeField] private VRDialogueSystem dialogueSystem;
    
    [Tooltip("Reference to the DialogueMediaTrigger for showing images")]
    [SerializeField] private DialogueMediaTrigger mediaTrigger;
    
    [Tooltip("The GameObject with your movement component (e.g., ActionBasedContinuousMoveProvider, ContinuousMoveProviderBase, or any XR movement script). Will be disabled during teleport tutorial.")]
    [SerializeField] private MonoBehaviour movementProvider;
    
    [Header("Tutorial Stages - Triggers")]
    [Tooltip("Trigger zone the player must enter to learn movement")]
    [SerializeField] private TutorialTriggerZone movementTriggerZone;
    
    [Tooltip("Platform the player must teleport to")]
    [SerializeField] private TutorialTeleportZone teleportZone;
    
    [Tooltip("Reference to the investigation object for X button tutorial")]
    [SerializeField] private GameObject investigationObject;
    
    [Tooltip("Reference to the storage task for grip tutorial")]
    [SerializeField] private StorageTaskController grippingTask;
    
    [Header("Input Actions")]
    [Tooltip("Input action for thumbstick turning (primary2DAxis) - assign the same action that detects all directions")]
    public InputActionProperty thumbstickAction;
    
    [Tooltip("Input action for opening task list (Y button / primaryButton)")]
    public InputActionProperty openTaskListAction;
    
    [Tooltip("Input action for player movement (assign the 'Move' action from your Input Action Manager - will be disabled during teleport tutorial)")]
    public InputActionProperty moveAction;
    
    [Header("Tutorial Stage Flags")]
    private bool hasStartedTutorial = false;
    private bool hasLearnedDialogueProgression = false;
    private bool hasEnteredMovementZone = false;
    private bool hasLearnedTurning = false;
    private bool hasLearnedTeleportation = false;
    private bool hasLearnedTaskList = false;
    private bool hasLearnedInvestigation = false;
    private bool hasLearnedGripping = false;
    private bool tutorialComplete = false;
    
    private int turningStep = 0; // 0 = not started, 1 = right, 2 = left, 3 = down, 4 = complete
    
    private bool movementWasEnabled = true; // Track if movement provider was enabled before we disabled it
    
    void Start()
    {
        if (dialogueSystem == null)
        {
            Debug.LogError("TutorialManager: VRDialogueSystem reference is not set!");
            enabled = false;
            return;
        }
        
        // Check if tutorial has already been completed
        if (TutorialSaveData.Instance != null && TutorialSaveData.Instance.IsTutorialCompleted())
        {
            Debug.Log("Tutorial already completed - skipping tutorial");
            SkipTutorialSequence();
            return;
        }
        
        // Subscribe to input actions
        if (thumbstickAction.action != null)
        {
            thumbstickAction.action.performed += OnThumbstickPerformed;
        }
        
        if (openTaskListAction.action != null)
        {
            openTaskListAction.action.performed += OnOpenTaskListPerformed;
        }
        
        // Subscribe to trigger events
        if (movementTriggerZone != null)
        {
            movementTriggerZone.OnPlayerEntered += HandleMovementZoneEntered;
        }
        
        if (teleportZone != null)
        {
            teleportZone.OnPlayerTeleported += HandleTeleportCompleted;
        }
        
        if (grippingTask != null)
        {
            grippingTask.OnTaskCompleted.AddListener(HandleGrippingTaskCompleted);
        }
        
        // Start the tutorial
        StartTutorial();
    }
    
    /// <summary>
    /// Skip the tutorial sequence - disables all tutorial elements
    /// </summary>
    void SkipTutorialSequence()
    {
        tutorialComplete = true;
        
        // Disable all tutorial trigger zones
        if (movementTriggerZone != null)
        {
            movementTriggerZone.gameObject.SetActive(false);
        }
        
        if (teleportZone != null)
        {
            teleportZone.gameObject.SetActive(false);
        }
        
        if (investigationObject != null)
        {
            HighlightableObject highlight = investigationObject.GetComponent<HighlightableObject>();
            if (highlight != null)
            {
                highlight.SetHighlight(false);
            }
        }
        
        if (grippingTask != null)
        {
            grippingTask.gameObject.SetActive(false);
        }
        
        // Make sure movement is enabled
        if (moveAction.action != null)
        {
            moveAction.action.Enable();
        }
        
        if (movementProvider != null)
        {
            movementProvider.enabled = true;
        }
        
        // Disable this script since tutorial is done
        this.enabled = false;
    }
    
    void OnEnable()
    {
        // Enable all input actions
        if (thumbstickAction.action != null)
        {
            thumbstickAction.action.Enable();
        }
        
        if (openTaskListAction.action != null)
        {
            openTaskListAction.action.Enable();
        }
    }
    
    void OnDisable()
    {
        // Disable all input actions
        if (thumbstickAction.action != null)
        {
            thumbstickAction.action.Disable();
        }
        
        if (openTaskListAction.action != null)
        {
            openTaskListAction.action.Disable();
        }
    }
    
    // Track last thumbstick values to detect new inputs
    private Vector2 lastThumbstickValue = Vector2.zero;
    private float thumbstickThreshold = 0.7f;
    private bool rightDetected = false;
    private bool leftDetected = false;
    private bool downDetected = false;
    
    /// <summary>
    /// Called when thumbstick input action is performed (primary2DAxis)
    /// Detects which direction and calls appropriate method
    /// </summary>
    private void OnThumbstickPerformed(InputAction.CallbackContext context)
    {
        Vector2 thumbstick = context.ReadValue<Vector2>();
        
        // Only detect during turning tutorial stage
        if (!hasEnteredMovementZone || hasLearnedTurning)
        {
            return;
        }
        
        // Detect RIGHT direction (for step 1)
        if (turningStep == 1 && thumbstick.x > thumbstickThreshold && !rightDetected)
        {
            rightDetected = true;
            OnTurnedRight();
        }
        else if (thumbstick.x < thumbstickThreshold * 0.5f)
        {
            rightDetected = false;
        }
        
        // Detect LEFT direction (for step 2)
        if (turningStep == 2 && thumbstick.x < -thumbstickThreshold && !leftDetected)
        {
            leftDetected = true;
            OnTurnedLeft();
        }
        else if (thumbstick.x > -thumbstickThreshold * 0.5f)
        {
            leftDetected = false;
        }
        
        // Detect DOWN direction (for step 3)
        if (turningStep == 3 && thumbstick.y < -thumbstickThreshold && !downDetected)
        {
            downDetected = true;
            OnTurnedDown();
        }
        else if (thumbstick.y > -thumbstickThreshold * 0.5f)
        {
            downDetected = false;
        }
    }
    
    /// <summary>
    /// Called when open task list input action is performed
    /// </summary>
    private void OnOpenTaskListPerformed(InputAction.CallbackContext context)
    {
        HandleTaskListOpened();
    }
    
    /// <summary>
    /// Starts the tutorial sequence
    /// </summary>
    void StartTutorial()
    {
        hasStartedTutorial = true;
        
        string[] introLines = new string[]
        {
            "Welcome to the HEAT VR Game.",
            "",
            "Today you will be learning protocols when there is a fire.",
            "All of this simulation facility will help you provide crucial knowledge",
            "regarding what to do when there's a fire.",
            "",
            "This tutorial will teach you the basic controls.",
            "Press the B button on your right controller to continue the dialogue."
        };
        
        dialogueSystem.StartDialog(introLines);
        
        // Wait for dialogue to finish, then check if player learned progression
        StartCoroutine(WaitForDialogueProgression());
    }
    
    /// <summary>
    /// Waits for the player to progress through the initial dialogue
    /// </summary>
    IEnumerator WaitForDialogueProgression()
    {
        // Wait until dialogue is no longer active (player finished it)
        yield return new WaitUntil(() => !dialogueSystem.IsDialogueActive());
        
        hasLearnedDialogueProgression = true;
        
        // Start movement tutorial
        StartMovementTutorial();
    }
    
    /// <summary>
    /// Starts the movement tutorial stage
    /// </summary>
    void StartMovementTutorial()
    {
        string[] movementLines = new string[]
        {
            "Great! You've learned how to progress through dialogue.",
            "",
            "Now let's learn how to move around.",
            "Please walk to the yellow highlighted area to continue."
        };
        
        dialogueSystem.StartDialog(movementLines);
        
        // Enable/highlight the movement trigger zone
        if (movementTriggerZone != null)
        {
            movementTriggerZone.gameObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// Called when player enters the movement trigger zone
    /// </summary>
    void HandleMovementZoneEntered()
    {
        if (hasEnteredMovementZone) return;
        
        hasEnteredMovementZone = true;
        
        // Disable the movement zone
        if (movementTriggerZone != null)
        {
            movementTriggerZone.gameObject.SetActive(false);
        }
        
        StartTurningTutorial();
    }
    
    /// <summary>
    /// Starts the turning tutorial
    /// </summary>
    void StartTurningTutorial()
    {
        turningStep = 1; // Start with right turn
        
        string[] turningLines = new string[]
        {
            "Perfect! You know how to move.",
            "",
            "Now let's learn how to turn.",
            "Push the right thumbstick to the RIGHT to turn right."
        };
        
        dialogueSystem.StartDialog(turningLines);
    }
    
    /// <summary>
    /// Called when player turns right
    /// PUBLIC so TutorialInputHelper can call it
    /// </summary>
    public void OnTurnedRight()
    {
        if (turningStep != 1) return;
        
        turningStep = 2; // Move to left turn
        
        string[] lines = new string[]
        {
            "Good! Now push the right thumbstick to the LEFT to turn left."
        };
        
        dialogueSystem.StartDialog(lines);
    }
    
    /// <summary>
    /// Called when player turns left
    /// PUBLIC so TutorialInputHelper can call it
    /// </summary>
    public void OnTurnedLeft()
    {
        if (turningStep != 2) return;
        
        turningStep = 3; // Move to turn around
        
        string[] lines = new string[]
        {
            "Excellent! Now push the right thumbstick DOWN to turn around."
        };
        
        dialogueSystem.StartDialog(lines);
    }
    
    /// <summary>
    /// Called when player turns down (around)
    /// PUBLIC so TutorialInputHelper can call it
    /// </summary>
    public void OnTurnedDown()
    {
        if (turningStep != 3) return;
        
        turningStep = 4; // Complete
        hasLearnedTurning = true;
        
        StartTeleportationTutorial();
    }
    
    /// <summary>
    /// Starts the teleportation tutorial
    /// </summary>
    void StartTeleportationTutorial()
    {
        string[] teleportLines = new string[]
        {
            "Great! You've mastered turning.",
            "",
            "Now let's learn about teleportation.",
            "Look for the highlighted platform and use your controller",
            "to teleport to it by aiming and releasing the trigger."
        };
        
        dialogueSystem.StartDialog(teleportLines);
        
        // DISABLE walking movement input to force teleportation
        if (moveAction.action != null)
        {
            movementWasEnabled = moveAction.action.enabled;
            moveAction.action.Disable();
            Debug.Log("Tutorial: Move input DISABLED - player must use teleportation");
        }
        else
        {
            Debug.LogError("Tutorial: Move Action not assigned! Player can still walk. Assign the 'Move' action from your Input Actions.");
        }
        
        // Also try disabling the movement provider component if assigned
        if (movementProvider != null)
        {
            movementProvider.enabled = false;
            Debug.Log("Tutorial: Movement Provider component also disabled");
        }
        
        // Enable the teleport zone
        if (teleportZone != null)
        {
            teleportZone.gameObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// Called when player successfully teleports to the platform
    /// </summary>
    void HandleTeleportCompleted()
    {
        if (hasLearnedTeleportation) return;
        
        hasLearnedTeleportation = true;
        
        // RE-ENABLE walking movement input
        if (moveAction.action != null && movementWasEnabled)
        {
            moveAction.action.Enable();
            Debug.Log("Tutorial: Move input re-enabled");
        }
        
        // Re-enable movement provider if it was assigned
        if (movementProvider != null)
        {
            movementProvider.enabled = true;
            Debug.Log("Tutorial: Movement Provider re-enabled");
        }
        
        // Disable the teleport zone
        if (teleportZone != null)
        {
            teleportZone.gameObject.SetActive(false);
        }
        
        StartTaskListTutorial();
    }
    
    /// <summary>
    /// Starts the task list tutorial
    /// </summary>
    void StartTaskListTutorial()
    {
        string[] taskListLines = new string[]
        {
            "Excellent teleportation!",
            "",
            "During gameplay, you'll need to complete various tasks.",
            "Press the Y button on your left controller to open the task list."
        };
        
        dialogueSystem.StartDialog(taskListLines);
    }
    
    /// <summary>
    /// Called when player opens the task list
    /// </summary>
    void HandleTaskListOpened()
    {
        // Only proceed if we're at the right stage
        if (!hasLearnedTeleportation || hasLearnedTaskList) return;
        
        hasLearnedTaskList = true;
        StartInvestigationTutorial();
    }
    
    /// <summary>
    /// Starts the investigation tutorial
    /// </summary>
    void StartInvestigationTutorial()
    {
        string[] investigationLines = new string[]
        {
            "Perfect! The task list shows your objectives.",
            "",
            "Now let's learn about investigation.",
            "Look at the highlighted object and press the X button",
            "on your left controller to investigate it.",
            "Then answer the question correctly to proceed."
        };
        
        dialogueSystem.StartDialog(investigationLines);
        
        // Enable/highlight the investigation object
        if (investigationObject != null)
        {
            HighlightableObject highlight = investigationObject.GetComponent<HighlightableObject>();
            if (highlight != null)
            {
                highlight.SetHighlight(true);
            }
        }
    }
    
    /// <summary>
    /// Called when player successfully investigates and answers correctly
    /// Must be called from the investigation system's correct answer button
    /// </summary>
    public void HandleInvestigationCompleted()
    {
        if (hasLearnedInvestigation) return;
        
        hasLearnedInvestigation = true;
        
        // Disable highlight on investigation object
        if (investigationObject != null)
        {
            HighlightableObject highlight = investigationObject.GetComponent<HighlightableObject>();
            if (highlight != null)
            {
                highlight.SetHighlight(false);
            }
        }
        
        StartGrippingTutorial();
    }
    
    /// <summary>
    /// Starts the gripping tutorial
    /// </summary>
    void StartGrippingTutorial()
    {
        string[] grippingLines = new string[]
        {
            "Well done on your investigation!",
            "",
            "Now let's learn how to grab and store items.",
            "Look at the highlighted cube on your right.",
            "Hold the grip button on your left or right controller to grab it,",
            "then place it in the highlighted box to complete the task."
        };
        
        dialogueSystem.StartDialog(grippingLines);
        
        // Initialize the gripping task
        if (grippingTask != null)
        {
            grippingTask.InitializeTask();
            grippingTask.StartTask();
        }
    }
    
    /// <summary>
    /// Called when the gripping task is completed
    /// </summary>
    void HandleGrippingTaskCompleted()
    {
        if (hasLearnedGripping) return;
        
        hasLearnedGripping = true;
        CompleteTutorial();
    }
    
    /// <summary>
    /// Completes the tutorial and SAVES completion status
    /// </summary>
    void CompleteTutorial()
    {
        tutorialComplete = true;
        
        // SAVE TUTORIAL COMPLETION
        if (TutorialSaveData.Instance != null)
        {
            TutorialSaveData.Instance.CompleteTutorial();
        }
        
        string[] completionLines = new string[]
        {
            "Congratulations! You've completed the tutorial!",
            "",
            "You've learned all the basic controls:",
            "- Moving and turning",
            "- Teleportation",
            "- Opening the task list (Y button)",
            "- Investigation (X button)",
            "- Grabbing and storing items (Grip button)",
            "",
            "Now approach the computer screen in the lobby",
            "to select your training mode and department.",
            "",
            "Good luck with your training!"
        };
        
        dialogueSystem.StartDialog(completionLines);
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (movementTriggerZone != null)
        {
            movementTriggerZone.OnPlayerEntered -= HandleMovementZoneEntered;
        }
        
        if (teleportZone != null)
        {
            teleportZone.OnPlayerTeleported -= HandleTeleportCompleted;
        }
        
        if (grippingTask != null)
        {
            grippingTask.OnTaskCompleted.RemoveListener(HandleGrippingTaskCompleted);
        }
    }
}