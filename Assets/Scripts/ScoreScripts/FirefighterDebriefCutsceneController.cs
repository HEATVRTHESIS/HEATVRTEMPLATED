using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using Crosstales.RTVoice;
using Crosstales.RTVoice.Model;

/// <summary>
/// Post-evacuation cutscene controller with integrated dialogue, TTS, and mascot animation.
/// Features a firefighter speaking to the player who just evacuated.
/// Does NOT pause time - animations work normally.
/// </summary>
public class FirefighterDebriefCutsceneController : MonoBehaviour
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
        "Hey! You made it out! Are you injured? Can you breathe okay?",
        "Good, good. I'm Captain Rodriguez with Station 47.",
        "You did great getting yourself out of there. That smoke was thick.",
        "Listen, I need you to stay right here in this safe zone.",
        "We're at least 100 feet from the building - that's where you need to stay.",
        "Don't go back in for anything. Not for belongings, not for anyone.",
        "If you know anyone who's still inside, tell me now.",
        "Give me details - what floor, what department, any landmarks.",
        "My crew is going in, and that information could save lives.",
        "You see any other survivors coming out, you direct them here to me.",
        "When the paramedics arrive, let them check you out.",
        "Even if you feel fine, smoke inhalation can cause problems hours later.",
        "You did everything right in there. You kept your head, you got out.",
        "That's exactly what you're supposed to do.",
        "Now let us handle the rest. We've got this."
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
    
    [Header("RT-Voice Settings")]
    [Tooltip("Enable text-to-speech using RT-Voice")]
    public bool enableTTS = true;
    [Tooltip("AudioSource for RT-Voice speech output")]
    public AudioSource speechAudioSource;
    [Tooltip("Voice to use for speech (leave empty for default)")]
    public string voiceName = "";
    [Tooltip("Speech rate (0.1 to 3.0, 1.0 is normal speed)")]
    [Range(0.1f, 3.0f)]
    public float speechRate = 1.0f;
    [Tooltip("Speech volume (0.0 to 1.0)")]
    [Range(0.0f, 1.0f)]
    public float speechVolume = 1.0f;
    
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
    private Voice selectedVoice;
    private string currentSpeechId;

    void Start()
    {
        // Try to find CharacterController on player rig
        if (playerRig != null)
        {
            characterController = playerRig.GetComponent<CharacterController>();
        }
        
        // Setup AudioSource for RT-Voice if not assigned
        if (speechAudioSource == null && enableTTS)
        {
            speechAudioSource = gameObject.GetComponent<AudioSource>();
            if (speechAudioSource == null)
            {
                speechAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
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
        
        // Subscribe to RT-Voice events if TTS is enabled
        if (enableTTS)
        {
            Speaker.Instance.OnSpeakStart += OnSpeechStart;
            Speaker.Instance.OnSpeakComplete += OnSpeechComplete;
            Speaker.Instance.OnVoicesReady += OnVoicesReady;
        }
    }
    
    void OnDisable()
    {
        // Unsubscribe from input action
        if (nextLineAction.action != null)
        {
            nextLineAction.action.performed -= OnNextLinePressed;
            nextLineAction.action.Disable();
        }
        
        // Unsubscribe from RT-Voice events
        if (enableTTS && Speaker.Instance != null)
        {
            Speaker.Instance.OnSpeakStart -= OnSpeechStart;
            Speaker.Instance.OnSpeakComplete -= OnSpeechComplete;
            Speaker.Instance.OnVoicesReady -= OnVoicesReady;
        }
    }
    
    // RT-Voice event handlers
    private void OnVoicesReady()
    {
        if (!string.IsNullOrEmpty(voiceName))
        {
            selectedVoice = Speaker.Instance.VoiceForName(voiceName);
        }
    }
    
    private void OnSpeechStart(Wrapper wrapper)
    {
        if (wrapper.Uid == currentSpeechId)
        {
            isSpeaking = true;
            if (!isTyping)
            {
                StartMouthAnimation();
            }
        }
    }
    
    private void OnSpeechComplete(Wrapper wrapper)
    {
        if (wrapper.Uid == currentSpeechId)
        {
            isSpeaking = false;
            if (!isTyping)
            {
                StopMouthAnimation();
            }
        }
    }
    
    // Input handler
    private void OnNextLinePressed(InputAction.CallbackContext context)
    {
        if (!cutsceneActive) return;
        
        // If speech is playing, skip it
        if (isSpeaking && enableTTS)
        {
            Speaker.Instance.Silence();
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
        
        if (enableTTS && isSpeaking)
        {
            Speaker.Instance.Silence();
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
        
        if (!isSpeaking)
        {
            StopMouthAnimation();
        }
    }
    
    private void StartSpeech(string text)
    {
        if (!enableTTS || Speaker.Instance == null || string.IsNullOrEmpty(text.Trim()))
            return;
        
        currentSpeechId = System.Guid.NewGuid().ToString();
        
        Speaker.Instance.SpeakNative(
            text,
            selectedVoice,
            speechRate,
            1.0f,
            speechVolume
        );
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
                evalScreen.CalculateAndDisplayResults();
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
        if (enableTTS && Speaker.Instance != null)
        {
            Speaker.Instance.Silence();
        }
    }
}