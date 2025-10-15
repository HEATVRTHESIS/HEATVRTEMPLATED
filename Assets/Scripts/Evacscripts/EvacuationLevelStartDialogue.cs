using UnityEngine;

/// <summary>
/// A simple script to start the dialogue system for the evacuation training level.
/// Attach this script to an empty GameObject in your scene.
/// </summary>
public class EvacuationLevelStartDialogue : MonoBehaviour
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
            string[] evacuationLines = new string[]
            {
                "Welcome to the Emergency Evacuation Training Module.",
                "",
                "A fire has broken out in the facility. Your primary objective",
                "is to evacuate safely before the timer runs out.",
                "",
                "In a real fire scenario, smoke inhalation is one of the most",
                "dangerous hazards. You cannot safely breathe in smoke-filled",
                "environments without protection.",
                "",
                "CRITICAL FIRST STEP: Locate a cloth or rag object and douse it",
                "in water. Hold this wet cloth over your face to filter the smoke",
                "and allow safer breathing during evacuation.",
                "",
                "Monitor your oxygen meter displayed on screen at all times.",
                "This gauge shows your current breathing capacity. If it depletes",
                "completely, you will lose consciousness.",
                "",
                "As you navigate toward the exit, you may encounter patients or",
                "colleagues who require assistance. Help them when possible, but",
                "remember that your own safety comes first.",
                "",
                "Stay vigilant and avoid any objects that are on fire or emitting",
                "flames. Your health watch on your right hand will display your",
                "current health status. Taking damage from fire or obstacles will",
                "reduce your health and may prevent successful evacuation.",
                "",
                "To find the nearest available exit, follow the compass indicator",
                "visible in front of you. The compass will guide you to safety.",
                "Below your oxygen bar, you will see the distance remaining and",
                "the name of the exit you are heading toward.",
                "",
                "Key objectives for this training:",
                "- Protect yourself with a wet cloth immediately",
                "- Watch your oxygen meter and health status",
                "- Assist others when safe to do so",
                "- Avoid fire and burning obstacles",
                "- Follow the compass to the nearest exit",
                "- Complete evacuation before time expires",
                "",
                "Time is critical. Stay calm, move efficiently, and prioritize",
                "your safety above all else.",
                "",
                "The evacuation timer begins now. Good luck!"
            };

            // Call the StartDialog method to begin displaying the text.
            dialogueSystem.StartDialog(evacuationLines);
        }
        else
        {
            Debug.LogError("VRDialogueSystem reference is not set in the Inspector on " + gameObject.name);
        }
    }
}