using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.AI;
using Meta.WitAi.TTS.Utilities; // Meta Voice SDK namespace
using Meta.WitAi.TTS.Data; // For TTSClipData

public class FollowerNPC : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public Button followButton;
    public Button cancelButton;

    [Header("Navigation")]
    public NavMeshAgent navAgent;
    
    [Header("VR Interaction")]
    public GameObject interactionIndicator;
    public Transform rightControllerTransform;
    public InputActionProperty talkAction;
    
    [Header("Dialogue")]
    [TextArea(2, 4)]
    public string initialDialogue = "I don't know where to go! Can you help me?";
    [TextArea(2, 4)]
    public string followingDialogue = "Okay, I'll follow you!";
    
    [Header("NPC Settings")]
    public Animator npcAnimator;
    public Transform playerTransform;

    [Header("Meta Voice TTS Settings")]
    [Tooltip("Enable text-to-speech for this NPC")]
    public bool enableTTS = true;
    [Tooltip("Shared TTSSpeaker component for Meta Voice SDK speech output")]
    public TTSSpeaker ttsSpeaker;
    [Tooltip("Voice preset name for this NPC (e.g., WITSREBECCA, WITSCODY)")]
    public string voicePresetName = "WITSCODY";
    
    private bool isPlayerPointingAtNPC = false;
    private bool isDialogueActive = false;
    private bool isFollowing = false;
    private bool isSpeaking = false;
    private string currentSpeechText;

    void Start()
    {
        dialoguePanel.SetActive(false);
        interactionIndicator.SetActive(false);
        
        followButton.onClick.AddListener(OnFollowButtonClicked);
        cancelButton.onClick.AddListener(OnCancelButtonClicked);
        
        talkAction.action.Enable();

        // Setup TTSSpeaker if not assigned
        if (ttsSpeaker == null && enableTTS)
        {
            ttsSpeaker = GetComponent<TTSSpeaker>();
            if (ttsSpeaker == null)
            {
                Debug.LogError($"FollowerNPC on {gameObject.name}: TTSSpeaker component not found! Please add a TTSSpeaker component or assign it in the inspector.");
            }
        }

        // Subscribe to Meta Voice TTS events if TTS is enabled
        if (enableTTS && ttsSpeaker != null)
        {
            ttsSpeaker.Events.OnPlaybackStart.AddListener(OnSpeechStart);
            ttsSpeaker.Events.OnPlaybackComplete.AddListener(OnSpeechComplete);
            ttsSpeaker.Events.OnPlaybackCancelled.AddListener(OnSpeechCancelled);
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
            FollowPlayer();
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
        isDialogueActive = true;
        dialoguePanel.SetActive(true);
        dialogueText.text = initialDialogue;
        interactionIndicator.SetActive(false);
        
        followButton.gameObject.SetActive(true);
        cancelButton.gameObject.SetActive(true);

        // Speak the initial dialogue
        StartSpeech(initialDialogue);
    }

    void OnFollowButtonClicked()
    {
        dialogueText.text = followingDialogue;
        followButton.gameObject.SetActive(false);
        cancelButton.gameObject.SetActive(false);

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

        dialoguePanel.SetActive(false);
        isDialogueActive = false;
    }

    IEnumerator StartFollowingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        dialoguePanel.SetActive(false);
        isDialogueActive = false;
        isFollowing = true;
        
        Debug.Log($"{gameObject.name} is now following the player");
    }

    void FollowPlayer()
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("Player transform not assigned!");
            return;
        }

        if (navAgent != null)
        {
            // Set destination to player
            navAgent.SetDestination(playerTransform.position);
            
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
    /// Optional: Auto-detect player if not assigned
    /// </summary>
    void OnValidate()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
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

    public bool IsFollowing()
    {
        return isFollowing;
    }
}