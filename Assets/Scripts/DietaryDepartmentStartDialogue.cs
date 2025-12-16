using UnityEngine;

/// <summary>
/// A simple script to start the dialogue system at the beginning of a level.
/// Attach this script to an empty GameObject in your scene.
/// </summary>
public class DietaryDepartmentStartDialogue : MonoBehaviour
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
                "Welcome to the Dietary Department Training Module.",
                "",
                "You are now in a hospital food service environment.",
                "This facility prepares meals for patients with diverse dietary",
                "needs, including therapeutic diets, texture-modified foods,",
                "and nutrition for critically ill patients. Safe food preparation",
                "is essential for patient recovery and safety.",
                "",
                "Common equipment here includes commercial ranges, ovens,",
                "deep fryers, grills, steamers, food warmers, refrigeration units,",
                "dishwashing systems, and ventilation hoods that must be",
                "properly maintained to prevent fire hazards.",
                "",
                "CRITICAL SAFETY NOTE: Dietary departments face unique fire risks,",
                "particularly Class K fires involving cooking oils, greases, and fats.",
                "These fires burn at extremely high temperatures and CANNOT be",
                "extinguished with water or standard fire extinguishers. Using the",
                "wrong extinguisher can cause dangerous grease splatter and spread",
                "the fire rapidly.",
                "",
                "Today's training focuses on fire prevention and Class K fire safety",
                "protocols specific to food service operations. You will learn proper",
                "storage of cooking oils, grease trap maintenance, hood and exhaust",
                "system inspection, safe operation of cooking equipment, and the",
                "critical differences between Class K and other fire extinguishers.",
                "",
                "Interactive objects are highlighted with glowing outlines to",
                "guide your training. Press Y on your left VR controller to",
                "open your task checklist and monitor your progress.",
                "You can focus on specific tasks by pressing the track button",
                "on the side of each task entry.",
                "",
                "Remember: Class K fire extinguishers contain wet chemical agents",
                "that create a foam blanket to suppress cooking fires. Never use",
                "water on grease fires. Unattended cooking equipment is the leading",
                "cause of kitchen fires. Your attention to fire safety protocols",
                "protects patients, staff, and the hospital's ability to provide",
                "essential nutrition services.",
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