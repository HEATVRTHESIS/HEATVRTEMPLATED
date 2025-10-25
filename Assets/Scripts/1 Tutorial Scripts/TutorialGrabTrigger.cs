using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit; // Required for grab events

// --- THIS SCRIPT GOES ON THE GRABBABLE ITEM ---
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class TutorialGrabTrigger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag your 'TutorialDialogueManager' GameObject here.")]
    public TutorialDialogueManager tutorialManager;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private bool hasBeenGrabbed = false;

    void Awake()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        if (tutorialManager == null)
        {
            Debug.LogError("Tutorial Manager is not assigned on " + gameObject.name);
        }

        // --- MODIFIED ---
        // Subscribe to the "On Grab" event
        // The new (non-obsolete) event is "selectEntered"
        grabInteractable.selectEntered.AddListener(OnGrab);
        // --- END MODIFIED ---
    }

    // This function is called by the XRGrabInteractable when the item is picked up
    // The "SelectEnterEventArgs" signature is correct for the new "selectEntered" event
    public void OnGrab(SelectEnterEventArgs args)
    {
        // Only fire once, and only if the manager is assigned
        if (hasBeenGrabbed || tutorialManager == null) return;

        hasBeenGrabbed = true;
        Debug.Log("Item grabbed! Advancing dialogue.");
        tutorialManager.AdvanceDialogue();

        // --- MODIFIED ---
        // We can unsubscribe now to be safe
        grabInteractable.selectEntered.RemoveListener(OnGrab);
        Debug.Log("Item Remove! Advancing dialogue.");
        tutorialManager.AdvanceDialogue();
        // --- END MODIFIED ---
    }

    // Clean up listener if the object is destroyed
    void OnDestroy()
    {
        if (grabInteractable != null)
        {
            // --- MODIFIED ---
            grabInteractable.selectEntered.RemoveListener(OnGrab);
            // --- END MODIFIED ---
        }
    }
}

