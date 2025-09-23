using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Controls a garage door that lowers when triggered.
/// Can be connected to the PullDownTrigger or other activation methods.
/// </summary>
public class GarageDoorController : MonoBehaviour
{
    [Header("Door Settings")]
    [Tooltip("The GameObject that will move down (the garage door)")]
    public GameObject doorObject;
    
    [Tooltip("How far down the door should move (in units)")]
    public float lowerDistance = 3f;
    
    [Tooltip("How fast the door moves (units per second)")]
    public float moveSpeed = 1f;
    
    [Tooltip("Should the door move smoothly or instantly?")]
    public bool smoothMovement = true;

    [Header("Events")]
    [Tooltip("Called when the door starts moving")]
    public UnityEvent OnDoorStartMoving;
    
    [Tooltip("Called when the door finishes moving")]
    public UnityEvent OnDoorFinishedMoving;

    [Header("Audio (Optional)")]
    [Tooltip("Sound to play when door starts moving")]
    public AudioClip doorMovingSound;
    
    private AudioSource audioSource;
    private Vector3 originalPosition;
    private Vector3 targetPosition;
    private bool isMoving = false;
    private bool isDoorClosed = false;

    void Start()
    {
        // Store the original position
        if (doorObject != null)
        {
            originalPosition = doorObject.transform.position;
            targetPosition = originalPosition - Vector3.up * lowerDistance;
        }
        
        // Get or create audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && doorMovingSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    /// <summary>
    /// Public method to trigger the door closing - call this from your lever script
    /// </summary>
    public void CloseDoor()
    {
        if (isDoorClosed || isMoving || doorObject == null)
        {
            Debug.Log("Door is already closed, moving, or doorObject is null");
            return;
        }

        Debug.Log("Garage door closing initiated!");
        
        // Trigger events
        OnDoorStartMoving?.Invoke();
        
        // Play sound
        if (audioSource != null && doorMovingSound != null)
        {
            audioSource.clip = doorMovingSound;
            audioSource.Play();
        }

        if (smoothMovement)
        {
            StartCoroutine(MoveDoorSmooth());
        }
        else
        {
            MoveDoorInstant();
        }
    }

    /// <summary>
    /// Smooth door movement coroutine
    /// </summary>
    private IEnumerator MoveDoorSmooth()
    {
        isMoving = true;
        Vector3 startPosition = doorObject.transform.position;
        float journeyLength = Vector3.Distance(startPosition, targetPosition);
        float journeyTime = journeyLength / moveSpeed;
        float elapsedTime = 0;

        while (elapsedTime < journeyTime)
        {
            elapsedTime += Time.deltaTime;
            float fractionOfJourney = elapsedTime / journeyTime;
            
            doorObject.transform.position = Vector3.Lerp(startPosition, targetPosition, fractionOfJourney);
            
            yield return null;
        }

        // Ensure we end at the exact target position
        doorObject.transform.position = targetPosition;
        
        CompleteDoorMovement();
    }

    /// <summary>
    /// Instant door movement
    /// </summary>
    private void MoveDoorInstant()
    {
        isMoving = true;
        doorObject.transform.position = targetPosition;
        CompleteDoorMovement();
    }

    /// <summary>
    /// Called when door movement is complete
    /// </summary>
    private void CompleteDoorMovement()
    {
        isMoving = false;
        isDoorClosed = true;
        
        Debug.Log("Garage door closed successfully!");
        OnDoorFinishedMoving?.Invoke();
    }

    /// <summary>
    /// Reset the door to its original position (useful for restarting scenarios)
    /// </summary>
    [ContextMenu("Reset Door")]
    public void ResetDoor()
    {
        if (doorObject != null)
        {
            doorObject.transform.position = originalPosition;
            isDoorClosed = false;
            isMoving = false;
            Debug.Log("Garage door reset to original position");
        }
    }

    /// <summary>
    /// Check if the door is currently closed
    /// </summary>
    public bool IsDoorClosed()
    {
        return isDoorClosed;
    }

    /// <summary>
    /// Check if the door is currently moving
    /// </summary>
    public bool IsDoorMoving()
    {
        return isMoving;
    }

    /// <summary>
    /// Get the current movement progress (0-1)
    /// </summary>
    public float GetMovementProgress()
    {
        if (doorObject == null) return 0f;
        
        float totalDistance = Vector3.Distance(originalPosition, targetPosition);
        float currentDistance = Vector3.Distance(originalPosition, doorObject.transform.position);
        
        return Mathf.Clamp01(currentDistance / totalDistance);
    }

    // Visual debugging
    void OnDrawGizmosSelected()
    {
        if (doorObject != null)
        {
            // Show original position
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(originalPosition, Vector3.one * 0.5f);
            
            // Show target position
            Gizmos.color = Color.red;
            Vector3 target = originalPosition - Vector3.up * lowerDistance;
            Gizmos.DrawWireCube(target, Vector3.one * 0.5f);
            
            // Draw movement path
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(originalPosition, target);
            
            // Show current position if in play mode
            if (Application.isPlaying)
            {
                Gizmos.color = isDoorClosed ? Color.red : (isMoving ? Color.yellow : Color.green);
                Gizmos.DrawWireCube(doorObject.transform.position, Vector3.one * 0.3f);
            }
        }
    }
}