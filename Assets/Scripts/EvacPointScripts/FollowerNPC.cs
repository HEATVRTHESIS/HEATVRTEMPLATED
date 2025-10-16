using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.AI;
using Crosstales.RTVoice; // Add RT-Voice namespace
using Crosstales.RTVoice.Model;

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

    [Header("RT-Voice TTS Settings")]
    [Tooltip("Enable text-to-speech for this NPC")]
    public bool enableTTS = true;
    [Tooltip("AudioSource for RT-Voice speech output")]
    public AudioSource speechAudioSource;
    [Tooltip("Voice to use for speech (leave empty for default)")]
    public string voiceName = "";
    [Tooltip("Speech rate (0.1 to 3.0, 1.0 is normal speed)")]
    [Range(0.1f, 3.0f)]
    public float speechRate = 1.0f;
    [Tooltip("Speech pitch (0.0 to 2.0, 1.0 is normal pitch)")]
    [Range(0.0f, 2.0f)]
    public float speechPitch = 1.0f;
    [Tooltip("Speech volume (0.0 to 1.0)")]
    [Range(0.0f, 1.0f)]
    public float speechVolume = 1.0f;
    [Tooltip("Use native speech (no file generation)")]
    public bool useNativeSpeech = true;
    
    private bool isPlayerPointingAtNPC = false;
    private bool isDialogueActive = false;
    private bool isFollowing = false;
    private bool isSpeaking = false;
    private Voice selectedVoice;
    private string currentSpeechId;

    void Start()
    {
        dialoguePanel.SetActive(false);
        interactionIndicator.SetActive(false);
        
        followButton.onClick.AddListener(OnFollowButtonClicked);
        cancelButton.onClick.AddListener(OnCancelButtonClicked);
        
        talkAction.action.Enable();

        // Setup AudioSource for RT-Voice if not assigned
        if (speechAudioSource == null && enableTTS)
        {
            speechAudioSource = gameObject.GetComponent<AudioSource>();
            if (speechAudioSource == null)
            {
                speechAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // Subscribe to RT-Voice events if TTS is enabled
        if (enableTTS && Speaker.Instance != null)
        {
            Speaker.Instance.OnSpeakStart += OnSpeechStart;
            Speaker.Instance.OnSpeakComplete += OnSpeechComplete;
            Speaker.Instance.OnVoicesReady += OnVoicesReady;
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from RT-Voice events
        if (enableTTS && Speaker.Instance != null)
        {
            Speaker.Instance.OnSpeakStart -= OnSpeechStart;
            Speaker.Instance.OnSpeakComplete -= OnSpeechComplete;
            Speaker.Instance.OnVoicesReady -= OnVoicesReady;
            Speaker.Instance.Silence();
        }
    }

    /// <summary>
    /// RT-Voice event: Called when voices are ready
    /// </summary>
    private void OnVoicesReady()
    {
        // Try to find the specified voice, or use default
        if (!string.IsNullOrEmpty(voiceName))
        {
            selectedVoice = Speaker.Instance.VoiceForName(voiceName);
            if (selectedVoice == null)
            {
                Debug.LogWarning($"Voice '{voiceName}' not found for {gameObject.name}. Using default voice.");
            }
        }
    }

    /// <summary>
    /// RT-Voice event: Called when speech starts
    /// </summary>
    private void OnSpeechStart(Wrapper wrapper)
    {
        if (wrapper.Uid == currentSpeechId)
        {
            isSpeaking = true;
        }
    }

    /// <summary>
    /// RT-Voice event: Called when speech completes
    /// </summary>
    private void OnSpeechComplete(Wrapper wrapper)
    {
        if (wrapper.Uid == currentSpeechId)
        {
            isSpeaking = false;
        }
    }

    /// <summary>
    /// Starts speech using RT-Voice
    /// </summary>
    private void StartSpeech(string text)
    {
        if (!enableTTS || Speaker.Instance == null || string.IsNullOrEmpty(text.Trim()))
            return;

        // Stop any ongoing speech first
        if (isSpeaking)
        {
            Speaker.Instance.Silence();
        }

        // Generate unique ID for this speech
        currentSpeechId = System.Guid.NewGuid().ToString();

        if (useNativeSpeech)
        {
            // Use native speech (no file generation)
            Speaker.Instance.SpeakNative(
                text,
                selectedVoice,
                speechRate,
                speechPitch,
                speechVolume
            );
        }
        else
        {
            // Use file generation method
            Speaker.Instance.Speak(
                text,
                speechAudioSource,
                selectedVoice,
                true,
                speechRate,
                speechPitch,
                speechVolume,
                currentSpeechId
            );
        }
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
        if (enableTTS && isSpeaking)
        {
            Speaker.Instance.Silence();
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
        if (enableTTS && isSpeaking)
        {
            Speaker.Instance.Silence();
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
    /// Public method to set voice by name
    /// </summary>
    public void SetVoice(string newVoiceName)
    {
        voiceName = newVoiceName;
        selectedVoice = Speaker.Instance?.VoiceForName(voiceName);
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