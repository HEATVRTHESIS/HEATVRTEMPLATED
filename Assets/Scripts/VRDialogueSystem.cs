using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // Make sure you have the Text Mesh Pro package imported
using UnityEngine.InputSystem;
using UnityEngine.UI; // Required for the ContentSizeFitter component
using UnityEngine.Events; // Needed for UnityEvent

/// <summary>
/// A text display system for VR that shows dialogue lines and progresses with user input.
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
    [Tooltip("The ContentSizeFitter on the parent of the TextMeshPro element. This will scale the background.")]
    public ContentSizeFitter canvasSizeFitter;
    [Tooltip("The local position of the dialogue canvas relative to the player's camera.")]
    public Vector3 dialogueOffset = new Vector3(0, -0.5f, 1.5f);
    [Tooltip("The speed at which characters are typed out. A smaller value is faster.")]
    public float typingSpeed = 0.05f;

    // A public event that other scripts can listen to
    public UnityEvent OnDialogueEnd;

    // --- Private Fields ---
    private Queue<string> _dialogLines = new Queue<string>();
    private bool _isDisplaying = false;
    private bool _isTyping = false;
    private string _currentLine;
    private Transform _playerTransform;
    private Coroutine _typingCoroutine;

    void Awake()
    {
        // Get a reference to the player's camera transform.
        _playerTransform = Camera.main.transform;

        // Ensure the initial state is hidden
        if (dialogCanvas != null)
        {
            dialogCanvas.SetActive(false);
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

        // Add all new lines to the queue
        foreach (string line in lines)
        {
            _dialogLines.Enqueue(line);
        }

        // Show the canvas and display the first line
        if (dialogCanvas != null)
        {
            // Parent the canvas to the player's camera and set its local position.
            dialogCanvas.transform.SetParent(_playerTransform, false);
            dialogCanvas.transform.localPosition = dialogueOffset;
            dialogCanvas.transform.localRotation = Quaternion.identity;
            
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
    /// </summary>
    private IEnumerator TypeLine(string line)
    {
        _isTyping = true;
        dialogText.text = ""; // Clear the text field before typing
        foreach (char character in line.ToCharArray())
        {
            dialogText.text += character;
            yield return new WaitForSeconds(typingSpeed);
        }
        _isTyping = false; // Typing is complete
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
    }

    /// <summary>
    /// Hides the dialogue canvas and marks the system as inactive.
    /// </summary>
    private void EndDialog()
    {
        _isDisplaying = false;
        if (dialogCanvas != null)
        {
            // Unparent the canvas to prevent it from following the player
            dialogCanvas.transform.SetParent(null);
            dialogCanvas.SetActive(false);
        }
        if (dialogText != null)
        {
            dialogText.text = string.Empty; // Clear the text
        }
        
        // Call the event to notify other scripts that the dialogue is finished
        OnDialogueEnd?.Invoke();
    }
}