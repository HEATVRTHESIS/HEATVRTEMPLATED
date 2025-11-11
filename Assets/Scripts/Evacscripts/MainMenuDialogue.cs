using UnityEngine;

/// <summary>
/// Updated main menu dialogue that now works with the tutorial system.
/// This script should be attached to an empty GameObject in your tutorial scene.
/// </summary>
public class MainMenuDialogue : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag the GameObject with the TutorialManager component here.")]
    [SerializeField]
    private TutorialManager tutorialManager;

    void Start()
    {
        // Check if the tutorial manager reference is set
        if (tutorialManager != null)
        {
            Debug.Log("Tutorial system initialized. The TutorialManager will handle all dialogue.");
            // The TutorialManager will automatically start the tutorial in its Start() method
        }
        else
        {
            Debug.LogError("TutorialManager reference is not set in the Inspector on " + gameObject.name);
        }
    }
}