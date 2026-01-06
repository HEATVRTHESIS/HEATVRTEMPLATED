using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Meta.WitAi.TTS.Utilities; // Meta Voice SDK namespace
using Meta.WitAi.TTS.Data; // For TTSClipData

public class NPCInteraction : CustomTaskController
{
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

    [Header("Dialogue Settings")]
    [Tooltip("Is 'Yes' the correct answer? (True = Option1 is correct, False = Option2 is correct)")]
    public bool isOption1Correct = true;

    [Header("UI")]
    public PopupManager popupManager;

    // VR Controller Input
    public InputActionProperty talkAction;

    [Header("Meta Voice TTS Settings")]
    [Tooltip("Enable text-to-speech for this NPC")]
    public bool enableTTS = true;
    [Tooltip("TTSSpeaker component for Meta Voice SDK speech output")]
    public TTSSpeaker ttsSpeaker;

    private bool isPlayerPointingAtNPC = false;
    private bool isDialogueActive = false;
    private bool isSpeaking = false;
    private string currentSpeechText;
    private bool questionAnswered = false;

    void Start()
    {
        Debug.Log($"NPCInteraction Start() called on {gameObject.name}");
        Debug.Log($"taskName: '{taskName}', taskDescription: '{taskDescription}'");
        
        interactionIndicator.SetActive(false);
        talkAction.action.Enable();

        // Task controller setup
        if (targetObject == null)
        {
            // Try to find HighlightableObject on this GameObject if not assigned
            targetObject = GetComponent<HighlightableObject>();
        }

        // Set totalItems to 1 for NPC evacuation task
        totalItems = 1;

        // Setup TTSSpeaker if not assigned
        if (ttsSpeaker == null && enableTTS)
        {
            ttsSpeaker = GetComponent<TTSSpeaker>();
            if (ttsSpeaker == null)
            {
                Debug.LogError($"NPCInteraction on {gameObject.name}: TTSSpeaker component not found! Please add a TTSSpeaker component or assign it in the inspector.");
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
            ttsSpeaker.Stop(currentSpeechText);
        }

        // Store current speech text for tracking
        currentSpeechText = text;

        // Use SpeakQueued to ensure speech is properly queued
        ttsSpeaker.SpeakQueued(text);
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

        if (isPlayerPointingAtNPC && talkAction.action.WasPressedThisFrame() && !isDialogueActive && !questionAnswered)
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
        if (QuestionUIManager.Instance == null)
        {
            Debug.LogError("QuestionUIManager.Instance is null! Make sure QuestionUIManager is in the scene.");
            return;
        }

        isDialogueActive = true;
        interactionIndicator.SetActive(false);

        // Speak the initial dialogue
        StartSpeech(initialDialogue);

        // Show the question UI using the QuestionUIManager
        ShowNPCQuestion();
    }

    /// <summary>
    /// Shows the NPC question using the QuestionUIManager
    /// </summary>
    private void ShowNPCQuestion()
    {
        if (QuestionUIManager.Instance == null)
        {
            Debug.LogError("Cannot show NPC question: QuestionUIManager.Instance is null!");
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

        // Set up button listeners for this specific NPC interaction
        // Yes button = Option 1 (e.g., "Evacuate")
        // No button = Option 2 (e.g., "Fight the fire")
        if (QuestionUIManager.Instance.yesButton != null)
        {
            QuestionUIManager.Instance.yesButton.onClick.RemoveAllListeners();
            QuestionUIManager.Instance.yesButton.onClick.AddListener(OnReplyOption1);
        }

        if (QuestionUIManager.Instance.noButton != null)
        {
            QuestionUIManager.Instance.noButton.onClick.RemoveAllListeners();
            QuestionUIManager.Instance.noButton.onClick.AddListener(OnReplyOption2);
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

    void OnReplyOption1()
    {
        // Check if this is the correct answer
        if (isOption1Correct)
        {
            // Correct answer
            UpdateDialogueText(correctDialogue);
            StartSpeech(correctDialogue);
            questionAnswered = true;
            StartCoroutine(DelayedEvacuation());
        }
        else
        {
            // Wrong answer
            UpdateDialogueText(wrongDialogue);
            StartSpeech(wrongDialogue);
            
            // Track the error in FireScoreTracker
            if (FireScoreTracker.Instance != null)
            {
                FireScoreTracker.Instance.OnWrongNPCResponse(); // This already calls OnTaskError internally
            }
            
            StartCoroutine(ResetDialogueAfterDelay());
        }
        
        isDialogueActive = false;
    }

    void OnReplyOption2()
    {
        // Check if this is the correct answer
        if (!isOption1Correct)
        {
            // Correct answer
            UpdateDialogueText(correctDialogue);
            StartSpeech(correctDialogue);
            questionAnswered = true;
            StartCoroutine(DelayedEvacuation());
        }
        else
        {
            // Wrong answer
            UpdateDialogueText(wrongDialogue);
            StartSpeech(wrongDialogue);
            
            // Track the error in FireScoreTracker
            if (FireScoreTracker.Instance != null)
            {
                FireScoreTracker.Instance.OnTaskError("Wrong NPC evacuation advice");
            }
            
            StartCoroutine(ResetDialogueAfterDelay());
        }
    }

    /// <summary>
    /// Updates the dialogue text in the QuestionUIManager
    /// </summary>
    private void UpdateDialogueText(string text)
    {
        if (QuestionUIManager.Instance != null && QuestionUIManager.Instance.questionTextUI != null)
        {
            QuestionUIManager.Instance.questionTextUI.text = text;
        }
    }

    IEnumerator ResetDialogueAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        
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

    IEnumerator DelayedEvacuation()
    {
        // Wait a moment for the player to read/hear the response
        yield return new WaitForSeconds(1.5f);
        
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
        
        // Start evacuation
        yield return StartCoroutine(EvacuateNPC());
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

        // Add this line to track NPC evacuation completion
if (FireScoreTracker.Instance != null)
{
    FireScoreTracker.Instance.OnNPCEvacuated();
}

        // Stop any ongoing speech before destroying
        if (enableTTS && isSpeaking && ttsSpeaker != null)
        {
            ttsSpeaker.Stop();
        }

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
        if (enableTTS && isSpeaking && ttsSpeaker != null)
        {
            ttsSpeaker.Stop();
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
    /// Check if NPC is currently speaking
    /// </summary>
    public bool IsSpeaking()
    {
        return isSpeaking;
    }
}