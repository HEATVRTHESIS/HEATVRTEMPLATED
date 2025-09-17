using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    [SerializeField]
    private VRDialogueSystem dialogueSystem;

    // A reference to the image pop-up to show next
    public GameObject imagePopup;

    void Start()
    {
        // Make sure the image pop-up is initially hidden
        if (imagePopup != null)
        {
            imagePopup.SetActive(false);
        }

        if (dialogueSystem != null)
        {
            // Subscribe to the event before starting the dialogue
            dialogueSystem.OnDialogueEnd.AddListener(OnDialogueFinished);

            string[] tutorialLines = new string[]
            {
                "Welcome to HeatVR\nYour training starts now", 
                "Use the left\nthumbstick to move",
            };
            
            // Start the dialogue
            dialogueSystem.StartDialog(tutorialLines);
        }
        else
        {
            Debug.LogError("VRDialogueSystem reference is not set in the Inspector on " + gameObject.name);
        }
    }
    
    // This function will be called when the dialogue ends
    public void OnDialogueFinished()
    {
        if (imagePopup != null)
        {
            imagePopup.SetActive(true);
        }
    }
}