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
    // --- NEW ---
    [Tooltip("Assign the 'X' Button (Primary) action here")]
    public InputActionProperty investigationButtonAction;
    [Tooltip("Assign the 'Y' Button (Secondary) action here")]
    public InputActionProperty tasklistButtonAction;
    // --- END NEW ---

    [Header("Tutorial Triggers")]
    [Tooltip("Drag your 'WalkTutorialTrigger' GameObject here.")]
    public GameObject walkToMarkerTrigger;
    [Tooltip("Drag your 'TeleportTutorialTrigger' GameObject here.")]
    public GameObject teleportTutorialTrigger;
    [Tooltip("Drag your 'JumpTutorialTrigger' GameObject here.")]
    public GameObject jumpTutorialTrigger;
    [Tooltip("Drag the Grabbable Item (e.g., 'TutorialCube') here.")]
    public GameObject grabItemTrigger; 
    [Tooltip("Drag the 'TrashCanTrigger' zone here.")]
    public GameObject trashCanTrigger;

    [Header("Player Reset")]
    [Tooltip("The main player object (e.g., XR Origin) to teleport.")]
    public GameObject playerObject;
    [Tooltip("An empty GameObject marking the position and rotation to reset to.")]
    public Transform resetPosition;

    [Header("Dialogue Content")]
    [TextArea(3, 10)]
    public string[] dialogueLines;

    private int currentLineIndex = 0;
    private bool isDialogueActive = false;

    // This flag will stop the 'Next' button from working
    // when we're waiting for a trigger to be completed.
    private bool isWaitingForTrigger = false;
    
    // --- NEW ---
    // New flags for specific button presses
    private bool isWaitingForXPress = false;
    private bool isWaitingForYPress = false;
    // --- END NEW ---

    void Start()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        // Make sure all triggers are hidden when the game starts
        if (walkToMarkerTrigger != null)
        {
            walkToMarkerTrigger.SetActive(false);
        }
        if (teleportTutorialTrigger != null)
        {
            teleportTutorialTrigger.SetActive(false);
        }
        if (jumpTutorialTrigger != null)
        {
            jumpTutorialTrigger.SetActive(false);
        }
        if (grabItemTrigger != null)
        {
            grabItemTrigger.SetActive(false);
        }
        if (trashCanTrigger != null)
        {
            trashCanTrigger.SetActive(false);
        }

        if (nextButtonAction.action != null)
        {
            nextButtonAction.action.Enable();
        }
        
        // --- NEW ---
        if (investigationButtonAction.action != null)
        {
            investigationButtonAction.action.Enable();
        }
        if (tasklistButtonAction.action != null)
        {
            tasklistButtonAction.action.Enable();
        }
        // --- END NEW ---
    }

    // This function runs every frame
    void Update()
    {
        // Don't do anything if the dialogue isn't active
        if (!isDialogueActive)
        {
            return;
        }

        // --- NEW ---
        // Check for Investigation (X) press
        if (isWaitingForXPress)
        {
            if (investigationButtonAction.action != null && investigationButtonAction.action.WasPressedThisFrame())
            {
                Debug.Log("Investigation (X) button pressed! Advancing dialogue.");
                AdvanceDialogue();
            }
            return; // Block other input
        }

        // Check for Tasklist (Y) press
        if (isWaitingForYPress)
        {
            if (tasklistButtonAction.action != null && tasklistButtonAction.action.WasPressedThisFrame())
            {
                Debug.Log("Tasklist (Y) button pressed! Advancing dialogue.");
                AdvanceDialogue();
            }
            return; // Block other input
        }
        // --- END NEW ---

        // Check if the "next" button was pressed AND we are not waiting for a trigger
        if (nextButtonAction.action != null && 
            nextButtonAction.action.WasPressedThisFrame() && 
            !isWaitingForTrigger)
        {
            // If it was, advance to the next line
            AdvanceDialogue();
        }
    }

    // Call this from your trigger zone to start the entire tutorial
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

    // This advances the dialogue one line at a time
    // It is called by Update() (when pressing 'Next') OR
    // by your triggers (OnPlayerEnter)
    public void AdvanceDialogue()
    {
        // --- NEW ---
        // Reset all wait flags to ensure a clean state
        isWaitingForTrigger = false;
        isWaitingForXPress = false;
        isWaitingForYPress = false;
        // --- END NEW ---

        currentLineIndex++;

        if (currentLineIndex < dialogueLines.Length)
        {
            // Still have lines, show the next one
            ShowDialogueLine(currentLineIndex);
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

        // Reset player position after dialogue ends
        if (playerObject != null && resetPosition != null)
        {
            Debug.Log("Resetting player position.");
            playerObject.transform.position = resetPosition.position;
            playerObject.transform.rotation = resetPosition.rotation;
        }
        else
        {
            Debug.LogWarning("Player Object or Reset Position not assigned. Cannot reset player position.");
        }
    }

    // This private function displays the line AND checks for special actions
    private void ShowDialogueLine(int index)
    {
        dialogueTextUI.text = dialogueLines[index];
        
        // By default, we are not waiting for a trigger.
        // NOTE: We reset flags in AdvanceDialogue() *before* this runs
        // to ensure a clean state.
        isWaitingForTrigger = false; 

        // Check for walking trigger
        if (index == 3) // Element 4
        {
            if (walkToMarkerTrigger != null)
            {
                Debug.Log("Activating Walk-To-Marker trigger. Disabling 'Next' button.");
                walkToMarkerTrigger.SetActive(true);
                isWaitingForTrigger = true; 
            }
        }
        // Check for teleport trigger
        else if (index == 5) // Element 6
        {
            if (teleportTutorialTrigger != null)
            {
                Debug.Log("Activating Teleport-To-Marker trigger. Disabling 'Next' button.");
                teleportTutorialTrigger.SetActive(true);
                isWaitingForTrigger = true; 
            }
        }
        // Check for jump trigger
        else if (index == 8) // Element 9
        {
            if (jumpTutorialTrigger != null)
            {
                Debug.Log("Activating Jump-To-Marker trigger. Disabling 'Next' button.");
                jumpTutorialTrigger.SetActive(true);
                isWaitingForTrigger = true; 
            }
        }
        // --- NEW ---
        // Check for "Investigation" dialogue
        /*
        else if (index == 9) // Element 10 (NEW)
        {
            Debug.Log("Waiting for Investigation (X) press. Disabling 'Next' button.");
            isWaitingForXPress = true;
            isWaitingForTrigger = true; // This disables the 'Next' button
        }
        */
        // Check for "Tasklist" dialogue
        else if (index == 9) // Element 10 (NEW) Change value once the Investigation is added
        {
            Debug.Log("Waiting for Tasklist (Y) press. Disabling 'Next' button.");
            isWaitingForYPress = true;
            isWaitingForTrigger = true; // This disables the 'Next' button
        }
        // --- END NEW ---
        // Check for "Grab Item" dialogue
        else if (index == 12) // Element 13 (Original 11)
        {
            if (grabItemTrigger != null)
            {
                Debug.Log("Activating Grab Item trigger. Disabling 'Next' button.");
                grabItemTrigger.SetActive(true);
                isWaitingForTrigger = true; 
            }
        }
        // Check for "Trash Item" dialogue
        else if (index == 14) // Element 15 (Original 12)
        {
            if (trashCanTrigger != null)
            {
                Debug.Log("Activating Trash Can trigger. Disabling 'Next' button.");
                trashCanTrigger.SetActive(true);
                isWaitingForTrigger = true; 
            }
        }
    }
}

