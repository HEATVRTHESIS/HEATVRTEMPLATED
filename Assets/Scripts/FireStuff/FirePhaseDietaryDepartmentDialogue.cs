using UnityEngine;
using System.Collections;

/// <summary>
/// Fire safety training dialogue for the Dietary Department.
/// Teaches RACE and PASS acronyms with emphasis on Class K fire response.
/// </summary>
public class FirePhaseDietaryDepartmentDialogue : MonoBehaviour
{
    [Tooltip("Drag the GameObject with the VRDialogueSystem component here.")]
    [SerializeField]
    private VRDialogueSystem dialogueSystem;

    void Start()
    {
        // Start the dialogue after a small delay to ensure everything is initialized
        StartCoroutine(StartDialogueAfterDelay());
    }

    private IEnumerator StartDialogueAfterDelay()
    {
        // Wait a frame to ensure all GameObjects are properly initialized
        yield return null;
        
        // Check if the dialogue system reference is set to avoid errors.
        if (dialogueSystem != null)
        {
            // Define the fire safety training lines
            string[] fireTrainingLines = new string[]
            {
                "Welcome to the Fire Safety Emergency Response Training Module.",
                "",
                "A fire emergency has been simulated in the Dietary Department.",
                "In this critical training scenario, you will learn and practice",
                "the essential fire safety protocols that could save lives",
                "and protect patients and food service staff.",
                "",
                "Every second counts in a fire emergency. Your quick thinking",
                "and proper execution of safety procedures are crucial.",
                "",
                "First, let's learn the RACE acronym - your immediate",
                "response protocol when discovering a fire:",
                "",
                "R - RESCUE: Remove anyone in immediate danger from the fire area.",
                "Move people away from smoke and flames to a safe location.",
                "",
                "A - ALARM: Activate the fire alarm system immediately.",
                "Alert others in the building and notify emergency services.",
                "",
                "C - CONFINE: Close doors and windows to contain the fire",
                "and prevent it from spreading to other areas.",
                "",
                "E - EVACUATE or EXTINGUISH: If the fire is small and you",
                "are trained, attempt to extinguish it. Otherwise, evacuate",
                "the area immediately and let professionals handle it.",
                "",
                "Next, the PASS technique for using fire extinguishers safely:",
                "",
                "P - PULL: Pull the safety pin from the fire extinguisher.",
                "This breaks the tamper seal and allows operation.",
                "",
                "A - AIM: Aim the nozzle at the BASE of the flames,",
                "not at the top. Target the fuel source, not the smoke.",
                "",
                "S - SQUEEZE: Squeeze the handle to release the",
                "extinguishing agent in controlled bursts.",
                "",
                "S - SWEEP: Sweep the nozzle side to side at the base",
                "of the fire until it appears to be completely out.",
                "",
                "Remember: Only attempt to fight small fires. If the fire",
                "is larger than you, growing rapidly, or blocking your",
                "escape route, evacuate immediately.",
                "",
                "In this training, you must extinguish ALL fires to complete",
                "the scenario successfully. Use the PASS technique on each",
                "fire source you encounter.",
                "",
                "CRITICAL: Dietary fires often involve cooking oils and grease - Class K fires.",
                "These require Class K extinguishers with wet chemical agents. NEVER use water",
                "on grease fires as this causes explosive splatter and rapid fire spread.",
                "",
                "Interactive fire extinguishers and burning objects are",
                "highlighted. Press Y on your left controller to view",
                "your task checklist and track your progress.",
                "",
                "Time is critical in fire emergencies. Work efficiently",
                "but safely. Apply the RACE and PASS protocols you've",
                "just learned.",
                "",
                "Begin the fire response training when ready.",
                "Lives depend on your actions. Good luck!"
            };

            // Call the StartDialog method to begin displaying the text.
            dialogueSystem.StartDialog(fireTrainingLines);
        }
        else
        {
            Debug.LogError("VRDialogueSystem reference is not set in the Inspector on " + gameObject.name);
        }
    }
}