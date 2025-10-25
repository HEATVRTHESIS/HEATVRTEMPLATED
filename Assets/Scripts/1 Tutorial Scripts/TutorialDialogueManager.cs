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
    [Tooltip("Drag your 'TeleportTutorialTrigger' GameObject here.")]
    public GameObject teleportTutorialTrigger;
    [Tooltip("Drag your 'JumpTutorialTrigger' GameObject here.")]
    public GameObject jumpTutorialTrigger;
    [Tooltip("Drag the Grabbable Item (e.g., 'TutorialCube') here.")]
    public GameObject grabItemTrigger; 
    [Tooltip("Drag the 'TrashCanTrigger' zone here.")]
    public GameObject trashCanTrigger;

    // --- NEW ---
    [Header("Player Reset")]
    [Tooltip("The main player object (e.g., XR Origin) to teleport.")]
    public GameObject playerObject;
    [Tooltip("An empty GameObject marking the position and rotation to reset to.")]
    public Transform resetPosition;
    // --- END NEW ---

    [Header("Dialogue Content")]
    [TextArea(3, 10)]
    public string[] dialogueLines;

    private int currentLineIndex = 0;
    private bool isDialogueActive = false;

    // This flag will stop the 'Next' button from working
    // when we're waiting for a trigger to be completed.
    private bool isWaitingForTrigger = false;

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
    }

    // This function runs every frame
    void Update()
    {
        // Don't do anything if the dialogue isn't active
        if (!isDialogueActive)
        {
            return;
        }

        // Check if the "next" button was pressed AND we are not waiting for a trigger
        if (nextButtonAction.action != null && 
            nextButtonAction.action.WasPressedThisFrame() && 
            !isWaitingForTrigger) // <--- THIS IS THE NEW CONDITION
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

        // --- NEW ---
        // Reset player position after dialogue ends
        if (playerObject != null && resetPosition != null)
        {
            Debug.Log("Resetting player position.");
            
            // For this to work correctly, 'playerObject' should be your
            // 'XR Origin' or main player controller object.
            
            // This is a simple teleport. If you use a CharacterController,
            // you may need to disable it before changing the transform.
            playerObject.transform.position = resetPosition.position;
            playerObject.transform.rotation = resetPosition.rotation;
        }
        else
        {
            Debug.LogWarning("Player Object or Reset Position not assigned. Cannot reset player position.");
        }
        // --- END NEW ---
    }

    // This private function displays the line AND checks for special actions
    private void ShowDialogueLine(int index)
    {
        dialogueTextUI.text = dialogueLines[index];
        
        // By default, we are not waiting for a trigger.
        isWaitingForTrigger = false;

        // Check for walking trigger
        if (index == 3) // Element 3
        {
            if (walkToMarkerTrigger != null)
            {
                Debug.Log("Activating Walk-To-Marker trigger. Disabling 'Next' button.");
                walkToMarkerTrigger.SetActive(true);
                isWaitingForTrigger = true; 
            }
        }
        // Check for teleport trigger
        else if (index == 5) // Element 5
        {
            if (teleportTutorialTrigger != null)
            {
                Debug.Log("Activating Teleport-To-Marker trigger. Disabling 'Next' button.");
                teleportTutorialTrigger.SetActive(true);
                isWaitingForTrigger = true; 
            }
        }
        // Check for jump trigger
        else if (index == 8) // Element 8
        {
            if (jumpTutorialTrigger != null)
            {
                Debug.Log("Activating Jump-To-Marker trigger. Disabling 'Next' button.");
                jumpTutorialTrigger.SetActive(true);
                isWaitingForTrigger = true; 
            }
        }
        // Check for "Grab Item" dialogue
        else if (index == 11) // Element 11 (Assuming this is your "Grab Me" dialogue)
        {
            if (grabItemTrigger != null)
            {
                Debug.Log("Activating Grab Item trigger. Disabling 'Next' button.");
                grabItemTrigger.SetActive(true);
                isWaitingForTrigger = true; 
            }
        }
        // Check for "Trash Item" dialogue
        else if (index == 12) // Element 10 (Assuming this is your "Now throw it away" dialogue)
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

