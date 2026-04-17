using UnityEngine;

/// <summary>
/// Practice-mode intro dialogue for the evacuation scenario.
/// Attach this script to an empty GameObject in the practice scene.
/// </summary>
public class PracticeEvacuationStartDialogue : MonoBehaviour
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
                "This is the evacuation practice for the level you selected.",
                "Use this time to learn the route and get familiar with the",
                "evacuation flow at your own pace.",
                "",
                "This is an unguided version of the level, so objects are not",
                "highlighted and you will need to find your way independently.",
                "",
                "The goal of practice mode is to help you build confidence",
                "before starting the full evacuation scenario.",
                "",
                "Take your time, stay aware of your surroundings,",
                "and do not worry about mistakes.",
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