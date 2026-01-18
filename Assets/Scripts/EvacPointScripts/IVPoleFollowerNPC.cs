using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.AI;
using Meta.WitAi.TTS.Utilities;
using Meta.WitAi.TTS.Data;

public class IVPoleFollowerNPC : MonoBehaviour
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
    public string initialDialogue = "I need my IV! Can you bring it to me?";
    [TextArea(2, 4)]
    public string followingDialogue = "Thank you! I'll follow my IV pole now.";
    [TextArea(2, 4)]
    public string tooFarDialogue = "Wait! My IV pole is too far away!";
    
    [Header("Error Dialogue")]
    [Tooltip("The dialogue lines to display when IV pole is left too far behind.")]
    public string[] leftBehindDialogue;

    [Tooltip("The dialogue lines to display when player gives wrong answer.")]
    public string[] wrongAnswerDialogue;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip errorSound;
    
    [Header("NPC Settings")]
    public Animator npcAnimator;
    public Transform ivPoleTransform;
    public float followDistance = 2.5f;
    public float stopFollowingDistance = 15f;

    [Header("Distance Tracking")]
    [Tooltip("Time IV pole must be far before triggering left behind")]
    public float leftBehindDelay = 5f;

    [Header("Meta Voice TTS Settings")]
    public bool enableTTS = true;
    public TTSSpeaker ttsSpeaker;
    public string voicePresetName = "WITSREBECCA";
    
    private bool isPlayerPointingAtNPC = false;
    private bool isDialogueActive = false;
    private bool isFollowing = false;
    private bool isSpeaking = false;
    private bool hasSpokenTooFarWarning = false;
    private bool hasPlayedLeftBehindDialogue = false;
    private bool hasPlayedWrongAnswerDialogue = false;
    private string currentSpeechText;
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
                Debug.LogError($"IVPoleFollowerNPC on {gameObject.name}: TTSSpeaker not found!");
            }
        }

        if (enableTTS && ttsSpeaker != null)
        {
            ttsSpeaker.Events.OnPlaybackStart.AddListener(OnSpeechStart);
            ttsSpeaker.Events.OnPlaybackComplete.AddListener(OnSpeechComplete);
            ttsSpeaker.Events.OnPlaybackCancelled.AddListener(OnSpeechCancelled);
        }

        if (navAgent != null)
        {
            navAgent.stoppingDistance = followDistance;
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
        ShowIVPoleQuestion();
    }

    private void ShowIVPoleQuestion()
    {
        if (QuestionUIManager.Instance == null)
        {
            Debug.LogError("Cannot show IV pole question!");
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
        hasSpokenTooFarWarning = false;
    }

    void FollowIVPole()
    {
        if (ivPoleTransform == null)
        {
            Debug.LogWarning("IV Pole transform not assigned!");
            return;
        }

        float distanceToIVPole = Vector3.Distance(transform.position, ivPoleTransform.position);
        
        if (distanceToIVPole > stopFollowingDistance)
        {
            if (navAgent != null && !navAgent.isStopped)
            {
                navAgent.isStopped = true;
                if (npcAnimator != null)
                {
                    npcAnimator.SetTrigger("Idle");
                }
            }
            
            if (!hasSpokenTooFarWarning)
            {
                StartSpeech(tooFarDialogue);
                hasSpokenTooFarWarning = true;
            }

            // Track how long IV pole has been too far
            if (!hasPlayedLeftBehindDialogue)
            {
                distanceTimer += Time.deltaTime;

                if (distanceTimer >= leftBehindDelay)
                {
                    OnIVPoleLeftBehind();
                }
            }
            
            return;
        }
        
        hasSpokenTooFarWarning = false;
        distanceTimer = 0f;

        if (navAgent != null)
        {
            if (navAgent.isStopped)
            {
                navAgent.isStopped = false;
            }

            navAgent.SetDestination(ivPoleTransform.position);
            
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

    void OnIVPoleLeftBehind()
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
        if (ivPoleTransform == null)
        {
            GameObject ivPole = GameObject.FindGameObjectWithTag("IVPole");
            if (ivPole != null)
            {
                ivPoleTransform = ivPole.transform;
            }
        }
    }

    public bool IsSpeaking() => isSpeaking;
    public bool IsFollowing() => isFollowing;

    public void SetIVPole(Transform newIVPole)
    {
        ivPoleTransform = newIVPole;
    }
}