using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Meta.WitAi.TTS.Utilities;
using Meta.WitAi.TTS.Data;

public class NPCInteraction : CustomTaskController
{
    public GameObject interactionIndicator;

    private string initialDialogue = "There's a fire, what should we do?";
    private string correctDialogue = "Alright! I will evacuate and call for help.";
    private string wrongDialogue = "That doesn't sound right. Try again.";

    public Transform rightControllerTransform;
    public Animator npcAnimator;
    public float evacuationSpeed = 2f;
    public Transform evacuationPoint;

    [Header("Task Settings")]
    public HighlightableObject targetObject;

    [Header("Dialogue Settings")]
    public bool isOption1Correct = true;

    [Header("Error Dialogue")]
    [Tooltip("The dialogue lines to display on first error.")]
    public string[] taskErrorDialogue;

    [Header("Audio")]
    public AudioClip errorSound;

    [Header("UI")]
    public PopupManager popupManager;

    public InputActionProperty talkAction;

    [Header("Meta Voice TTS Settings")]
    public bool enableTTS = true;
    public TTSSpeaker ttsSpeaker;

    private bool isPlayerPointingAtNPC = false;
    private bool isDialogueActive = false;
    private bool isSpeaking = false;
    private string currentSpeechText;
    private bool questionAnswered = false;
    private bool hasPlayedErrorDialogue = false;

    void Start()
    {
        interactionIndicator.SetActive(false);
        talkAction.action.Enable();

        if (targetObject == null)
        {
            targetObject = GetComponent<HighlightableObject>();
        }

        totalItems = 1;

        if (ttsSpeaker == null && enableTTS)
        {
            ttsSpeaker = GetComponent<TTSSpeaker>();
            if (ttsSpeaker == null)
            {
                Debug.LogError($"NPCInteraction on {gameObject.name}: TTSSpeaker component not found!");
            }
        }

        if (enableTTS && ttsSpeaker != null)
        {
            ttsSpeaker.Events.OnPlaybackStart.AddListener(OnSpeechStart);
            ttsSpeaker.Events.OnPlaybackComplete.AddListener(OnSpeechComplete);
            ttsSpeaker.Events.OnPlaybackCancelled.AddListener(OnSpeechCancelled);
        }
    }

    void OnDestroy()
    {
        if (enableTTS && ttsSpeaker != null)
        {
            ttsSpeaker.Events.OnPlaybackStart.RemoveListener(OnSpeechStart);
            ttsSpeaker.Events.OnPlaybackComplete.RemoveListener(OnSpeechComplete);
            ttsSpeaker.Events.OnPlaybackCancelled.RemoveListener(OnSpeechCancelled);
            ttsSpeaker.Stop();
        }
    }

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

    private void StartSpeech(string text)
    {
        if (!enableTTS || ttsSpeaker == null || string.IsNullOrEmpty(text.Trim()))
            return;

        if (VRDialogueSystem.IsTTSMuted)
            return;

        if (isSpeaking)
        {
            ttsSpeaker.Stop(currentSpeechText);
        }

        currentSpeechText = text;
        ttsSpeaker.SpeakQueued(text);
    }

    public override void InitializeTask()
    {
        base.InitializeTask();
        Debug.Log($"NPC evacuation task '{taskName}' initialized.");
    }

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
            Debug.LogError("QuestionUIManager.Instance is null!");
            return;
        }

        isDialogueActive = true;
        interactionIndicator.SetActive(false);
        StartSpeech(initialDialogue);
        ShowNPCQuestion();
    }

    private void ShowNPCQuestion()
    {
        if (QuestionUIManager.Instance == null)
        {
            Debug.LogError("Cannot show NPC question: QuestionUIManager.Instance is null!");
            return;
        }

        if (QuestionUIManager.Instance.questionTextUI != null)
        {
            QuestionUIManager.Instance.questionTextUI.text = initialDialogue;
        }

        if (QuestionUIManager.Instance.questionImage != null)
        {
            QuestionUIManager.Instance.questionImage.gameObject.SetActive(false);
        }

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

        ShowQuestionForNPC();
    }

    private void ShowQuestionForNPC()
    {
        if (QuestionUIManager.Instance == null || QuestionUIManager.Instance.questionCanvas == null)
            return;

        Transform target = targetObject != null ? targetObject.transform : transform;
        
        QuestionUIManager.Instance.questionCanvas.SetActive(true);
        
        if (QuestionUIManager.Instance.cameraTransform != null)
        {
            Vector3 basePosition = target.position + Vector3.up * QuestionUIManager.Instance.heightOffset;
            Vector3 cameraToTarget = (basePosition - QuestionUIManager.Instance.cameraTransform.position).normalized;
            Vector3 uiPosition = basePosition - cameraToTarget * QuestionUIManager.Instance.distanceFromObject;
            
            QuestionUIManager.Instance.questionCanvas.transform.position = uiPosition;
            
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
        if (isOption1Correct)
        {
            UpdateDialogueText(correctDialogue);
            StartSpeech(correctDialogue);
            questionAnswered = true;
            
            // ✅ SOLUTION 2: Complete task IMMEDIATELY when correct answer is given
            CompleteTask();
            if (FireScoreTracker.Instance != null)
            {
                FireScoreTracker.Instance.OnNPCEvacuated();
            }
            
            StartCoroutine(DelayedEvacuation());
        }
        else
        {
            HandleWrongAnswer();
        }
        
        isDialogueActive = false;
    }

    void OnReplyOption2()
    {
        if (!isOption1Correct)
        {
            UpdateDialogueText(correctDialogue);
            StartSpeech(correctDialogue);
            questionAnswered = true;
            
            // ✅ SOLUTION 2: Complete task IMMEDIATELY when correct answer is given
            CompleteTask();
            if (FireScoreTracker.Instance != null)
            {
                FireScoreTracker.Instance.OnNPCEvacuated();
            }
            
            StartCoroutine(DelayedEvacuation());
        }
        else
        {
            HandleWrongAnswer();
        }
    }

    private void HandleWrongAnswer()
    {
        UpdateDialogueText(wrongDialogue);
        StartSpeech(wrongDialogue);
        
        if (FireScoreTracker.Instance != null)
        {
            FireScoreTracker.Instance.OnWrongNPCResponse();
        }

        if (ErrorTracker.Instance != null)
            ErrorTracker.Instance.RecordFireNPCError();

        if (audioSource != null && errorSound != null)
            audioSource.PlayOneShot(errorSound);

        if (!hasPlayedErrorDialogue)
        {
            hasPlayedErrorDialogue = true;
            VRDialogueSystem dialogueSystem = FindObjectOfType<VRDialogueSystem>();
            if (dialogueSystem != null && taskErrorDialogue != null && taskErrorDialogue.Length > 0)
            {
                dialogueSystem.StartDialog(taskErrorDialogue);
            }
        }
        
        StartCoroutine(ResetDialogueAfterDelay());
    }

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
        
        if (QuestionUIManager.Instance != null)
        {
            QuestionUIManager.Instance.HideQuestion();
            
            if (QuestionUIManager.Instance.questionImage != null)
            {
                QuestionUIManager.Instance.questionImage.gameObject.SetActive(true);
            }
        }
        
        isDialogueActive = false;
    }

    IEnumerator DelayedEvacuation()
    {
        yield return new WaitForSeconds(1.5f);
        
        if (QuestionUIManager.Instance != null)
        {
            QuestionUIManager.Instance.HideQuestion();
            
            if (QuestionUIManager.Instance.questionImage != null)
            {
                QuestionUIManager.Instance.questionImage.gameObject.SetActive(true);
            }
        }
        
        yield return StartCoroutine(EvacuateNPC());
    }

    IEnumerator EvacuateNPC()
    {
        npcAnimator.SetTrigger("Walk");

        while (Vector3.Distance(transform.position, evacuationPoint.position) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, evacuationPoint.position, evacuationSpeed * Time.deltaTime);
            transform.LookAt(evacuationPoint);
            yield return null;
        }

        // Task already completed when correct answer was given
        // Just clean up TTS and hide the NPC
        if (enableTTS && isSpeaking && ttsSpeaker != null)
        {
            ttsSpeaker.Stop();
        }

        yield return new WaitForSeconds(0.5f);
        
        // Hide instead of destroy to prevent evaluation errors
        gameObject.SetActive(false);
    }

    public override void StartTask()
    {
        if (IsTaskCompleted()) return;

        if (targetObject != null)
        {
            targetObject.SetHighlight(true);
        }
    }

    public override void EndTask()
    {
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

    public bool IsSpeaking()
    {
        return isSpeaking;
    }
}