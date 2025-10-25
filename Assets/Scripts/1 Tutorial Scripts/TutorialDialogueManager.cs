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

    [Header("Tutorial Triggers")]
    [Tooltip("Drag your 'WalkTutorialTrigger' GameObject here.")]
    public GameObject walkToMarkerTrigger;
    
    // --- NEW ---
    [Tooltip("Drag your 'TeleportTutorialTrigger' GameObject here.")]
    public GameObject teleportTutorialTrigger;
    // --- END NEW ---

    [Tooltip("Drag your 'Jump Tutorial Trigger' GameObject here.")]
    public GameObject jumpTutorialTrigger;

    [Header("Dialogue Content")]
    [TextArea(3, 10)]
    public string[] dialogueLines;

    private int currentLineIndex = 0;
    private bool isDialogueActive = false;

    void Start()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        // Make sure the triggers are hidden when the game starts
        if (walkToMarkerTrigger != null)
        {
            walkToMarkerTrigger.SetActive(false);
        }
        
        // --- NEW ---
        if (teleportTutorialTrigger != null)
        {
            teleportTutorialTrigger.SetActive(false);
        }
        // --- END NEW ---

         if (jumpTutorialTrigger != null)
        {
            jumpTutorialTrigger.SetActive(false);
        }
        // --- END NEW ---

        if (nextButtonAction.action != null)
        {
            nextButtonAction.action.Enable();
        }
    }

    void Update()
    {
        if (!isDialogueActive)
        {
            return;
        }

        if (nextButtonAction.action != null && nextButtonAction.action.WasPressedThisFrame())
        {
            AdvanceDialogue();
        }
    }

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
        ShowDialogueLine(currentLineIndex);
    }

    // --- MODIFIED ---
    // (Make sure this is public!)
    public void AdvanceDialogue()
    // --- END MODIFIED ---
    {
        currentLineIndex++; 

        if (currentLineIndex < dialogueLines.Length)
        {
            ShowDialogueLine(currentLineIndex);
        }
        else
        {
            EndDialogue();
        }
    }

    private void ShowDialogueLine(int index)
    {
        dialogueTextUI.text = dialogueLines[index];

        // Check for walking trigger
        if (index == 2) // Element 3
        {
            if (walkToMarkerTrigger != null)
            {
                Debug.Log("Activating Walk-To-Marker trigger.");
                walkToMarkerTrigger.SetActive(true);
            }
        }
        
        // --- NEW ---
        // Check for teleport trigger
        else if (index == 4) // Element 5
        {
            if (teleportTutorialTrigger != null)
            {
                Debug.Log("Activating Teleport-To-Marker trigger.");
                teleportTutorialTrigger.SetActive(true);
            }
        }
        // --- END NEW ---

         // --- NEW ---
        // Check for Jump Trigger
        else if (index == 7) // Element 8
        {
            if (teleportTutorialTrigger != null)
            {
                Debug.Log("Activating Jump-To-Marker trigger.");
                teleportTutorialTrigger.SetActive(true);
            }
        }
        // --- END NEW ---

    }

    public void EndDialogue()
    {
        isDialogueActive = false;
        dialoguePanel.SetActive(false);
        Debug.Log("Dialogue finished.");
    }
}