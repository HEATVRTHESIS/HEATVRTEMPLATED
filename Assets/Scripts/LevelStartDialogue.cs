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
                "Welcome to the game.",
                "This is the dietary department."
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
