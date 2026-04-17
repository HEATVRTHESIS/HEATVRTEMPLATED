using UnityEngine;

/// <summary>
/// Practice-mode intro dialogue for the risk reduction phase.
/// Attach this script to an empty GameObject in the practice scene.
/// </summary>
public class PracticeRiskReductionStartDialogue : MonoBehaviour
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
                "This is the risk reduction phase for the level you selected.",
                "Use this time to get familiar with the environment",
                "and complete the level at your own pace.",
                "",
                "This is an unguided version of the level, so objects are not",
                "highlighted and you will need to explore and proceed on your own.",
                "",
                "The goal of practice mode is to learn the flow of the level",
                "and build confidence before starting the full scenario.",
                "",
                "Take your time, explore carefully, and do not worry about mistakes.",
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