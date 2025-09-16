using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class NPCDialogueController : MonoBehaviour
{
    // (Other public variables as before)
    public GameObject door; 
    public Animator npcAnimator;

    private bool hasEvacuated = false;

    // (Other functions like Start, OnTriggerEnter, etc. remain the same)

    IEnumerator Evacuate()
    {
        HideDialogue();
        hasEvacuated = true;
        
        // --- THIS IS THE MODIFIED PART ---
        
        // 1. Trigger the Turn animation
        npcAnimator.SetTrigger("Evacuate"); 

        // 2. Wait for the turn animation to finish before starting to move
        // You'll need to know the length of your turn animation.
        // Get the length of the "Turn" animation clip
        float turnAnimationLength = GetAnimationClipLength("Turn"); // You will need to implement this helper function or just use a magic number

        yield return new WaitForSeconds(turnAnimationLength); 

        // 3. Begin moving the character towards the door
        Vector3 direction = (door.transform.position - transform.position).normalized;
        
        while (Vector3.Distance(transform.position, door.transform.position) > 1f)
        {
            // The animator is now in the "Walk/Run" state, so this will look correct.
            GetComponent<CharacterController>().Move(direction * 2f * Time.deltaTime);
            yield return null;
        }

        // 4. Destroy the NPC object once they are out the door
        Destroy(gameObject); 
    }
    
    // Helper function to get the animation clip length
    private float GetAnimationClipLength(string clipName)
    {
        if (npcAnimator == null) return 0f;
        
        foreach (var clip in npcAnimator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName)
            {
                return clip.length;
            }
        }
        return 0f;
    }
}