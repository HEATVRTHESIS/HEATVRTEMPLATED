using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.AI;
using Meta.WitAi.TTS.Utilities; // Meta Voice SDK namespace
using Meta.WitAi.TTS.Data; // For TTSClipData

public class IVPoleFollowerNPC : MonoBehaviour
{
    [Header("Navigation")]
    public NavMeshAgent navAgent;
    
    [Header("Task Settings")]
    [Tooltip("The NPC object to highlight (usually this same GameObject)")]
    public HighlightableObject targetObject;
    
    [Header("VR Interaction")]
    public GameObject interactionIndicator;
    public Transform rightControllerTransform;
    public InputActionProperty talkAction;
    
    [Header("Dialogue")]
    [TextArea(2, 4)]
    public string initialDialogue = "I need my IV! Can you bring it to me?";
    [TextArea(2, 4)]
    public string followingDialogue = "Thank you! I'll follow my IV pole now.";
    [TextArea(2, 4)]
    public string tooFarDialogue = "Wait! My IV pole is too far away!";
    
    [Header("NPC Settings")]
    public Animator npcAnimator;
    public Transform ivPoleTransform; // The IV pole this NPC will follow
    public float followDistance = 2.5f; // Distance to maintain from IV pole
    public float stopFollowingDistance = 15f; // Stop following if IV pole gets too far

    [Header("Meta Voice TTS Settings")]
    [Tooltip("Enable text-to-speech for this NPC")]
    public bool enableTTS = true;
    [Tooltip("Shared TTSSpeaker component for Meta Voice SDK speech output")]
    public TTSSpeaker ttsSpeaker;
    [Tooltip("Voice preset name for this NPC (e.g., WITSREBECCA, WITSCODY)")]
    public string voicePresetName = "WITSREBECCA";
    
    private bool isPlayerPointingAtNPC = false;
    private bool isDialogueActive = false;
    private bool isFollowing = false;
    private bool isSpeaking = false;
    private bool hasSpokenTooFarWarning = false;
    private string currentSpeechText;

    void Start()
    {
        interactionIndicator.SetActive(false);
        
        talkAction.action.Enable();

        // Task controller setup
        if (targetObject == null)
        {
            // Try to find HighlightableObject on this GameObject if not assigned
            targetObject = GetComponent<HighlightableObject>();
        }

        // Setup TTSSpeaker if not assigned
        if (ttsSpeaker == null && enableTTS)
        {
            ttsSpeaker = GetComponent<TTSSpeaker>();
            if (ttsSpeaker == null)
            {
                Debug.LogError($"IVPoleFollowerNPC on {gameObject.name}: TTSSpeaker component not found! Please add a TTSSpeaker component or assign it in the inspector.");
            }
        }

        // Subscribe to Meta Voice TTS events if TTS is enabled
        if (enableTTS && ttsSpeaker != null)
        {
            ttsSpeaker.Events.OnPlaybackStart.AddListener(OnSpeechStart);
            ttsSpeaker.Events.OnPlaybackComplete.AddListener(OnSpeechComplete);
            ttsSpeaker.Events.OnPlaybackCancelled.AddListener(OnSpeechCancelled);
        }

        // Set NavMesh stopping distance based on follow distance
        if (navAgent != null)
        {
            navAgent.stoppingDistance = followDistance;
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from Meta Voice TTS events
        if (enableTTS && ttsSpeaker != null)
        {
            ttsSpeaker.Events.OnPlaybackStart.RemoveListener(OnSpeechStart);
            ttsSpeaker.Events.OnPlaybackComplete.RemoveListener(OnSpeechComplete);
            ttsSpeaker.Events.OnPlaybackCancelled.RemoveListener(OnSpeechCancelled);
            ttsSpeaker.Stop();
        }
    }

    /// <summary>
    /// Meta Voice TTS event: Called when speech playback starts
    /// </summary>
    private void OnSpeechStart(TTSSpeaker speaker, TTSClipData clipData)
    {
        if (clipData.textToSpeak == currentSpeechText)
        {
            isSpeaking = true;
        }
    }

    /// <summary>
    /// Meta Voice TTS event: Called when speech playback completes
    /// </summary>
    private void OnSpeechComplete(TTSSpeaker speaker, TTSClipData clipData)
    {
        if (clipData.textToSpeak == currentSpeechText)
        {
            isSpeaking = false;
        }
    }

    /// <summary>
    /// Meta Voice TTS event: Called when speech playback is cancelled
    /// </summary>
    private void OnSpeechCancelled(TTSSpeaker speaker, TTSClipData clipData, string reason)
    {
        if (clipData.textToSpeak == currentSpeechText)
        {
            isSpeaking = false;
        }
    }

    /// <summary>
    /// Starts speech using Meta Voice SDK
    /// </summary>
    private void StartSpeech(string text)
    {
        if (!enableTTS || ttsSpeaker == null || string.IsNullOrEmpty(text.Trim()))
            return;

        // Check if TTS is muted globally (from VRPauseMenu)
        if (VRDialogueSystem.IsTTSMuted)
            return;

        // Stop any ongoing speech first
        if (isSpeaking)
        {
            ttsSpeaker.Stop();
        }

        // Set the voice preset for this NPC before speaking
        if (!string.IsNullOrEmpty(voicePresetName))
        {
            Debug.Log($"[{gameObject.name}] Setting voice to: '{voicePresetName}'");
            ttsSpeaker.VoiceID = voicePresetName;
            Debug.Log($"[{gameObject.name}] Voice now set to: '{ttsSpeaker.VoiceID}'");
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] voicePresetName is empty!");
        }

        // Store current speech text for tracking
        currentSpeechText = text;

        // Use Speak() for immediate playback
        ttsSpeaker.Speak(text);
    }

    void Update()
    {
        if (isFollowing)
        {
            FollowIVPole();
        }
        else
        {
            CheckForRaycastHit();
            
            if (isPlayerPointingAtNPC && talkAction.action.WasPressedThisFrame() && !isDialogueActive)
            {
                ShowInitialDialogue();
            }
        }
    }

    void CheckForRaycastHit()
    {
        RaycastHit hit;
        if (Physics.Raycast(rightControllerTransform.position, rightControllerTransform.forward, out hit, 10f))
        {
            if (hit.collider.gameObject == this.gameObject || hit.collider.transform.IsChildOf(transform))
            {
                isPlayerPointingAtNPC = true;
                interactionIndicator.SetActive(true);
            }
            else
            {
                isPlayerPointingAtNPC = false;
                interactionIndicator.SetActive(false);
            }
        }
        else
        {
            isPlayerPointingAtNPC = false;
            interactionIndicator.SetActive(false);
        }
    }

    void ShowInitialDialogue()
    {
        if (QuestionUIManager.Instance == null)
        {
            Debug.LogError("QuestionUIManager.Instance is null! Make sure QuestionUIManager is in the scene.");
            return;
        }

        // SAFETY CHECK: Don't show if another NPC is already using the UI
        if (QuestionUIManager.Instance.questionCanvas.activeSelf && !isDialogueActive)
        {
            Debug.LogWarning($"{gameObject.name}: Question UI is already active. Wait for it to close.");
            return;
        }

        isDialogueActive = true;
        interactionIndicator.SetActive(false);

        // Speak the initial dialogue
        StartSpeech(initialDialogue);

        // Show the question UI using the QuestionUIManager
        ShowIVPoleQuestion();
    }

    /// <summary>
    /// Shows the IV pole question using the QuestionUIManager
    /// </summary>
    private void ShowIVPoleQuestion()
    {
        if (QuestionUIManager.Instance == null)
        {
            Debug.LogError("Cannot show IV pole question: QuestionUIManager.Instance is null!");
            return;
        }

        // Set the question text
        if (QuestionUIManager.Instance.questionTextUI != null)
        {
            QuestionUIManager.Instance.questionTextUI.text = initialDialogue;
        }

        // Hide the image for NPC dialogue (it's only used for maintenance tasks)
        if (QuestionUIManager.Instance.questionImage != null)
        {
            QuestionUIManager.Instance.questionImage.gameObject.SetActive(false);
        }

        // Set up button listeners for this specific IV pole follower interaction
        // Yes button = "Help bring IV pole"
        // No button = "Cancel"
        if (QuestionUIManager.Instance.yesButton != null)
        {
            QuestionUIManager.Instance.yesButton.onClick.RemoveAllListeners();
            QuestionUIManager.Instance.yesButton.onClick.AddListener(OnFollowButtonClicked);
        }

        if (QuestionUIManager.Instance.noButton != null)
        {
            QuestionUIManager.Instance.noButton.onClick.RemoveAllListeners();
            QuestionUIManager.Instance.noButton.onClick.AddListener(OnCancelButtonClicked);
        }

        // Position and show the canvas
        ShowQuestionForNPC();
    }

    /// <summary>
    /// Helper method to position and show the question UI for this NPC
    /// </summary>
    private void ShowQuestionForNPC()
    {
        if (QuestionUIManager.Instance == null || QuestionUIManager.Instance.questionCanvas == null)
            return;

        Transform target = targetObject != null ? targetObject.transform : transform;
        
        QuestionUIManager.Instance.questionCanvas.SetActive(true);
        
        // Manually position the canvas
        if (QuestionUIManager.Instance.cameraTransform != null)
        {
            Vector3 basePosition = target.position + Vector3.up * QuestionUIManager.Instance.heightOffset;
            Vector3 cameraToTarget = (basePosition - QuestionUIManager.Instance.cameraTransform.position).normalized;
            Vector3 uiPosition = basePosition - cameraToTarget * QuestionUIManager.Instance.distanceFromObject;
            
            QuestionUIManager.Instance.questionCanvas.transform.position = uiPosition;
            
            // Make UI face the camera
            Vector3 lookDirection = QuestionUIManager.Instance.cameraTransform.position - QuestionUIManager.Instance.questionCanvas.transform.position;
            lookDirection.y = 0;
            
            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                targetRotation *= Quaternion.Euler(0, 180, 0);
                QuestionUIManager.Instance.questionCanvas.transform.rotation = targetRotation;
            }
        }
    }

    void OnFollowButtonClicked()
    {
        // Update the dialogue text to show feedback
        if (QuestionUIManager.Instance != null && QuestionUIManager.Instance.questionTextUI != null)
        {
            QuestionUIManager.Instance.questionTextUI.text = followingDialogue;
        }

        // Speak the following dialogue
        StartSpeech(followingDialogue);
        
        StartCoroutine(StartFollowingAfterDelay(1.5f));
    }

    void OnCancelButtonClicked()
    {
        // Stop any ongoing speech
        if (enableTTS && isSpeaking && ttsSpeaker != null)
        {
            ttsSpeaker.Stop();
        }

        // Hide the question UI
        if (QuestionUIManager.Instance != null)
        {
            QuestionUIManager.Instance.HideQuestion();
            
            // Re-enable the image for future maintenance tasks
            if (QuestionUIManager.Instance.questionImage != null)
            {
                QuestionUIManager.Instance.questionImage.gameObject.SetActive(true);
            }
        }

        isDialogueActive = false;
    }

    IEnumerator StartFollowingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Hide the question UI
        if (QuestionUIManager.Instance != null)
        {
            QuestionUIManager.Instance.HideQuestion();
            
            // Re-enable the image for future maintenance tasks
            if (QuestionUIManager.Instance.questionImage != null)
            {
                QuestionUIManager.Instance.questionImage.gameObject.SetActive(true);
            }
        }
        
        isDialogueActive = false;
        isFollowing = true;
        hasSpokenTooFarWarning = false; // Reset warning flag
        
        Debug.Log($"{gameObject.name} is now following the IV pole");
    }

    void FollowIVPole()
    {
        if (ivPoleTransform == null)
        {
            Debug.LogWarning("IV Pole transform not assigned!");
            return;
        }

        // Check if IV pole is too far away
        float distanceToIVPole = Vector3.Distance(transform.position, ivPoleTransform.position);
        if (distanceToIVPole > stopFollowingDistance)
        {
            // IV pole is too far, stop following temporarily
            if (navAgent != null && !navAgent.isStopped)
            {
                navAgent.isStopped = true;
                if (npcAnimator != null)
                {
                    npcAnimator.SetTrigger("Idle");
                }
            }
            
            // Speak the too far warning (only once until pole comes back in range)
            if (!hasSpokenTooFarWarning)
            {
                StartSpeech(tooFarDialogue);
                hasSpokenTooFarWarning = true;
            }
            
            return;
        }
        
        // Reset the warning flag when IV pole is back in range
        hasSpokenTooFarWarning = false;

        if (navAgent != null)
        {
            // Resume movement if it was stopped
            if (navAgent.isStopped)
            {
                navAgent.isStopped = false;
            }

            // Set destination to IV pole position
            navAgent.SetDestination(ivPoleTransform.position);
            
            // Check if agent has reached the destination (within stopping distance)
            if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
            {
                // Reached destination - idle
                if (npcAnimator != null)
                {
                    npcAnimator.SetTrigger("Idle");
                }
            }
            else
            {
                // Still moving - walk
                if (npcAnimator != null)
                {
                    npcAnimator.SetTrigger("Walk");
                }
            }
        }
    }

    /// <summary>
    /// Call this to make the NPC stop following (e.g., when they reach the exit)
    /// </summary>
    public void StopFollowing()
    {
        isFollowing = false;
        
        if (navAgent != null)
        {
            navAgent.isStopped = true;
        }
        
        if (npcAnimator != null)
        {
            npcAnimator.SetTrigger("Idle");
        }

        // Stop any ongoing speech
        if (enableTTS && isSpeaking && ttsSpeaker != null)
        {
            ttsSpeaker.Stop();
        }
        
        Debug.Log($"{gameObject.name} stopped following");
    }

    /// <summary>
    /// Optional: Auto-detect IV pole if not assigned
    /// </summary>
    void OnValidate()
    {
        if (ivPoleTransform == null)
        {
            GameObject ivPole = GameObject.FindGameObjectWithTag("IVPole");
            if (ivPole != null)
            {
                ivPoleTransform = ivPole.transform;
            }
        }
    }

    /// <summary>
    /// Check if NPC is currently speaking
    /// </summary>
    public bool IsSpeaking()
    {
        return isSpeaking;
    }

    /// <summary>
    /// Check if NPC is currently following the IV pole
    /// </summary>
    public bool IsFollowing()
    {
        return isFollowing;
    }

    /// <summary>
    /// Set the IV pole that this NPC should follow
    /// </summary>
    public void SetIVPole(Transform newIVPole)
    {
        ivPoleTransform = newIVPole;
    }
}