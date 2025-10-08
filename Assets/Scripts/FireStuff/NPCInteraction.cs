using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Crosstales.RTVoice; // Add RT-Voice namespace
using Crosstales.RTVoice.Model;

public class NPCInteraction : CustomTaskController
{
    // UI Elements
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public Button replyOption1;
    public Button replyOption2;

    // VR Interaction UI
    public GameObject interactionIndicator;

    // Dialogue Content
    private string initialDialogue = "There's a fire, what should we do?";
    private string correctDialogue = "Alright! I will evacuate and call for help.";
    private string wrongDialogue = "That doesn't sound right. Try again.";

    // Player and NPC
    public Transform rightControllerTransform;
    public Animator npcAnimator;
    public float evacuationSpeed = 2f;
    public Transform evacuationPoint;

    // Task-specific fields
    [Header("Task Settings")]
    [Tooltip("The NPC object to highlight (usually this same GameObject)")]
    public HighlightableObject targetObject;

    [Header("UI")]
    public PopupManager popupManager;

    // VR Controller Input
    public InputActionProperty talkAction;

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
    private bool isSpeaking = false;
    private Voice selectedVoice;
    private string currentSpeechId;

    void Start()
    {
        Debug.Log($"NPCInteraction Start() called on {gameObject.name}");
        Debug.Log($"taskName: '{taskName}', taskDescription: '{taskDescription}'");
        dialoguePanel.SetActive(false);
        interactionIndicator.SetActive(false);
        replyOption1.onClick.AddListener(OnReplyOption1);
        replyOption2.onClick.AddListener(OnReplyOption2);
        talkAction.action.Enable();

        // Task controller setup
        if (targetObject == null)
        {
            // Try to find HighlightableObject on this GameObject if not assigned
            targetObject = GetComponent<HighlightableObject>();
        }

        // Set totalItems to 1 for NPC evacuation task
        totalItems = 1;

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

    /// <summary>
    /// Override the InitializeTask method from TaskController
    /// </summary>
    public override void InitializeTask()
    {
        base.InitializeTask();

        Debug.Log($"NPC evacuation task '{taskName}' initialized.");
    }

    /// <summary>
    /// Called after UI is set up to update initial progress
    /// </summary>
    public void UpdateInitialProgress()
    {
        OnProgressUpdated.Invoke(0, totalItems);
    }

    void Update()
    {
        CheckForRaycastHit();

        if (isPlayerPointingAtNPC && talkAction.action.WasPressedThisFrame() && !isDialogueActive)
        {
            ShowInitialDialogue();
        }
    }

    void CheckForRaycastHit()
    {
        RaycastHit hit;
        if (Physics.Raycast(rightControllerTransform.position, rightControllerTransform.forward, out hit, 10f))
        {
            if (hit.collider.gameObject == this.gameObject)
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

        // Speak the initial dialogue
        StartSpeech(initialDialogue);
    }

    void OnReplyOption1()
    {
        dialogueText.text = correctDialogue;
        replyOption1.gameObject.SetActive(false);
        replyOption2.gameObject.SetActive(false);

        // Speak the correct response
        StartSpeech(correctDialogue);

        StartCoroutine(EvacuateNPC());
        isDialogueActive = false;
    }

    void OnReplyOption2()
    {
        dialogueText.text = wrongDialogue;

        // Speak the wrong response
        StartSpeech(wrongDialogue);
    }

    IEnumerator EvacuateNPC()
    {
        // Immediately start the walk animation
        npcAnimator.SetTrigger("Walk");

        while (Vector3.Distance(transform.position, evacuationPoint.position) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, evacuationPoint.position, evacuationSpeed * Time.deltaTime);
            transform.LookAt(evacuationPoint);
            yield return null;
        }

        Debug.Log("NPC reached evacuation point");

        // COMPLETE THE TASK
        CompleteTask();

        // Stop any ongoing speech before destroying
        if (enableTTS && isSpeaking)
        {
            Speaker.Instance.Silence();
        }

        // Hide dialogue first
        dialoguePanel.SetActive(false);

        // Wait for events to finish, then destroy
        yield return new WaitForSeconds(0.5f);

        Debug.Log("Destroying NPC now");
        Destroy(gameObject);
    }

    /// <summary>
    /// Override the StartTask method from TaskController
    /// </summary>
    public override void StartTask()
    {
        Debug.Log($"NPCInteraction.StartTask() called. IsCompleted: {IsTaskCompleted()}");
        Debug.Log($"NPC targetObject is: {(targetObject != null ? targetObject.name : "null")}");
        
        if (IsTaskCompleted()) return;

        if (targetObject != null)
        {
            targetObject.SetHighlight(true);
        }

        Debug.Log($"Started NPC evacuation task '{taskName}'.");
    }

    /// <summary>
    /// Override the EndTask method from TaskController
    /// </summary>
    public override void EndTask()
    {
        Debug.Log($"Ending NPC evacuation task '{taskName}' and turning off highlights.");

        // Stop any ongoing speech
        if (enableTTS && isSpeaking)
        {
            Speaker.Instance.Silence();
        }

        if (targetObject != null)
        {
            targetObject.SetHighlight(false);
        }

        if (interactionIndicator != null)
        {
            interactionIndicator.SetActive(false);
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
}