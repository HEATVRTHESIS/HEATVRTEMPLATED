using UnityEngine;

/// <summary>
/// A simple script to start the dialogue system at the beginning of a level.
/// Attach this script to an empty GameObject in your scene.
/// </summary>
public class EmergencyRoomStartDialogue : MonoBehaviour
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
    "Welcome to the Emergency Room Training Module.",
    "",
    "You are now in an Emergency Department environment.",
    "This facility provides critical care for patients requiring",
    "immediate medical attention, including trauma cases, acute illnesses,",
    "and life-threatening conditions that demand rapid assessment",
    "and intervention.",
    "",
    "Common equipment here includes defibrillators, crash carts,",
    "oxygen delivery systems, IV infusion pumps, patient monitors,",
    "suction devices, and emergency medication storage units",
    "that must be readily accessible at all times.",
    "",
    "Please note: This training module includes access to surgical",
    "and procedure rooms. Under normal circumstances, entry to these",
    "sterile environments is strictly controlled due to contamination",
    "risks and sterilization protocols. However, they are included",
    "in this training to help you identify potential fire hazards",
    "and safety risks that may be present in these critical areas.",
    "",
    "Today's training focuses on fire risk management and emergency",
    "safety protocols essential for the Emergency Department. You will",
    "learn proper storage of flammable medical supplies, oxygen",
    "cylinder safety procedures, electrical equipment inspection,",
    "and how to maintain fire safety systems in high-pressure",
    "medical environments.",
    "",
    "Interactive objects are highlighted with glowing outlines to",
    "guide your training. Press Y on your left VR controller to",
    "open your task checklist and monitor your progress.",
    "You can focus on specific tasks by pressing the track button",
    "on the side of each task entry.",
    "",
    "Remember: fire safety in emergency departments is critical.",
    "Many patients cannot evacuate quickly, and oxygen-enriched",
    "environments create additional fire risks. Your vigilance",
    "protects vulnerable patients and emergency medical staff.",
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