using UnityEngine;
using TMPro; // For the UI text
using UnityEngine.InputSystem; // For the button action

public class TutorialDialogueManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI dialogueTextUI;
    public GameObject dialoguePanel;

    [Header("Input Action")]
    [Tooltip("Assign the 'Secondary Button' (B/Y) action here")]
    public InputActionProperty nextButtonAction; 

    [Header("Dialogue Content")]
    [TextArea(3, 10)]
    public string[] dialogueLines; // A simple array of dialogue strings

    // Private variables
    private int currentLineIndex = 0;
    private bool isDialogueActive = false;

    void Start()
    {
        // Make sure the dialogue UI is hidden at the start
        if(dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        // Enable the button action so we can listen to it
        if (nextButtonAction.action != null)
        {
            nextButtonAction.action.Enable();
        }
    }

    void Update()
    {
        // Don't do anything if the dialogue isn't active
        if (!isDialogueActive)
        {
            return;
        }

        // Check if the "next" button was pressed this frame
        if (nextButtonAction.action != null && nextButtonAction.action.WasPressedThisFrame())
        {
            // If it was, advance to the next line
            AdvanceDialogue();
        }
    }

    // Call this from your trigger zone to start
    public void StartDialogue()
    {
        if (dialogueLines.Length == 0)
        {
            Debug.LogWarning("No dialogue lines found!");
            return;
        }

        isDialogueActive = true;
        currentLineIndex = 0;
        dialoguePanel.SetActive(true);
        dialogueTextUI.text = dialogueLines[currentLineIndex];
    }

    // This advances the dialogue one line at a time
    private void AdvanceDialogue()
    {
        currentLineIndex++; // Move to the next index

        if (currentLineIndex < dialogueLines.Length)
        {
            // Still have lines, show the next one
            dialogueTextUI.text = dialogueLines[currentLineIndex];
        }
        else
        {
            // No more lines, end the dialogue
            EndDialogue();
        }
    }

    // This hides the panel and stops listening for input
    public void EndDialogue()
    {
        isDialogueActive = false;
        dialoguePanel.SetActive(false);
        Debug.Log("Dialogue finished.");
    }
}