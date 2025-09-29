using System.Collections;
using System.Collections.Generic;
using System.IO; // Add this for Directory operations
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Crosstales.RTVoice; // Add RT-Voice namespace
using Crosstales.RTVoice.Model;

/// <summary>
/// A text display system for VR that shows dialogue lines and progresses with user input.
/// Includes animated mascot that "talks" during text typing and RT-Voice text-to-speech.
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
    [Tooltip("Speech pitch (0.0 to 2.0, 1.0 is normal pitch)")]
    [Range(0.0f, 2.0f)]
    public float speechPitch = 1.0f;
    [Tooltip("Speech volume (0.0 to 1.0)")]
    [Range(0.0f, 1.0f)]
    public float speechVolume = 1.0f;
    [Tooltip("Wait for speech to complete before allowing next line")]
    public bool waitForSpeech = true;
    [Tooltip("Show text immediately when speech starts (disable typewriter for speech)")]
    public bool showTextImmediatelyWithSpeech = false;
    [Tooltip("Use native speech (no file generation) to avoid file system issues")]
    public bool useNativeSpeech = true;

    // --- Private Fields ---
    private Queue<string> _dialogLines = new Queue<string>();
    private bool _isDisplaying = false;
    private bool _isTyping = false;
    private bool _isSpeaking = false;
    private string _currentLine;
    private Coroutine _typingCoroutine;
    private Coroutine _mouthAnimationCoroutine;
    private float _originalTimeScale = 1f;
    private Voice _selectedVoice;
    private string _currentSpeechId;

    void Awake()
    {
        // Store the original time scale
        _originalTimeScale = Time.timeScale;

        // Configure RT-Voice audio path to avoid file conflicts
        if (enableTTS)
        {
            // Set RT-Voice to use a specific directory for audio files
            string audioPath = Path.Combine(Application.persistentDataPath, "RTVoiceAudio");
            if (!Directory.Exists(audioPath))
            {
                Directory.CreateDirectory(audioPath);
            }
            Crosstales.RTVoice.Util.Config.AUDIOFILE_PATH = audioPath;
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

        // Setup AudioSource for RT-Voice if not assigned
        if (speechAudioSource == null && enableTTS)
        {
            speechAudioSource = gameObject.GetComponent<AudioSource>();
            if (speechAudioSource == null)
            {
                speechAudioSource = gameObject.AddComponent<AudioSource>();
            }
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
        if (nextLineAction.action != null)
        {
            nextLineAction.action.Disable();
        }

        // Unsubscribe from RT-Voice events
        if (enableTTS && Speaker.Instance != null)
        {
            Speaker.Instance.OnSpeakStart -= OnSpeechStart;
            Speaker.Instance.OnSpeakComplete -= OnSpeechComplete;
            Speaker.Instance.OnVoicesReady -= OnVoicesReady;
        }
        
        // Restore time scale if dialogue is disabled while active
        if (_isDisplaying && pauseTimeScale)
        {
            RestoreTimeScale();
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
            _selectedVoice = Speaker.Instance.VoiceForName(voiceName);
            if (_selectedVoice == null)
            {
                Debug.LogWarning($"Voice '{voiceName}' not found. Using default voice.");
            }
        }
    }

    /// <summary>
    /// RT-Voice event: Called when speech starts
    /// </summary>
    private void OnSpeechStart(Wrapper wrapper)
    {
        if (wrapper.Uid == _currentSpeechId)
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
    /// RT-Voice event: Called when speech completes
    /// </summary>
    private void OnSpeechComplete(Wrapper wrapper)
    {
        if (wrapper.Uid == _currentSpeechId)
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
        if (enableTTS)
        {
            Speaker.Instance.Silence();
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
    /// Handles the button press event from the input system.
    /// </summary>
    private void OnNextLineButtonPressed(InputAction.CallbackContext context)
    {
        if (!_isDisplaying)
        {
            // Do nothing if the dialogue is not active
            return;
        }

        // If speech is playing and we're waiting for it, skip speech
        if (_isSpeaking && enableTTS)
        {
            Speaker.Instance.Silence();
            return;
        }

        // If a line is currently being typed, complete it immediately.
        if (_isTyping)
        {
            CompleteLine();
        }
        else if (!waitForSpeech || !_isSpeaking)
        {
            // If the line is complete and speech is done (or we're not waiting), move to next line
            DisplayNextLine();
        }
    }

    /// <summary>
    /// Displays the next line from the queue or ends the dialogue if no more lines exist.
    /// </summary>
    private void DisplayNextLine()
    {
        // Ensure the canvas is active before trying to start coroutines
        if (dialogCanvas != null && !dialogCanvas.activeInHierarchy)
        {
            Debug.LogWarning("VRDialogueSystem: Trying to display dialogue when canvas is inactive!");
            return;
        }

        // Stop any ongoing typing coroutine just in case
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
        }

        // Stop any ongoing speech
        if (enableTTS && _isSpeaking)
        {
            Speaker.Instance.Silence();
        }

        // Check if there are lines left to display
        if (_dialogLines.Count > 0)
        {
            // Dequeue and store the line before starting the coroutine.
            _currentLine = _dialogLines.Dequeue();
            
            // Skip empty lines
            if (string.IsNullOrEmpty(_currentLine.Trim()))
            {
                DisplayNextLine(); // Recursively call to get the next non-empty line
                return;
            }
            
            if (enableTTS && showTextImmediatelyWithSpeech)
            {
                // Show text immediately and start speech
                dialogText.text = _currentLine;
                StartSpeech(_currentLine);
            }
            else
            {
                // Start typing animation
                _typingCoroutine = StartCoroutine(TypeLine(_currentLine));
            }
        }
        else
        {
            // If the queue is empty, all lines have been displayed, so end the dialogue
            EndDialog();
        }
    }

    /// <summary>
    /// Starts speech using RT-Voice
    /// </summary>
    private void StartSpeech(string text)
    {
        if (!enableTTS || Speaker.Instance == null || string.IsNullOrEmpty(text.Trim()))
            return;

        // Generate unique ID for this speech
        _currentSpeechId = System.Guid.NewGuid().ToString();

        if (useNativeSpeech)
        {
            // Use native speech (no file generation)
            Speaker.Instance.SpeakNative(
                text,                    // text to speak
                _selectedVoice,          // voice (can be null for default)
                speechRate,              // rate
                speechPitch,             // pitch  
                speechVolume             // volume
            );
        }
        else
        {
            // Use the correct RT-Voice Speak method with file generation
            Speaker.Instance.Speak(
                text,                    // text to speak
                speechAudioSource,       // audio source
                _selectedVoice,          // voice (can be null for default)
                true,                    // speak immediately
                speechRate,              // rate
                speechPitch,             // pitch  
                speechVolume,            // volume
                _currentSpeechId         // unique ID
            );
        }
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

        // Start speech if enabled and not already playing
        if (enableTTS && !_isSpeaking)
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
        if (enableTTS)
        {
            Speaker.Instance.Silence();
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
        if (enableTTS)
        {
            Speaker.Instance.Silence();
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
    /// Get available voices for UI selection
    /// </summary>
    public List<Voice> GetAvailableVoices()
    {
        if (enableTTS && Speaker.Instance != null)
        {
            return Speaker.Instance.Voices;
        }
        return new List<Voice>();
    }

    /// <summary>
    /// Set voice by name
    /// </summary>
    public void SetVoice(string newVoiceName)
    {
        voiceName = newVoiceName;
        _selectedVoice = Speaker.Instance?.VoiceForName(voiceName);
    }

    void OnDestroy()
    {
        // Ensure time scale is restored if this object is destroyed while dialogue is active
        if (_isDisplaying && pauseTimeScale)
        {
            RestoreTimeScale();
        }

        // Stop any ongoing speech
        if (enableTTS && Speaker.Instance != null)
        {
            Speaker.Instance.Silence();
        }
    }
}