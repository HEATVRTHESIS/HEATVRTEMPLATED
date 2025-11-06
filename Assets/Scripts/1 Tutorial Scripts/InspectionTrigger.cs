using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// This script listens to a Ray Interactor and shows a hover icon
/// when it points at an "InvestigatableObject".
/// 
/// Place this script on your XR Origin.
/// </summary>
public class HoverIconController : MonoBehaviour
{
    [Header("Components")]
    [Tooltip("The XR Ray Interactor to listen to (e.g., your Left Hand's ray)")]
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor rayInteractor;

    [Tooltip("The Magnifying Glass UI GameObject to show/hide")]
    public GameObject hoverIconUI;

    void Start()
    {
        // --- Error Checking ---
        if (rayInteractor == null)
        {
            Debug.LogError("HoverIconController: Ray Interactor is not assigned!", this);
            return;
        }
        if (hoverIconUI == null)
        {
            Debug.LogError("HoverIconController: Hover Icon UI is not assigned!", this);
            return;
        }

        // --- Setup ---
        // Start with the icon hidden
        hoverIconUI.SetActive(false);

        // --- Listen for Events ---
        // When the ray *starts* hitting something...
        rayInteractor.hoverEntered.AddListener(OnHoverStarted);
        // When the ray *stops* hitting something...
        rayInteractor.hoverExited.AddListener(OnHoverEnded);
    }

    private void OnDestroy()
    {
        // Clean up listeners when the object is destroyed
        if (rayInteractor != null)
        {
            rayInteractor.hoverEntered.RemoveListener(OnHoverStarted);
            rayInteractor.hoverExited.RemoveListener(OnHoverEnded);
        }
    }

    // This function is called when the ray *starts* hovering
    private void OnHoverStarted(HoverEnterEventArgs args)
    {
        // Check if the object we're pointing at has the "InvestigatableObject" tag script
        if (args.interactableObject.transform.TryGetComponent<InvestigatableObject>(out _))
        {
            // If it does, show the icon
            hoverIconUI.SetActive(true);
        }
    }

    // This function is called when the ray *stops* hovering
    private void OnHoverEnded(HoverExitEventArgs args)
    {
        // Check if the object we *just* stopped pointing at had the "InvestigatableObject" tag script
        if (args.interactableObject.transform.TryGetComponent<InvestigatableObject>(out _))
        {
            // If it did, hide the icon
            hoverIconUI.SetActive(false);
        }
    }
}