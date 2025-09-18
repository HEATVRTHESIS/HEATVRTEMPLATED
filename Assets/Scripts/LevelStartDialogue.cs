using UnityEngine;

/// <summary>
/// A simple script to start the dialogue system at the beginning of a level.
/// Attach this script to an empty GameObject in your scene.
/// </summary>
public class LevelStartDialogue : MonoBehaviour
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
    "Welcome to the MedTech Laboratory Training Module.",
    "",
    "You are now in a medical technology laboratory environment.",
    "This facility houses diagnostic equipment, chemical reagents,",
    "biological samples, and various testing instruments used for",
    "patient analysis and research.",
    "",
    "Common equipment here includes centrifuges, microscopes,",
    "autoclave sterilizers, chemical fume hoods, specimen refrigerators,",
    "and analytical instruments that require precise environmental",
    "conditions and regular maintenance.",
    "",
    "Today's training focuses on fire risk management and safety",
    "protocols essential for laboratory operations. You will learn",
    "proper storage procedures for flammable chemicals and materials,",
    "correct disposal methods for hazardous waste, and how to",
    "inspect and maintain fire safety equipment to ensure",
    "compliance with safety standards.",
    "",
    "Interactive objects are highlighted with glowing outlines to",
    "guide your training. Press Y on your left VR controller to",
    "open your task checklist and monitor your progress.",
    "You can focus on specific tasks by pressing the track button",
    "on the side of each task entry.",
    "",
    "Remember: proper fire safety protocols protect both personnel",
    "and valuable laboratory equipment. Your attention to detail",
    "could prevent accidents and save lives.",
    "",
    "Begin when ready, and take your time to complete each task thoroughly.",
    "Right now it is ok to make mistakes.",
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
