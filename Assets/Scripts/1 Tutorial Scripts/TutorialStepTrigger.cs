using UnityEngine;
using UnityEngine.Events;

public class TutorialStepTrigger : MonoBehaviour
{
    public UnityEvent OnPlayerEnter;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object that entered is the player
        if (other.CompareTag("Player"))
        {
            // If it is the player, invoke the event.
            // This will call "AdvanceDialogue()"
            OnPlayerEnter.Invoke();

            // --- THIS IS THE CHANGE ---
            // This destroys the trigger object and all its children
            // (including any particle systems).
            Destroy(gameObject);
        }
    }
}