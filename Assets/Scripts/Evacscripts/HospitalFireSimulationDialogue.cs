using UnityEngine;

/// <summary>
/// A script to start the dialogue system for the hospital fire simulation scenario.
/// The player narrates their experience after successfully handling a fire in their department,
/// only to discover the entire hospital is engulfed in flames.
/// Attach this script to an empty GameObject in your scene.
/// </summary>
public class HospitalFireSimulationDialogue : MonoBehaviour
{
    [Tooltip("Drag the GameObject with the VRDialogueSystem component here.")]
    [SerializeField]
    private VRDialogueSystem dialogueSystem;

    void Start()
    {
        // Check if the dialogue system reference is set to avoid errors.
        if (dialogueSystem != null)
        {
            // Define the lines to be displayed in first-person perspective.
            string[] simulationLines = new string[]
            {
                "We did it... I managed to get my colleague out and put out",
                "that fire in our department. I thought we had it under control.",
                "",
                "But as soon as I stepped into the hallway...",
                "",
                "Oh no. No, no, no. The entire hospital is on fire.",
                "The other departments... they weren't prepared. The flames",
                "are everywhere. Smoke is filling the corridors.",
                "",
                "I can hear people calling for help. There are patients still",
                "trapped in there. I have to do something.",
                "",
                "I can't breathe in this smoke. I need to find a cloth and",
                "wet it with water or something. Without it, I won't be able",
                "to breathe through these conditions.",
                "",
                "My oxygen is already dropping. I need to watch that meter",
                "carefully. If it hits zero, I'm done.",
                "",
                "I can't save everyone, but I'll save who I can on my way out.",
                "Any patient I see, any colleague who needs help... I'll do",
                "what I can. But I have to keep moving.",
                "",
                "The flames are spreading fast. I need to avoid anything that's",
                "burning. One wrong move and I could get seriously hurt.",
                "My health watch shows I'm still okay, but I can't afford to",
                "take any damage.",
                "",
                "The compass is pointing toward the nearest exit. That's my",
                "way out. I need to follow it and get to safety.",
                "",
                "This is real now. No training scenario, no safety nets.",
                "People are counting on me, but I can't help anyone if I",
                "don't make it out alive.",
                "",
                "Stay calm. Find that cloth. Watch the oxygen. Help who",
                "you can. Avoid the fire. Follow the compass.",
                "",
                "I can do this. I have to do this.",
                "",
                "Let's move!"
            };

            // Call the StartDialog method to begin displaying the text.
            dialogueSystem.StartDialog(simulationLines);
        }
        else
        {
            Debug.LogError("VRDialogueSystem reference is not set in the Inspector on " + gameObject.name);
        }
    }
}