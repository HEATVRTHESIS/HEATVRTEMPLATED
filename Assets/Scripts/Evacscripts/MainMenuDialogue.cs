using UnityEngine;

/// <summary>
/// A simple script to start the dialogue system at the beginning of a level.
/// Attach this script to an empty GameObject in your scene.
/// </summary>
public class MainMenuDialogue: MonoBehaviour
{
    [Tooltip("Drag the GameObject with the VRDialogueSystem component here.")]
    [SerializeField]
    private VRDialogueSystem dialogueSystem;

    void Start()
    {
        // Check if the dialogue system reference is set to avoid errors.
        if (dialogueSystem != null)
        {
           // Define the lines to be displayed.
string[] welcomeLines = new string[]
{
    "Welcome to the HEAT VR Game.",
    "",
    "Today you will be learning protocols when there is a fire.",
    "All of this simulation facility will help you provide crucial knowledge on",
    "regarding what to do when there's a fire.",
    "",
    "First off, check your checklist and look behind you.",
    "The items and the checklist help you formalize on what you're about to do",
    "on the training area.",
    "",
    "Good luck!"
};

            // Call the StartDialog method to begin displaying the text.
            dialogueSystem.StartDialog(welcomeLines);
        }
        else
        {
            Debug.LogError("VRDialogueSystem reference is not set in the Inspector on " + gameObject.name);
        }
    }
}