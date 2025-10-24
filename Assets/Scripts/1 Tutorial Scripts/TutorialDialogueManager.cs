using UnityEngine;
using TMPro; // Make sure to import TextMeshPro

// This class will hold the data for each step.
// We make it [System.Serializable] so we can edit it in the Inspector.
[System.Serializable]
public class DialogueStep
{
    [TextArea(3, 10)]
    public string dialogueText;
    
    // The GameObject that contains the "listener" script for this step.
    // e.g., the "JoystickLookListener" or the "GrabbableCube".
    public GameObject actionListener; 
}


public class TutorialDialogueManager : MonoBehaviour
{
    // Assign your UI Text and Panel in the Inspector
    public TextMeshProUGUI dialogueTextUI;
    public GameObject dialoguePanel;

    // This is your list of all tutorial steps
    public DialogueStep[] allSteps;

    private int currentStepIndex = 0;

    // Start the dialogue (e.g., call this from a button or trigger)
    public void StartDialogue()
    {
        if (allSteps.Length == 0) return;

        currentStepIndex = 0;
        dialoguePanel.SetActive(true);
        ShowStep(currentStepIndex);
    }

    // This is the public function our listeners will call
    public void AdvanceDialogue()
    {
        // Deactivate the listener for the step we just finished
        if (allSteps[currentStepIndex].actionListener != null)
        {
            allSteps[currentStepIndex].actionListener.SetActive(false);
        }

        // Move to the next step
        currentStepIndex++;

        // Check if we are at the end of the dialogue
        if (currentStepIndex < allSteps.Length)
        {
            ShowStep(currentStepIndex);
        }
        else
        {
            EndDialogue();
        }
    }

    // Private function to show the current step's info
    private void ShowStep(int index)
    {
        // Update the text
        dialogueTextUI.text = allSteps[index].dialogueText;

        // Activate the listener for this new step
        if (allSteps[index].actionListener != null)
        {
            allSteps[index].actionListener.SetActive(true);
        }
        // If there is no listener, it means we wait for a button press (or you can auto-advance)
        // For this example, we assume every step has a listener.
    }

    // Call this to hide the dialogue box
    public void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        Debug.Log("Dialogue finished.");
    }
}