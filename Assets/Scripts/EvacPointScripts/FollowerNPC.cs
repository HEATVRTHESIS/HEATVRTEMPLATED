using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.AI;
using Meta.WitAi.TTS.Utilities;
using Meta.WitAi.TTS.Data;

public class FollowerNPC : MonoBehaviour
{
    [Header("Navigation")]
    public NavMeshAgent navAgent;
    
    [Header("Task Settings")]
    public HighlightableObject targetObject;
    
    [Header("VR Interaction")]
    public GameObject interactionIndicator;
    public Transform rightControllerTransform;
    public InputActionProperty talkAction;
    
    [Header("Dialogue")]
    [TextArea(2, 4)]
    public string initialDialogue = "I don't know where to go! Can you help me?";
    [TextArea(2, 4)]
    public string followingDialogue = "Okay, I'll follow you!";
    
    [Header("Error Dialogue")]
    [Tooltip("The dialogue lines to display when NPC is left too far behind.")]
    public string[] leftBehindDialogue;

    [Tooltip("The dialogue lines to display when player gives wrong answer.")]
    public string[] wrongAnswerDialogue;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip errorSound;
    
    [Header("NPC Settings")]
    public Animator npcAnimator;
    public Transform playerTransform;

    [Header("Distance Tracking")]
    [Tooltip("Distance at which NPC is considered left behind")]
    public float maxFollowDistance = 15f;
    
    [Tooltip("Time player must be far before triggering left behind")]
    public float leftBehindDelay = 5f;

    [Header("Meta Voice TTS Settings")]
    public bool enableTTS = true;
    public TTSSpeaker ttsSpeaker;
    public string voicePresetName = "WITSCODY";
    
    private bool isPlayerPointingAtNPC = false;
    private bool isDialogueActive = false;
    private bool isFollowing = false;
    private bool isSpeaking = false;
    private string currentSpeechText;
    private bool hasPlayedLeftBehindDialogue = false;
    private bool hasPlayedWrongAnswerDialogue = false;
    private float distanceTimer = 0f;

    void Start()
    {
        interactionIndicator.SetActive(false);
        talkAction.action.Enable();

        if (targetObject == null)
        {
            targetObject = GetComponent<HighlightableObject>();
        }

        if (ttsSpeaker == null && enableTTS)
        {
            ttsSpeaker = GetComponent<TTSSpeaker>();
            if (ttsSpeaker == null)
            {
                Debug.LogError($"FollowerNPC on {gameObject.name}: TTSSpeaker not found!");
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
            ttsSpeaker.Stop();
        }

        if (!string.IsNullOrEmpty(voicePresetName))
        {
            ttsSpeaker.VoiceID = voicePresetName;
        }

        currentSpeechText = text;
        ttsSpeaker.Speak(text);
    }

    void Update()
    {
        if (isFollowing)
        {
            FollowPlayer();
            CheckIfLeftBehind();
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

    void CheckIfLeftBehind()
    {
        if (playerTransform == null || hasPlayedLeftBehindDialogue) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (distance > maxFollowDistance)
        {
            distanceTimer += Time.deltaTime;

            if (distanceTimer >= leftBehindDelay)
            {
                OnNPCLeftBehind();
            }
        }
        else
        {
            distanceTimer = 0f;
        }
    }

    void OnNPCLeftBehind()
    {
        hasPlayedLeftBehindDialogue = true;

        if (ErrorTracker.Instance != null)
            ErrorTracker.Instance.RecordEvacuationNPCLeftBehindError();

        if (audioSource != null && errorSound != null)
            audioSource.PlayOneShot(errorSound);

        VRDialogueSystem dialogueSystem = FindObjectOfType<VRDialogueSystem>();
        if (dialogueSystem != null && leftBehindDialogue != null && leftBehindDialogue.Length > 0)
        {
            dialogueSystem.StartDialog(leftBehindDialogue);
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
            Debug.LogError("QuestionUIManager.Instance is null!");
            return;
        }

        if (QuestionUIManager.Instance.questionCanvas.activeSelf && !isDialogueActive)
        {
            Debug.LogWarning($"{gameObject.name}: Question UI already active.");
            return;
        }

        isDialogueActive = true;
        interactionIndicator.SetActive(false);
        StartSpeech(initialDialogue);
        ShowFollowerQuestion();
    }

    private void ShowFollowerQuestion()
    {
        if (QuestionUIManager.Instance == null)
        {
            Debug.LogError("Cannot show follower question!");
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
            QuestionUIManager.Instance.yesButton.onClick.AddListener(OnFollowButtonClicked);
        }

        if (QuestionUIManager.Instance.noButton != null)
        {
            QuestionUIManager.Instance.noButton.onClick.RemoveAllListeners();
            QuestionUIManager.Instance.noButton.onClick.AddListener(OnCancelButtonClicked);
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

    void OnFollowButtonClicked()
    {
        if (QuestionUIManager.Instance != null && QuestionUIManager.Instance.questionTextUI != null)
        {
            QuestionUIManager.Instance.questionTextUI.text = followingDialogue;
        }

        StartSpeech(followingDialogue);
        StartCoroutine(StartFollowingAfterDelay(1.5f));
    }

    void OnCancelButtonClicked()
    {
        if (ErrorTracker.Instance != null)
            ErrorTracker.Instance.RecordEvacuationNPCNotRescuedError();

        if (audioSource != null && errorSound != null)
            audioSource.PlayOneShot(errorSound);

        if (!hasPlayedWrongAnswerDialogue)
        {
            hasPlayedWrongAnswerDialogue = true;
            VRDialogueSystem dialogueSystem = FindObjectOfType<VRDialogueSystem>();
            if (dialogueSystem != null && wrongAnswerDialogue != null && wrongAnswerDialogue.Length > 0)
            {
                dialogueSystem.StartDialog(wrongAnswerDialogue);
            }
        }

        if (enableTTS && isSpeaking && ttsSpeaker != null)
        {
            ttsSpeaker.Stop();
        }

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

    IEnumerator StartFollowingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (QuestionUIManager.Instance != null)
        {
            QuestionUIManager.Instance.HideQuestion();
            
            if (QuestionUIManager.Instance.questionImage != null)
            {
                QuestionUIManager.Instance.questionImage.gameObject.SetActive(true);
            }
        }
        
        isDialogueActive = false;
        isFollowing = true;
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
            navAgent.SetDestination(playerTransform.position);
            
            if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
            {
                if (npcAnimator != null)
                {
                    npcAnimator.SetTrigger("Idle");
                }
            }
            else
            {
                if (npcAnimator != null)
                {
                    npcAnimator.SetTrigger("Walk");
                }
            }
        }
    }

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

        if (enableTTS && isSpeaking && ttsSpeaker != null)
        {
            ttsSpeaker.Stop();
        }
    }

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

    public bool IsSpeaking() => isSpeaking;
    public bool IsFollowing() => isFollowing;
}