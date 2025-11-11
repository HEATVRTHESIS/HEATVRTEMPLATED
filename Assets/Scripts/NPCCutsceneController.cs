using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using Meta.WitAi.TTS.Utilities; // Meta Voice SDK namespace
using Meta.WitAi.TTS.Data; // For TTSClipData

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
    public GameObject hoverIndicator;
    [Tooltip("Offset from hand bone")]
    public Vector3 checklistOffset = new Vector3(0, 0, 0.1f);
    
    [Header("Input Mapping")]
    public InputActionProperty pickupChecklistAction;
    [Tooltip("Input action to progress dialogue lines")]
    public InputActionProperty nextLineAction;
    
    [Header("Cutscene Settings")]
    public bool startCutsceneOnStart = true;
    public float cutsceneStartDelay = 0.5f;
    [Tooltip("The speed at which characters are typed out. A smaller value is faster.")]
    public float typingSpeed = 0.05f;
    
    [Header("Meta Voice TTS Settings")]
    [Tooltip("Enable text-to-speech using Meta Voice SDK")]
    public bool enableTTS = true;
    [Tooltip("TTSSpeaker component for Meta Voice SDK speech output")]
    public TTSSpeaker ttsSpeaker;
    [Tooltip("Wait for speech to complete before allowing next line")]
    public bool waitForSpeech = true;
    [Tooltip("Show text immediately when speech starts (disable typewriter for speech)")]
    public bool showTextImmediatelyWithSpeech = false;
    
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
    private float lastHoverChangeTime = 0f;

    // TTS and dialogue progression variables
    private bool isTyping = false;
    private bool isSpeaking = false;
    private bool speechStartedForCurrentLine = false;
    private string currentLine;
    private string currentSpeechText;
    private Coroutine typingCoroutine;
    private Queue<string> dialogueQueue = new Queue<string>();
    private bool dialogueInProgress = false;

    void Awake()
    {
        // Initially hide the hover indicator
        if (hoverIndicator != null)
        {
            hoverIndicator.SetActive(false);
        }

        // Setup TTSSpeaker if not assigned
        if (ttsSpeaker == null && enableTTS)
        {
            ttsSpeaker = GetComponent<TTSSpeaker>();
            if (ttsSpeaker == null)
            {
                Debug.LogError($"NPCCutsceneController on {gameObject.name}: TTSSpeaker component not found! Please add a TTSSpeaker component or assign it in the inspector.");
            }
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
        // Subscribe to pickup action
        if (pickupChecklistAction.action != null)
        {
            pickupChecklistAction.action.performed += OnPickupActionPerformed;
            pickupChecklistAction.action.Enable();
        }

        // Subscribe to next line action
        if (nextLineAction.action != null)
        {
            nextLineAction.action.performed += OnNextLineButtonPressed;
            nextLineAction.action.Enable();
        }

        // Subscribe to Meta Voice TTS events if TTS is enabled
        if (enableTTS && ttsSpeaker != null)
        {
            ttsSpeaker.Events.OnPlaybackStart.AddListener(OnSpeechStart);
            ttsSpeaker.Events.OnPlaybackComplete.AddListener(OnSpeechComplete);
            ttsSpeaker.Events.OnPlaybackCancelled.AddListener(OnSpeechCancelled);
        }
    }
    
    void OnDisable()
    {
        // Unsubscribe from pickup action
        if (pickupChecklistAction.action != null)
        {
            pickupChecklistAction.action.performed -= OnPickupActionPerformed;
            pickupChecklistAction.action.Disable();
        }

        // Unsubscribe from next line action
        if (nextLineAction.action != null)
        {
            nextLineAction.action.performed -= OnNextLineButtonPressed;
            nextLineAction.action.Disable();
        }

        // Unsubscribe from Meta Voice TTS events
        if (enableTTS && ttsSpeaker != null)
        {
            ttsSpeaker.Events.OnPlaybackStart.RemoveListener(OnSpeechStart);
            ttsSpeaker.Events.OnPlaybackComplete.RemoveListener(OnSpeechComplete);
            ttsSpeaker.Events.OnPlaybackCancelled.RemoveListener(OnSpeechCancelled);
        }
    }

    #region Meta Voice Event Handlers

    private void OnSpeechStart(TTSSpeaker speaker, TTSClipData clipData)
    {
        if (clipData.textToSpeak == currentSpeechText)
        {
            isSpeaking = true;
        }
    }

    private void OnSpeechComplete(TTSSpeaker speaker, TTSClipData clipData)
    {
        if (clipData.textToSpeak == currentSpeechText)
        {
            isSpeaking = false;
        }
    }

    private void OnSpeechCancelled(TTSSpeaker speaker, TTSClipData clipData, string reason)
    {
        if (clipData.textToSpeak == currentSpeechText)
        {
            isSpeaking = false;
        }
    }

    #endregion

    #region Input Handlers

    private void OnPickupActionPerformed(InputAction.CallbackContext context)
    {
        if (hoverIndicator != null && hoverIndicator.activeSelf && waitingForChecklistPickup)
        {
            PickupChecklist();
        }
    }

    private void OnNextLineButtonPressed(InputAction.CallbackContext context)
    {
        if (!dialogueInProgress || !cutsceneActive)
            return;

        // If typing is in progress, complete the current line
        if (isTyping)
        {
            CompleteLine();
            return;
        }

        // If we're waiting for speech to complete, don't allow progressing
        if (waitForSpeech && isSpeaking)
        {
            return;
        }

        // Stop any ongoing speech before moving to next line
        if (enableTTS && ttsSpeaker != null)
        {
            ttsSpeaker.Stop();
        }

        // Otherwise, show the next line
        DisplayNextDialogueLine();
    }

    #endregion
    
    private void StartCutscene()
    {
        cutsceneActive = true;
        DisablePlayerMovement();
        
        if (dialogueUI != null)
            dialogueUI.SetActive(true);
        
        // Initialize dialogue queue
        dialogueQueue.Clear();
        foreach (string line in dialogueLines)
        {
            dialogueQueue.Enqueue(line);
        }
        
        dialogueInProgress = true;
        DisplayNextDialogueLine();
        
        Debug.Log("Cutscene started!");
    }

    private IEnumerator ShowChecklistAfterDialogue()
    {
        yield return new WaitForSeconds(timeBetweenLines);
        
        // Hide dialogue UI
        if (dialogueUI != null)
            dialogueUI.SetActive(false);
        
        // Small pause before pointing
        yield return new WaitForSeconds(0.3f);
        
        // Play pointing animation and show checklist
        PlayAnimation(pointingAnimationName, false);
        
        // Wait a moment for the animation to reach the pointing pose
        yield return new WaitForSeconds(0.5f);
        
        // Show checklist
        if (checklistObject != null)
        {
            checklistObject.SetActive(true);
            checklistObject.transform.rotation = checklistOriginalRotation;
        }
        
        // Now wait for player to hover and pick up checklist
        waitingForChecklistPickup = true;
    }

    private void DisplayNextDialogueLine()
    {
        // Stop any ongoing typing
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        // Stop any ongoing speech
        if (enableTTS && isSpeaking && ttsSpeaker != null)
        {
            ttsSpeaker.Stop();
        }

        // Reset speech flag for new line
        speechStartedForCurrentLine = false;

        // Check if there are lines left to display
        if (dialogueQueue.Count > 0)
        {
            currentLine = dialogueQueue.Dequeue();

            // Switch to talking animation
            PlayAnimation(talkingAnimationName);

            if (enableTTS && showTextImmediatelyWithSpeech)
            {
                // Show text immediately and start speech
                if (dialogueText != null)
                    dialogueText.text = currentLine;
                StartSpeech(currentLine);
            }
            else
            {
                // Start typing animation
                typingCoroutine = StartCoroutine(TypeLine(currentLine));
            }
        }
        else
        {
            // All dialogue complete
            dialogueInProgress = false;
            PlayAnimation(idleAnimationName);
            StartCoroutine(ShowChecklistAfterDialogue());
        }
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        if (dialogueText != null)
            dialogueText.text = "";

        // Start speech if enabled and not showing text immediately
        if (enableTTS && !showTextImmediatelyWithSpeech)
        {
            StartSpeech(line);
        }

        foreach (char character in line.ToCharArray())
        {
            if (dialogueText != null)
                dialogueText.text += character;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isTyping = false;

        // Start speech after typing if not already started
        if (enableTTS && showTextImmediatelyWithSpeech && !isSpeaking)
        {
            StartSpeech(line);
        }
    }

    private void CompleteLine()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        isTyping = false;
        if (dialogueText != null)
            dialogueText.text = currentLine;

        // Only start speech if it hasn't been started yet for this line
        if (enableTTS && !speechStartedForCurrentLine && !isSpeaking)
        {
            StartSpeech(currentLine);
        }
    }

    private void StartSpeech(string text)
    {
        if (!enableTTS || ttsSpeaker == null || string.IsNullOrEmpty(text.Trim()))
            return;

        // Check if TTS is muted globally (from VRPauseMenu)
        if (VRDialogueSystem.IsTTSMuted)
            return;

        speechStartedForCurrentLine = true;
        currentSpeechText = text;

        // Use Speak() for immediate playback
        ttsSpeaker.Speak(text);
    }
    
    private void PlayAnimation(string animationName, bool loop = true)
    {
        if (npcAnimator == null) return;
        npcAnimator.Play(animationName);
    }

    #region Checklist Hover Methods

    public void OnChecklistHovered()
    {
        if (!waitingForChecklistPickup) return;

        if (Time.time - lastHoverChangeTime < hoverCooldown)
            return;

        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
            hoverCoroutine = null;
        }

        if (hoverIndicator != null && !isHovering)
        {
            hoverIndicator.SetActive(true);
            isHovering = true;
            lastHoverChangeTime = Time.time;
        }
    }

    public void OnChecklistHoverExit()
    {
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
        }
        hoverCoroutine = StartCoroutine(DelayedHoverExit());
    }

    private IEnumerator DelayedHoverExit()
    {
        yield return new WaitForSeconds(hoverExitDelay);

        if (hoverIndicator != null && isHovering)
        {
            hoverIndicator.SetActive(false);
            isHovering = false;
            lastHoverChangeTime = Time.time;
        }
        hoverCoroutine = null;
    }

    #endregion
    
    private void PickupChecklist()
    {
        waitingForChecklistPickup = false;
        
        if (hoverIndicator != null)
            hoverIndicator.SetActive(false);
        
        if (checklistObject != null)
            checklistObject.SetActive(false);
        
        PlayAnimation(idleAnimationName);
        
        EnablePlayerMovement();
        
        cutsceneActive = false;
        
        Debug.Log("Checklist picked up! Player can now move.");
    }
    
    private void DisablePlayerMovement()
    {
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
        }
        
        if (characterController != null)
        {
            characterControllerWasEnabled = characterController.enabled;
            characterController.enabled = false;
        }
    }
    
    private void EnablePlayerMovement()
    {
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = true;
        }
        
        if (characterController != null)
        {
            characterController.enabled = characterControllerWasEnabled;
        }
    }
    
    public void TriggerCutscene()
    {
        StartCutscene();
    }

    void OnDestroy()
    {
        // Stop any ongoing speech
        if (enableTTS && ttsSpeaker != null)
        {
            ttsSpeaker.Stop();
        }
    }
}