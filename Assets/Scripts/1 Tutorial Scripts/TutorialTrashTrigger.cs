using UnityEngine;

// --- THIS SCRIPT GOES ON THE "TRASH CAN" TRIGGER ZONE ---
[RequireComponent(typeof(Collider))]
public class TutorialTrashTrigger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag your 'TutorialDialogueManager' GameObject here.")]
    public TutorialDialogueManager tutorialManager;

    [Header("Settings")]
    [Tooltip("The Tag you will create and assign to your grabbable item.")]
    public string grabbableItemTag = "TutorialGrabbable";

    private bool hasBeenTriggered = false;

    void Start()
    {
        // Make sure this is a trigger
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            Debug.LogWarning("Collider on " + gameObject.name + " is not set to 'Is Trigger'. Forcing it.", this);
            col.isTrigger = true;
        }

        if (tutorialManager == null)
        {
            Debug.LogError("Tutorial Manager is not assigned on " + gameObject.name);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Only fire once
        if (hasBeenTriggered) return;

        // Check if the item that entered has the correct tag
        if (other.CompareTag(grabbableItemTag))
        {
            if (tutorialManager == null) return;

            hasBeenTriggered = true;
            Debug.Log("Item trashed! Advancing dialogue.");
            
            // 1. Advance the dialogue
            tutorialManager.AdvanceDialogue();

            // 2. Destroy the item
            Destroy(other.gameObject);

            // 3. Destroy this trigger zone so it can't be used again
            Destroy(gameObject);
        }
    }
}
