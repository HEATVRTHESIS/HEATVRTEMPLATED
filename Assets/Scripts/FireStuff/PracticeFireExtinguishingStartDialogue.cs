using UnityEngine;

/// <summary>
/// Practice-mode intro dialogue for the fire extinguishing phase.
/// Attach this script to an empty GameObject in the practice scene.
/// </summary>
public class PracticeFireExtinguishingStartDialogue : MonoBehaviour
{
    [Tooltip("Drag the GameObject with the VRDialogueSystem component here.")]
    [SerializeField]
    private VRDialogueSystem dialogueSystem;

    void Start()
    {
        if (dialogueSystem != null)
        {
            string[] practiceIntroLines = new string[]
            {
                "Welcome to Practice Mode.",
                "",
                "This is the fire extinguishing phase for the level you selected.",
                "Use this time to get comfortable with the equipment",
                "and learn the room layout at your own pace.",
                "",
                "This is an unguided version of the level, so objects are not",
                "highlighted and you will need to navigate and act independently.",
                "",
                "The goal of practice mode is to help you build confidence",
                "before starting the full fire scenario.",
                "",
                "Take your time, work carefully, and do not worry about mistakes.",
                "This is the place to learn the level safely.",
                "",
                "Begin when you are ready."
            };

            dialogueSystem.StartDialog(practiceIntroLines);
        }
        else
        {
            Debug.LogError("VRDialogueSystem reference is not set in the Inspector on " + gameObject.name);
        }
    }
}