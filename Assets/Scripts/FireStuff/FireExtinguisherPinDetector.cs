using UnityEngine;


/// <summary>
/// Simple pin detection system - enables nozzle grabbing when pin is no longer inside handle area
/// </summary>
public class FireExtinguisherPinDetector : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The XR Grab Interactable on the nozzle/handle")]
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable nozzleGrabInteractable;
    
    [Tooltip("The pin GameObject with box collider")]
    public GameObject safetyPin;
    
    [Tooltip("The handle GameObject with box collider (detection zone)")]
    public GameObject handle;

    [Header("Detection Settings")]
    [Tooltip("How often to check if pin is still inside handle (seconds)")]
    public float checkInterval = 0.1f;

    // Private variables
    private Collider pinCollider;
    private Collider handleCollider;
    private bool isPinInside = true;
    private bool nozzleEnabled = false;

    void Start()
    {
        // Get colliders
        if (safetyPin != null)
            pinCollider = safetyPin.GetComponent<Collider>();
            
        if (handle != null)
            handleCollider = handle.GetComponent<Collider>();

        // Initially disable nozzle grabbing
        if (nozzleGrabInteractable != null)
        {
            nozzleGrabInteractable.enabled = false;
        }

        // Start checking pin position
        InvokeRepeating(nameof(CheckPinPosition), 0f, checkInterval);
    }

    /// <summary>
    /// Checks if the pin's collider is still inside the handle's collider
    /// </summary>
    void CheckPinPosition()
    {
        if (pinCollider == null || handleCollider == null) return;

        // Check if pin bounds are completely outside handle bounds
        bool pinStillInside = handleCollider.bounds.Intersects(pinCollider.bounds);

        // If pin status changed from inside to outside
        if (isPinInside && !pinStillInside)
        {
            OnPinRemoved();
        }
        // If pin went from outside back to inside
        else if (!isPinInside && pinStillInside)
        {
            OnPinReinserted();
        }

        isPinInside = pinStillInside;
    }

    /// <summary>
    /// Called when pin is detected as removed from handle area
    /// </summary>
    void OnPinRemoved()
    {
        if (nozzleEnabled) return; // Already enabled

        nozzleEnabled = true;
        
        if (nozzleGrabInteractable != null)
        {
            nozzleGrabInteractable.enabled = true;
        }

        Debug.Log("Pin removed! Nozzle is now grabbable.");
    }

    /// <summary>
    /// Called when pin is detected as back inside handle area
    /// </summary>
    void OnPinReinserted()
    {
        if (!nozzleEnabled) return; // Already disabled

        nozzleEnabled = false;
        
        if (nozzleGrabInteractable != null)
        {
            nozzleGrabInteractable.enabled = false;
        }

        Debug.Log("Pin reinserted! Nozzle is no longer grabbable.");
    }

    /// <summary>
    /// Public method to check current pin status
    /// </summary>
    public bool IsPinRemoved()
    {
        return !isPinInside;
    }

    /// <summary>
    /// Public method to check if nozzle is currently grabbable
    /// </summary>
    public bool IsNozzleGrabbable()
    {
        return nozzleEnabled;
    }

    void OnDestroy()
    {
        // Stop the repeating check
        CancelInvoke(nameof(CheckPinPosition));
    }
}