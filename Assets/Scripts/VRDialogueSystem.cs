using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Meta.WitAi.TTS.Utilities; // Meta Voice SDK namespace
using Meta.WitAi.TTS.Events; // For TTS events
using Meta.WitAi.TTS.Data; // For TTSClipData

/// <summary>
/// A text display system for VR that shows dialogue lines and progresses with user input.
/// Includes animated mascot that "talks" during text typing and Meta Voice SDK text-to-speech.
/// Pauses the game time scale during dialogue display.
/// </summary>
public class VRDialogueSystem : MonoBehaviour
{
    // --- Public Fields (Drag and drop these in the Unity Inspector) ---
    [Tooltip("The parent Canvas containing the UI elements.")]
    public GameObject dialogCanvas;
    [Tooltip("The TextMeshProUGUI component that will display the text.")]
    public TextMeshProUGUI dialogText;
    [Tooltip("The Input Action for the button to progress the dialogue (e.g., right XR Controller's secondary button).")]
    public InputActionProperty nextLineAction;
    [Tooltip("The speed at which characters are typed out. A smaller value is faster.")]
    public float typingSpeed = 0.05f;

    [Header("Time Control")]
    [Tooltip("Should the game time be paused while dialogue is displayed?")]
    public bool pauseTimeScale = true;

    [Header("Mascot Animation")]
    [Tooltip("The Image component that displays the mascot sprite.")]
    public Image mascotImage;
    [Tooltip("The sprite for when the mascot's mouth is closed.")]
    public Sprite mouthClosedSprite;
    [Tooltip("The sprite for when the mascot's mouth is open.")]
    public Sprite mouthOpenSprite;
    [Tooltip("How fast the mascot's mouth animates (seconds between sprite changes).")]
    public float mouthAnimationSpeed = 0.15f;

    [Header("Meta Voice TTS Settings")]
    [Tooltip("Enable text-to-speech using Meta Voice SDK")]
    public bool enableTTS = true;
    [Tooltip("TTSSpeaker component for Meta Voice SDK speech output")]
    public TTSSpeaker ttsSpeaker;
    [Tooltip("Wait for speech to complete before allowing next line")]
    public bool waitForSpeech = true;
    [Tooltip("Show text immediately when speech starts (disable typewriter for speech)")]
    public bool showTextImmediatelyWithSpeech = false;

    // --- Private Fields ---
    private Queue<string> _dialogLines = new Queue<string>();
    private bool _isDisplaying = false;
    private bool _isTyping = false;
    private bool _isSpeaking = false;
    private bool _speechStartedForCurrentLine = false; // Track if speech already started for this line
    private string _currentLine;
    private Coroutine _typingCoroutine;
    private Coroutine _mouthAnimationCoroutine;
    private float _originalTimeScale = 1f;
    private string _currentSpeechText; // Track current speech text for stop operations

    // Static mute state that can be controlled by VRPauseMenu
    public static bool IsTTSMuted { get; set; } = false;

    void Awake()
    {
        // Store the original time scale
        _originalTimeScale = Time.timeScale;

        // Setup TTSSpeaker if not assigned
        if (ttsSpeaker == null && enableTTS)
        {
            ttsSpeaker = GetComponent<TTSSpeaker>();
            if (ttsSpeaker == null)
            {
                Debug.LogError("VRDialogueSystem: TTSSpeaker component not found! Please add a TTSSpeaker component or assign it in the inspector.");
            }
        }

        // Ensure the initial state is hidden
        if (dialogCanvas != null)
        {
            dialogCanvas.SetActive(false);
        }

        // Set initial mascot sprite to mouth closed
        if (mascotImage != null && mouthClosedSprite != null)
        {
            mascotImage.sprite = mouthClosedSprite;
        }

        // Listen for the next line action button press
        if (nextLineAction.action != null)
        {
            nextLineAction.action.performed += OnNextLineButtonPressed;
        }
    }

    void OnEnable()
    {
        if (nextLineAction.action != null)
        {
            nextLineAction.action.Enable();
        }

        // Subscribe to Meta Voice TTS events if TTS is enabled
        if (enableTTS && ttsSpeaker != null)
        {
            ttsSpeaker.Events.OnPlaybackStart.AddListener(OnSpeechStart);
            ttsSpeaker.Events.OnPlaybackComplete.AddListener(OnSpeechComplete);
            ttsSpeaker.Events.OnPlaybackCancelled.AddListener(OnSpeechCancelled);
        }
    }

    void OnDisable()
    {
        if (nextLineAction.action != null)
        {
            nextLineAction.action.Disable();
        }

        // Unsubscribe from Meta Voice TTS events
        if (enableTTS && ttsSpeaker != null)
        {
            ttsSpeaker.Events.OnPlaybackStart.RemoveListener(OnSpeechStart);
            ttsSpeaker.Events.OnPlaybackComplete.RemoveListener(OnSpeechComplete);
            ttsSpeaker.Events.OnPlaybackCancelled.RemoveListener(OnSpeechCancelled);
        }
        
        // Restore time scale if dialogue is disabled while active
        if (_isDisplaying && pauseTimeScale)
        {
            RestoreTimeScale();
        }
    }

    /// <summary>
    /// Meta Voice TTS event: Called when speech playback starts
    /// </summary>
    private void OnSpeechStart(TTSSpeaker speaker, TTSClipData clipData)
    {
        if (clipData.textToSpeak == _currentSpeechText)
        {
            _isSpeaking = true;
            // Start mouth animation when speech begins
            if (!_isTyping) // Only if we're not already animating from typing
            {
                StartMouthAnimation();
            }
        }
    }

    /// <summary>
    /// Meta Voice TTS event: Called when speech playback completes
    /// </summary>
    private void OnSpeechComplete(TTSSpeaker speaker, TTSClipData clipData)
    {
        if (clipData.textToSpeak == _currentSpeechText)
        {
            _isSpeaking = false;
            // Stop mouth animation when speech ends (if not typing)
            if (!_isTyping)
            {
                StopMouthAnimation();
            }
        }
    }

    /// <summary>
    /// Meta Voice TTS event: Called when speech playback is cancelled
    /// </summary>
    private void OnSpeechCancelled(TTSSpeaker speaker, TTSClipData clipData, string reason)
    {
        if (clipData.textToSpeak == _currentSpeechText)
        {
            _isSpeaking = false;
            // Stop mouth animation when speech is cancelled (if not typing)
            if (!_isTyping)
            {
                StopMouthAnimation();
            }
        }
    }

    /// <summary>
    /// Starts the dialogue system with a new set of lines.
    /// This method should be called by external scripts (e.g., LevelManager, TaskManager).
    /// </summary>
    /// <param name="lines">An array of strings to display sequentially.</param>
    public void StartDialog(string[] lines)
    {
        // Clear any previous lines and reset
        _dialogLines.Clear();
        _isDisplaying = true;
        _isTyping = false;
        _isSpeaking = false;

        // Stop any ongoing speech
        if (enableTTS && ttsSpeaker != null)
        {
            ttsSpeaker.Stop();
        }

        // Add all new lines to the queue
        foreach (string line in lines)
        {
            _dialogLines.Enqueue(line);
        }

        // Show the canvas and start dialogue after a frame delay
        if (dialogCanvas != null)
        {
            dialogCanvas.SetActive(true);
        }

        // Pause time if enabled (after canvas is active)
        if (pauseTimeScale)
        {
            PauseTimeScale();
        }
        
        // Start dialogue with a frame delay to ensure canvas is properly activated
        StartCoroutine(StartDialogueAfterFrame());
    }

    /// <summary>
    /// Waits one frame after canvas activation before starting dialogue to ensure GameObject is active
    /// </summary>
    private IEnumerator StartDialogueAfterFrame()
    {
        yield return null; // Wait one frame
        DisplayNextLine();
    }

    /// <summary>
    /// Displays the next line in the queue, if any.
    /// </summary>
    private void DisplayNextLine()
    {
        // If there are more lines, show the next one
        if (_dialogLines.Count > 0)
        {
            _currentLine = _dialogLines.Dequeue();
            _speechStartedForCurrentLine = false; // Reset speech flag for new line
            
            // Show text immediately if the option is enabled
            if (showTextImmediatelyWithSpeech)
            {
                dialogText.text = _currentLine;
                _isTyping = false;
                
                // Start speech immediately
                if (enableTTS)
                {
                    StartSpeech(_currentLine);
                }
            }
            else
            {
                // Otherwise, start the typing animation
                _typingCoroutine = StartCoroutine(TypeLine(_currentLine));
            }
        }
        else
        {
            // No more lines, end the dialogue
            EndDialog();
        }
    }

    /// <summary>
    /// Handles the button press for progressing dialogue.
    /// </summary>
    private void OnNextLineButtonPressed(InputAction.CallbackContext context)
    {
        // If dialogue isn't active, ignore
        if (!_isDisplaying)
            return;

        // If typing is in progress, complete the current line
        if (_isTyping)
        {
            CompleteLine();
            return;
        }

        // If we're waiting for speech to complete, don't allow progressing
        if (waitForSpeech && _isSpeaking)
        {
            return;
        }

        // Stop any ongoing speech before moving to next line
        if (enableTTS && ttsSpeaker != null)
        {
            ttsSpeaker.Stop(_currentSpeechText);
        }

        // Otherwise, show the next line
        DisplayNextLine();
    }

    /// <summary>
    /// Starts text-to-speech for the given line using Meta Voice SDK
    /// </summary>
    private void StartSpeech(string text)
    {
        if (!enableTTS || ttsSpeaker == null || IsTTSMuted)
            return;

        _currentSpeechText = text;
        _speechStartedForCurrentLine = true;

        // Use SpeakQueued to ensure speech is properly queued
        ttsSpeaker.SpeakQueued(text);
    }

    /// <summary>
    /// A coroutine that "types" out a string character by character.
    /// Uses unscaled time so it works even when Time.timeScale is 0.
    /// </summary>
    private IEnumerator TypeLine(string line)
    {
        _isTyping = true;
        dialogText.text = ""; // Clear the text field before typing

        // Start the mouth animation
        StartMouthAnimation();

        // Start speech if enabled and not showing text immediately
        if (enableTTS && !showTextImmediatelyWithSpeech)
        {
            StartSpeech(line);
        }

        foreach (char character in line.ToCharArray())
        {
            dialogText.text += character;
            // Use unscaled time so typing continues even when time is paused
            yield return new WaitForSecondsRealtime(typingSpeed);
        }
        
        _isTyping = false; // Typing is complete

        // Stop the mouth animation if speech isn't playing
        if (!_isSpeaking)
        {
            StopMouthAnimation();
        }

        // Start speech after typing if not already started
        if (enableTTS && showTextImmediatelyWithSpeech && !_isSpeaking)
        {
            StartSpeech(line);
        }
    }

    /// <summary>
    /// Immediately completes the current line being typed.
    /// </summary>
    private void CompleteLine()
    {
        // Stop the typing coroutine
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
        }
        
        _isTyping = false;
        dialogText.text = _currentLine;

        // Stop the mouth animation if speech isn't playing
        if (!_isSpeaking)
        {
            StopMouthAnimation();
        }

        // Only start speech if it hasn't been started yet for this line
        if (enableTTS && !_speechStartedForCurrentLine && !_isSpeaking)
        {
            StartSpeech(_currentLine);
        }
    }

    /// <summary>
    /// Starts the mascot mouth animation coroutine.
    /// </summary>
    private void StartMouthAnimation()
    {
        if (mascotImage != null && mouthClosedSprite != null && mouthOpenSprite != null)
        {
            // Stop any existing animation first
            if (_mouthAnimationCoroutine != null)
            {
                StopCoroutine(_mouthAnimationCoroutine);
            }
            
            _mouthAnimationCoroutine = StartCoroutine(AnimateMouth());
        }
    }

    /// <summary>
    /// Stops the mascot mouth animation and sets sprite to closed mouth.
    /// </summary>
    private void StopMouthAnimation()
    {
        if (_mouthAnimationCoroutine != null)
        {
            StopCoroutine(_mouthAnimationCoroutine);
            _mouthAnimationCoroutine = null;
        }

        // Set mascot to closed mouth when not talking
        if (mascotImage != null && mouthClosedSprite != null)
        {
            mascotImage.sprite = mouthClosedSprite;
        }
    }

    /// <summary>
    /// Coroutine that cycles between mouth open and closed sprites.
    /// Uses unscaled time so animation continues when time is paused.
    /// </summary>
    private IEnumerator AnimateMouth()
    {
        bool mouthOpen = false;

        while (_isTyping || _isSpeaking)
        {
            // Toggle between mouth open and closed
            if (mouthOpen)
            {
                mascotImage.sprite = mouthClosedSprite;
            }
            else
            {
                mascotImage.sprite = mouthOpenSprite;
            }

            mouthOpen = !mouthOpen;
            // Use unscaled time so animation continues when time is paused
            yield return new WaitForSecondsRealtime(mouthAnimationSpeed);
        }

        // Ensure we end with mouth closed
        mascotImage.sprite = mouthClosedSprite;
    }

    /// <summary>
    /// Pauses the game time scale.
    /// </summary>
    private void PauseTimeScale()
    {
        _originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        Debug.Log("VRDialogueSystem: Time paused for dialogue");
    }

    /// <summary>
    /// Restores the original time scale.
    /// </summary>
    private void RestoreTimeScale()
    {
        Time.timeScale = _originalTimeScale;
        Debug.Log("VRDialogueSystem: Time restored after dialogue");
    }

    /// <summary>
    /// Hides the dialogue canvas and marks the system as inactive.
    /// </summary>
    private void EndDialog()
    {
        _isDisplaying = false;
        _isSpeaking = false;

        // Stop any ongoing speech
        if (enableTTS && ttsSpeaker != null)
        {
            ttsSpeaker.Stop();
        }

        // Stop any mouth animation
        StopMouthAnimation();

        // Restore time scale if it was paused
        if (pauseTimeScale)
        {
            RestoreTimeScale();
        }

        if (dialogCanvas != null)
        {
            dialogCanvas.SetActive(false);
        }
        if (dialogText != null)
        {
            dialogText.text = string.Empty; // Clear the text
        }
    }

    /// <summary>
    /// Manual method to end dialogue early if needed.
    /// </summary>
    public void ForceEndDialog()
    {
        // Stop any ongoing coroutines
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
        }
        if (_mouthAnimationCoroutine != null)
        {
            StopCoroutine(_mouthAnimationCoroutine);
        }

        // Stop speech
        if (enableTTS && ttsSpeaker != null)
        {
            ttsSpeaker.Stop();
        }

        // Clear the queue and end dialogue
        _dialogLines.Clear();
        EndDialog();
    }

    /// <summary>
    /// Check if dialogue is currently active.
    /// </summary>
    public bool IsDialogueActive()
    {
        return _isDisplaying;
    }

    /// <summary>
    /// Get if TTS is currently speaking
    /// </summary>
    public bool IsSpeaking()
    {
        return _isSpeaking;
    }

    void OnDestroy()
    {
        // Ensure time scale is restored if this object is destroyed while dialogue is active
        if (_isDisplaying && pauseTimeScale)
        {
            RestoreTimeScale();
        }

        // Stop any ongoing speech
        if (enableTTS && ttsSpeaker != null)
        {
            ttsSpeaker.Stop();
        }
    }
}