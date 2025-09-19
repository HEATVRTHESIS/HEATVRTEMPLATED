using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // Make sure you have the Text Mesh Pro package imported
using UnityEngine.InputSystem;
using UnityEngine.UI; // Required for the Image component

/// <summary>
/// A text display system for VR that shows dialogue lines and progresses with user input.
/// Includes animated mascot that "talks" during text typing.
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

    // --- Private Fields ---
    private Queue<string> _dialogLines = new Queue<string>();
    private bool _isDisplaying = false;
    private bool _isTyping = false;
    private string _currentLine;
    private Coroutine _typingCoroutine;
    private Coroutine _mouthAnimationCoroutine;
    private float _originalTimeScale = 1f;

    void Awake()
    {
        // Store the original time scale
        _originalTimeScale = Time.timeScale;

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
    }

    void OnDisable()
    {
        if (nextLineAction.action != null)
        {
            nextLineAction.action.Disable();
        }
        
        // Restore time scale if dialogue is disabled while active
        if (_isDisplaying && pauseTimeScale)
        {
            RestoreTimeScale();
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
        _isTyping = false; // Reset typing state

        // Pause time if enabled
        if (pauseTimeScale)
        {
            PauseTimeScale();
        }

        // Add all new lines to the queue
        foreach (string line in lines)
        {
            _dialogLines.Enqueue(line);
        }

        // Show the canvas and display the first line
        if (dialogCanvas != null)
        {
            dialogCanvas.SetActive(true);
            DisplayNextLine();
        }
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

        // If a line is currently being typed, complete it immediately.
        if (_isTyping)
        {
            CompleteLine();
        }
        else
        {
            // If the line is already complete, move to the next one.
            DisplayNextLine();
        }
    }

    /// <summary>
    /// Displays the next line from the queue or ends the dialogue if no more lines exist.
    /// </summary>
    private void DisplayNextLine()
    {
        // Stop any ongoing typing coroutine just in case
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
        }

        // Check if there are lines left to display
        if (_dialogLines.Count > 0)
        {
            // Dequeue and store the line before starting the coroutine.
            _currentLine = _dialogLines.Dequeue();
            _typingCoroutine = StartCoroutine(TypeLine(_currentLine));
        }
        else
        {
            // If the queue is empty, all lines have been displayed, so end the dialogue
            EndDialog();
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

        foreach (char character in line.ToCharArray())
        {
            dialogText.text += character;
            // Use unscaled time so typing continues even when time is paused
            yield return new WaitForSecondsRealtime(typingSpeed);
        }
        
        _isTyping = false; // Typing is complete

        // Stop the mouth animation and set to closed mouth
        StopMouthAnimation();
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

        // Stop the mouth animation and set to closed mouth
        StopMouthAnimation();
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

        while (_isTyping)
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

    void OnDestroy()
    {
        // Ensure time scale is restored if this object is destroyed while dialogue is active
        if (_isDisplaying && pauseTimeScale)
        {
            RestoreTimeScale();
        }
    }
}