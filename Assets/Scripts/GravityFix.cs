using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GravityFix : MonoBehaviour
{
    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    
    void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        
        // Subscribe to release event
        grabInteractable.selectExited.AddListener(OnRelease);
    }
    
    private void OnRelease(SelectExitEventArgs args)
    {
        // Force restore physics
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }
    
    void OnDestroy()
    {
        if (grabInteractable != null)
            grabInteractable.selectExited.RemoveListener(OnRelease);
    }
}