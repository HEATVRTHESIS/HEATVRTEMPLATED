using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using Meta.WitAi.TTS.Utilities; // NEW: Meta Voice SDK namespace
using Meta.WitAi.TTS.Data; // NEW: For TTSClipData

// Removed: using Crosstales.RTVoice;
// Removed: using Crosstales.RTVoice.Model;

/// <summary>
/// Post-evacuation cutscene controller with integrated dialogue, TTS, and mascot animation.
/// Does NOT pause time - animations work normally.
/// </summary>
public class FireEvacuationCutsceneController : MonoBehaviour
{
    [Header("Player References")]
    [Tooltip("Assign the XR Origin or OVRPlayerController")]
    public GameObject playerRig;
    [Tooltip("If using a specific movement script, assign it here")]
    public MonoBehaviour playerMovementScript;
    
    [Header("Animation Setup")]
    public Animator npcAnimator;
    [Tooltip("Name of the talking animation state in Animator")]
    public string talkingAnimationName = "Talking";
    [Tooltip("Name of the idle animation state in Animator")]
    public string idleAnimationName = "Idle";
    [Tooltip("Name of the celebration/thumbs up animation (optional)")]
    public string congratulationAnimationName = "ThumbsUp";
    
    [Header("Dialogue Content")]
    [TextArea(3, 10)]
    public List<string> dialogueLines = new List<string>()
    {
        "Excellent work! You've successfully evacuated the building.",
        "Let me remind you of the critical steps after exiting a burning building.",
        "First, move to a safe distance - at least 100 feet - and stay there.",
        "Second, report to the authorities. Give them any information you have about people still inside.",
        "Third, do not, under any circumstances, go back inside the building.",
        "Fourth, wait for medical personnel to check you for smoke inhalation, even if you feel fine.",
        "You handled the situation perfectly. Now, let the professionals take over."
    };
    
    [Header("Dialogue UI")]
    public GameObject dialogueCanvas;
    public TextMeshProUGUI dialogueText;
    [Tooltip("The speed at which characters are typed out")]
    public float typingSpeed = 0.05f;
    [Tooltip("The Input Action for progressing dialogue")]
    public InputActionProperty nextLineAction;
    
    [Header("Mascot Animation")]
    [Tooltip("The Image component that displays the mascot sprite")]
    public Image mascotImage;
    [Tooltip("The sprite for when the mascot's mouth is closed")]
    public Sprite mouthClosedSprite;
    [Tooltip("The sprite for when the mascot's mouth is open")]
    public Sprite mouthOpenSprite;
    [Tooltip("How fast the mascot's mouth animates")]
    public float mouthAnimationSpeed = 0.15f;
    
    // --- NEW: Meta Voice TTS Settings ---
    [Header("Meta Voice TTS Settings")]
    [Tooltip("Enable text-to-speech using Meta Voice SDK")]
    public bool enableTTS = true;
    [Tooltip("TTSSpeaker component for Meta Voice SDK speech output")]
    public TTSSpeaker ttsSpeaker;
    // Removed RT-Voice Settings: speechAudioSource, voiceName, speechRate, speechVolume
    
    [Header("Evaluation Screen")]
    public GameObject evaluationScreen;
    
    [Header("Cutscene Settings")]
    public bool startCutsceneOnStart = true;
    public float cutsceneStartDelay = 1f;
    
    // Private fields
    private bool cutsceneActive = false;
    private bool isTyping = false;
    private bool isSpeaking = false;
    private Queue<string> dialogueQueue = new Queue<string>();
    private string currentLine;
    private Coroutine typingCoroutine;
    private Coroutine mouthAnimationCoroutine;
    private CharacterController characterController;
    private bool characterControllerWasEnabled;
    
    // NEW: Fields for Meta Voice tracking
    private string currentSpeechText;
    // Removed RT-Voice Fields: selectedVoice, currentSpeechId

    void Awake()
    {
        // Setup TTSSpeaker if not assigned
        if (ttsSpeaker == null && enableTTS)
        {
            ttsSpeaker = GetComponent<TTSSpeaker>();
            if (ttsSpeaker == null)
            {
                Debug.LogError($"FireEvacuationCutsceneController on {gameObject.name}: TTSSpeaker component not found! Please add a TTSSpeaker component or assign it in the inspector.");
            }
        }
    }

    void Start()
    {
        // Try to find CharacterController on player rig
        if (playerRig != null)
        {
            characterController = playerRig.GetComponent<CharacterController>();
        }
        
        // Removed: Setup AudioSource for RT-Voice
        
        // Set initial mascot sprite to mouth closed
        if (mascotImage != null && mouthClosedSprite != null)
        {
            mascotImage.sprite = mouthClosedSprite;
        }
        
        // Hide dialogue canvas initially
        if (dialogueCanvas != null)
            dialogueCanvas.SetActive(false);
        
        // Hide evaluation screen initially
        if (evaluationScreen != null)
            evaluationScreen.SetActive(false);
        
        if (startCutsceneOnStart)
        {
            Invoke(nameof(StartCutscene), cutsceneStartDelay);
        }
    }
    
    void OnEnable()
    {
        // Subscribe to input action
        if (nextLineAction.action != null)
        {
            nextLineAction.action.performed += OnNextLinePressed;
            nextLineAction.action.Enable();
        }
        
        // NEW: Subscribe to Meta Voice TTS events
        if (enableTTS && ttsSpeaker != null)
        {
            ttsSpeaker.Events.OnPlaybackStart.AddListener(OnSpeechStart);
            ttsSpeaker.Events.OnPlaybackComplete.AddListener(OnSpeechComplete);
            ttsSpeaker.Events.OnPlaybackCancelled.AddListener(OnSpeechCancelled); // FIX: 3-argument listener
        }
        // Removed RT-Voice subscriptions
    }
    
    void OnDisable()
    {
        // Unsubscribe from input action
        if (nextLineAction.action != null)
        {
            nextLineAction.action.performed -= OnNextLinePressed;
            nextLineAction.action.Disable();
        }
        
        // NEW: Unsubscribe from Meta Voice TTS events
        if (enableTTS && ttsSpeaker != null)
        {
            ttsSpeaker.Events.OnPlaybackStart.RemoveListener(OnSpeechStart);
            ttsSpeaker.Events.OnPlaybackComplete.RemoveListener(OnSpeechComplete);
            ttsSpeaker.Events.OnPlaybackCancelled.RemoveListener(OnSpeechCancelled);
        }
        // Removed RT-Voice unsubscriptions
    }
    
    #region Meta Voice Event Handlers
    // Removed OnVoicesReady()
    
    private void OnSpeechStart(TTSSpeaker speaker, TTSClipData clipData)
    {
        if (clipData.textToSpeak == currentSpeechText)
        {
            isSpeaking = true;
            if (!isTyping)
            {
                StartMouthAnimation();
            }
        }
    }
    
    private void OnSpeechComplete(TTSSpeaker speaker, TTSClipData clipData)
    {
        if (clipData.textToSpeak == currentSpeechText)
        {
            isSpeaking = false;
            if (!isTyping)
            {
                StopMouthAnimation();
            }
        }
    }

    // NEW: Handler for OnPlaybackCancelled (3-argument signature)
    private void OnSpeechCancelled(TTSSpeaker speaker, TTSClipData clipData, string reason)
    {
        // Treat cancellation the same way as completion
        if (clipData.textToSpeak == currentSpeechText)
        {
            isSpeaking = false;
            if (!isTyping)
            {
                StopMouthAnimation();
            }
        }
    }
    #endregion
    
    // Input handler
    private void OnNextLinePressed(InputAction.CallbackContext context)
    {
        if (!cutsceneActive) return;
        
        // If speech is playing, skip it
        if (isSpeaking && enableTTS && ttsSpeaker != null)
        {
            ttsSpeaker.Stop(); // NEW: Use ttsSpeaker.Stop()
            return;
        }
        
        // If typing, complete the line immediately
        if (isTyping)
        {
            CompleteLine();
        }
        else
        {
            // Move to next line
            DisplayNextLine();
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
        // Start with congratulation animation if available
        if (!string.IsNullOrEmpty(congratulationAnimationName))
        {
            PlayAnimation(congratulationAnimationName);
            yield return new WaitForSeconds(2f);
        }
        
        // Switch to talking animation
        PlayAnimation(talkingAnimationName);
        
        // Show dialogue canvas
        if (dialogueCanvas != null)
            dialogueCanvas.SetActive(true);
        
        // Queue up all dialogue lines
        dialogueQueue.Clear();
        foreach (string line in dialogueLines)
        {
            dialogueQueue.Enqueue(line);
        }
        
        // Start first line
        DisplayNextLine();
        
        // Wait until all dialogue is done
        while (dialogueQueue.Count > 0 || isTyping || isSpeaking)
        {
            yield return null;
        }
        
        // Hide dialogue canvas
        if (dialogueCanvas != null)
            dialogueCanvas.SetActive(false);
        
        // Return to idle
        PlayAnimation(idleAnimationName);
        
        // Small pause before showing evaluation
        yield return new WaitForSeconds(0.5f);
        
        // Show final evaluation screen
        ShowEvaluationScreen();
    }
    
    private void DisplayNextLine()
    {
        if (dialogueQueue.Count == 0)
        {
            return;
        }
        
        // Stop any ongoing typing or speech
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        
        if (enableTTS && isSpeaking && ttsSpeaker != null)
        {
            ttsSpeaker.Stop(); // NEW: Stop Meta Voice speech
        }
        
        // Get next line
        currentLine = dialogueQueue.Dequeue();
        
        // Skip empty lines
        if (string.IsNullOrEmpty(currentLine.Trim()))
        {
            DisplayNextLine();
            return;
        }
        
        // Start typing
        typingCoroutine = StartCoroutine(TypeLine(currentLine));
    }
    
    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogueText.text = "";
        
        // Start mouth animation
        StartMouthAnimation();
        
        // Start speech if enabled
        if (enableTTS)
        {
            StartSpeech(line);
        }
        
        // Type out each character
        foreach (char character in line.ToCharArray())
        {
            dialogueText.text += character;
            yield return new WaitForSeconds(typingSpeed);
        }
        
        isTyping = false;
        
        // Stop mouth animation if speech isn't playing
        if (!isSpeaking)
        {
            StopMouthAnimation();
        }
    }
    
    private void CompleteLine()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        
        isTyping = false;
        dialogueText.text = currentLine;
        
        // Ensure speech starts if it hasn't, in case the line was completed quickly
        if (enableTTS && !isSpeaking)
        {
            StartSpeech(currentLine);
        }
    }
    
    private void StartSpeech(string text)
    {
        if (!enableTTS || ttsSpeaker == null || string.IsNullOrEmpty(text.Trim()))
            return;
        
        currentSpeechText = text; // NEW: Set text to track completion/cancellation
        
        // NEW: Use TTSSpeaker's Speak()
        ttsSpeaker.Speak(text);
    }
    
    private void StartMouthAnimation()
    {
        if (mascotImage != null && mouthClosedSprite != null && mouthOpenSprite != null)
        {
            if (mouthAnimationCoroutine != null)
            {
                StopCoroutine(mouthAnimationCoroutine);
            }
            mouthAnimationCoroutine = StartCoroutine(AnimateMouth());
        }
    }
    
    private void StopMouthAnimation()
    {
        if (mouthAnimationCoroutine != null)
        {
            StopCoroutine(mouthAnimationCoroutine);
            mouthAnimationCoroutine = null;
        }
        
        if (mascotImage != null && mouthClosedSprite != null)
        {
            mascotImage.sprite = mouthClosedSprite;
        }
    }
    
    private IEnumerator AnimateMouth()
    {
        bool mouthOpen = false;
        
        // Animation runs while typing OR while speaking
        while (isTyping || isSpeaking)
        {
            if (mouthOpen)
            {
                mascotImage.sprite = mouthClosedSprite;
            }
            else
            {
                mascotImage.sprite = mouthOpenSprite;
            }
            
            mouthOpen = !mouthOpen;
            yield return new WaitForSeconds(mouthAnimationSpeed);
        }
        
        mascotImage.sprite = mouthClosedSprite;
    }
    
    private void PlayAnimation(string animationName)
    {
        if (npcAnimator == null || string.IsNullOrEmpty(animationName)) return;
        npcAnimator.Play(animationName);
    }
    
    private void ShowEvaluationScreen()
    {
        if (evaluationScreen != null)
        {
            evaluationScreen.SetActive(true);
            
            FinalEvaluationScreen evalScreen = evaluationScreen.GetComponent<FinalEvaluationScreen>();
            if (evalScreen != null)
            {
                // This method is assumed to exist on FinalEvaluationScreen
                // evalScreen.CalculateAndDisplayResults();
            }
        }
        
        PlayAnimation(idleAnimationName);
        EnablePlayerMovement();
        cutsceneActive = false;
        
        Debug.Log("Evaluation screen displayed. Cutscene complete.");
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
        if (enableTTS && ttsSpeaker != null)
        {
            ttsSpeaker.Stop(); // NEW: Use ttsSpeaker.Stop()
        }
        // Removed: RT-Voice Silence() call
    }
}