using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class NPCCutsceneController : MonoBehaviour
{
    [Header("Player References")]
    [Tooltip("Assign the XR Origin or OVRPlayerController")]
    public GameObject playerRig;
    [Tooltip("If using a specific movement script, assign it here")]
    public MonoBehaviour playerMovementScript;
    
    [Header("Animation Setup")]
    public Animator npcAnimator;
    [Tooltip("The hand bone/transform where checklist should follow")]
    public Transform npcHandTransform;
    [Tooltip("Name of the talking animation state in Animator")]
    public string talkingAnimationName = "Talking";
    [Tooltip("Name of the idle animation state in Animator")]
    public string idleAnimationName = "Idle";
    [Tooltip("Name of the pointing animation state in Animator")]
    public string pointingAnimationName = "Pointing";
    
    [Header("Dialogue System")]
    [TextArea(3, 10)]
    public List<string> dialogueLines = new List<string>();
    public float timeBetweenLines = 0.5f;
    
    [Header("UI References")]
    public GameObject dialogueUI;
    public TMPro.TextMeshProUGUI dialogueText;
    public GameObject checklistObject;
    public GameObject hoverIndicator; // Visual feedback when hovering
    [Tooltip("Offset from hand bone")]
    public Vector3 checklistOffset = new Vector3(0, 0, 0.1f);
    
    [Header("Input Mapping")]
    public InputActionProperty pickupChecklistAction;
    
    [Header("Cutscene Settings")]
    public bool startCutsceneOnStart = true;
    public float cutsceneStartDelay = 0.5f;
    
    [Header("VR Stability")]
    [Tooltip("Time before hover can toggle again to prevent flickering")]
    public float hoverCooldown = 0.15f;
    [Tooltip("Delay before hiding hover indicator after exit")]
    public float hoverExitDelay = 0.15f;
    
    private bool cutsceneActive = false;
    private bool waitingForChecklistPickup = false;
    private Transform playerCamera;
    private CharacterController characterController;
    private bool characterControllerWasEnabled;
    private Quaternion checklistOriginalRotation;

    private bool isHovering = false;
    private Coroutine hoverCoroutine;
    private float lastHoverChangeTime = 0f; // Track last hover state change

    void Awake()
    {
        // Initially hide the hover indicator
        if (hoverIndicator != null)
        {
            hoverIndicator.SetActive(false);
        }
    }
    
    void Start()
    {
        // Find player camera
        playerCamera = Camera.main.transform;
        
        // Try to find CharacterController on player rig
        if (playerRig != null)
        {
            characterController = playerRig.GetComponent<CharacterController>();
        }
        
        // Store checklist's original rotation
        if (checklistObject != null)
        {
            checklistOriginalRotation = checklistObject.transform.rotation;
            checklistObject.SetActive(false);
        }
        
        // Hide dialogue UI initially
        if (dialogueUI != null)
            dialogueUI.SetActive(false);
        
        if (startCutsceneOnStart)
        {
            Invoke(nameof(StartCutscene), cutsceneStartDelay);
        }
    }
    
    void Update()
    {
        // Make checklist follow the hand bone position only, keep original rotation
        if (waitingForChecklistPickup && checklistObject != null && npcHandTransform != null)
        {
            checklistObject.transform.position = npcHandTransform.position + checklistOffset;
            checklistObject.transform.rotation = checklistOriginalRotation;
        }
    }
    
    void OnEnable()
    {
        // Subscribe to the action's 'performed' event
        if (pickupChecklistAction.action != null)
        {
            pickupChecklistAction.action.performed += OnPickupActionPerformed;
            pickupChecklistAction.action.Enable();
        }
    }
    
    void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        if (pickupChecklistAction.action != null)
        {
            pickupChecklistAction.action.performed -= OnPickupActionPerformed;
            pickupChecklistAction.action.Disable();
        }
    }
    
    /// <summary>
    /// This method is called by the InputAction when the button is pressed.
    /// </summary>
    private void OnPickupActionPerformed(InputAction.CallbackContext context)
    {
        // Only pick up if hovering indicator is visible and waiting for pickup
        if (hoverIndicator != null && hoverIndicator.activeSelf && waitingForChecklistPickup)
        {
            PickupChecklist();
        }
    }
    
    public void StartCutscene()
    {
        if (cutsceneActive) return;
        
        cutsceneActive = true;
        DisablePlayerMovement();
        StartCoroutine(PlayCutsceneSequence());
    }
    
    private IEnumerator PlayCutsceneSequence()
    {
        // Start with idle animation
        PlayAnimation(idleAnimationName);
        
        // Show dialogue UI
        if (dialogueUI != null)
            dialogueUI.SetActive(true);
        
        // Play through all dialogue lines
        foreach (string line in dialogueLines)
        {
            // Switch to talking animation
            PlayAnimation(talkingAnimationName);
            
            // Display the dialogue
            if (dialogueText != null)
                dialogueText.text = line;
            
            // Wait for the line duration
            float lineDuration = CalculateLineDuration(line);
            yield return new WaitForSeconds(lineDuration);
            
            // Brief pause between lines with idle animation
            PlayAnimation(idleAnimationName);
            yield return new WaitForSeconds(timeBetweenLines);
        }
        
        // Hide dialogue UI
        if (dialogueUI != null)
            dialogueUI.SetActive(false);
        
        // Small pause before pointing
        yield return new WaitForSeconds(0.3f);
        
        // Play pointing animation and show checklist
        PlayAnimation(pointingAnimationName, false); // Don't loop
        
        // Wait a moment for the animation to reach the pointing pose
        yield return new WaitForSeconds(0.5f);
        
        // Show checklist
        if (checklistObject != null)
        {
            checklistObject.SetActive(true);
            // Reset to original rotation when shown
            checklistObject.transform.rotation = checklistOriginalRotation;
        }
        
        // Now wait for player to hover and pick up checklist
        waitingForChecklistPickup = true;
    }
    
    private void PlayAnimation(string animationName, bool loop = true)
    {
        if (npcAnimator == null) return;
        npcAnimator.Play(animationName);
    }
    
    private float CalculateLineDuration(string line)
    {
        // Rough estimate: 0.05 seconds per character, minimum 2 seconds
        float duration = Mathf.Max(2f, line.Length * 0.05f);
        return duration;
    }

    // PUBLIC METHOD: Call this from XR Grab Interactable's Hover Entered event
    public void OnChecklistHovered()
    {
        if (!waitingForChecklistPickup) return;

        // Prevent rapid toggling with cooldown
        if (Time.time - lastHoverChangeTime < hoverCooldown)
            return;

        // Cancel any pending hover exit
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
            hoverCoroutine = null;
        }

        if (hoverIndicator != null && !isHovering)
        {
            hoverIndicator.SetActive(true);
            isHovering = true;
            lastHoverChangeTime = Time.time; // Track time of state change
        }
    }

    // PUBLIC METHOD: Call this from XR Grab Interactable's Hover Exited event
    public void OnChecklistHoverExit()
    {
        // Delay the exit to prevent flickering from rapid hover enter/exit
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
        }
        hoverCoroutine = StartCoroutine(DelayedHoverExit());
    }

    private IEnumerator DelayedHoverExit()
    {
        // Increased delay for VR stability
        yield return new WaitForSeconds(hoverExitDelay);

        if (hoverIndicator != null && isHovering)
        {
            hoverIndicator.SetActive(false);
            isHovering = false;
            lastHoverChangeTime = Time.time; // Track time of state change
        }
        hoverCoroutine = null;
    }
    
    private void PickupChecklist()
    {
        waitingForChecklistPickup = false;
        
        // Hide hover indicator
        if (hoverIndicator != null)
            hoverIndicator.SetActive(false);
        
        // Hide checklist
        if (checklistObject != null)
            checklistObject.SetActive(false);
        
        // Return to idle animation
        PlayAnimation(idleAnimationName);
        
        // Re-enable player movement
        EnablePlayerMovement();
        
        // End cutscene
        cutsceneActive = false;
        
        Debug.Log("Checklist picked up! Player can now move.");
    }
    
    private void DisablePlayerMovement()
    {
        // Disable movement script if assigned
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
        }
        
        // Disable CharacterController if found
        if (characterController != null)
        {
            characterControllerWasEnabled = characterController.enabled;
            characterController.enabled = false;
        }
    }
    
    private void EnablePlayerMovement()
    {
        // Re-enable movement script if assigned
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = true;
        }
        
        // Re-enable CharacterController
        if (characterController != null)
        {
            characterController.enabled = characterControllerWasEnabled;
        }
    }
    
    // Public method to manually trigger cutscene (e.g., from a trigger collider)
    public void TriggerCutscene()
    {
        StartCutscene();
    }
}