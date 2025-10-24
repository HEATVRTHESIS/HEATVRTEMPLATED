using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class GrabObjectListener : MonoBehaviour
{
    // Assign your DialogueManager here
    public TutorialDialogueManager dialogueManager;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;

    void Awake()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grabInteractable == null)
        {
            Debug.LogError("GrabObjectListener needs to be on an object with an XRGrabInteractable.");
        }
    }

    // Subscribe to the "Select Entered" event (which means "Grabbed")
    void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnObjectGrabbed);
        }
    }

    // Unsubscribe when disabled
    void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnObjectGrabbed);
        }
    }

    private void OnObjectGrabbed(SelectEnterEventArgs args)
    {
        Debug.Log("Object grabbed! Advancing dialogue.");
        dialogueManager.AdvanceDialogue();
        
        // We only want this to trigger once, so we disable this component.
        // The manager will disable our parent GameObject anyway.
        this.enabled = false; 
    }
}