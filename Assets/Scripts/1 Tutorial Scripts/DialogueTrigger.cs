using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    // Drag your DialogueManager GameObject here in the Inspector
    public TutorialDialogueManager dialogueManager;

    // This bool prevents the trigger from firing multiple times
    private bool hasTriggered = false;

    // This function is called by Unity when another collider enters this trigger
    private void OnTriggerEnter(Collider other)
    {
        // We check two things:
        // 1. Has this trigger already been used?
        // 2. Did the object that entered have the "Player" tag?
        if (!hasTriggered && other.CompareTag("Player"))
        {
            // Mark as triggered so it doesn't run again
            hasTriggered = true;
            
            Debug.Log("Player entered trigger zone. Starting dialogue.");
            
            // Call the public function on the DialogueManager
            dialogueManager.StartDialogue();
            
            // Optional: You can make the trigger zone disappear after use
            // gameObject.SetActive(false);
        }
    }
}