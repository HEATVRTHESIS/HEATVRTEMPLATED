using UnityEngine;
using UnityEngine.InputSystem;
using TMPro; // For the UI text
using UnityEngine.UI; // For the Button

/// <summary>
/// This script manages the "Investigation Mode" UI using a trigger collider.
/// Place this on your Hand Controller (e.g., LeftHand Controller).
/// This hand MUST have a Sphere Collider set to "Is Trigger" and a Rigidbody set to "Is Kinematic".
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class InvestigationController : MonoBehaviour
{
    [Header("XR Components")]
    [Tooltip("The 'X' Button (Primary) action")]
    public InputActionProperty investigateButtonAction;

    [Header("Investigation UI")]
    [Tooltip("The WORLD SPACE UI element for the magnifying glass icon")]
    public GameObject hoverIconUI;
    
    [Tooltip("The WORLD SPACE UI panel that shows the details")]
    public GameObject detailsPanelUI;
    
    // --- THIS IS THE FIX ---
    [Tooltip("The TextMeshPro UI element for the object's title")]
    public TextMeshProUGUI titleText; // Corrected from TextMeshProUI
    
    [Tooltip("The TextMeshPro UI element for the object's description")]
    public TextMeshProUGUI descriptionText; // Corrected from TextMeshProUI
    // --- END FIX ---
    
    [Tooltip("The 'Close' button on your details panel")]
    public Button closeButton;

    // Private variable to store what we are currently hovering over
    private InvestigatableObject currentHoveredObject;

    void Start()
    {
        // --- Validate Collider Setup ---
        SphereCollider collider = GetComponent<SphereCollider>();
        if (!collider.isTrigger)
        {
            Debug.LogWarning("Sphere Collider on InvestigationController is not set to 'Is Trigger'. Forcing it.", this);
            collider.isTrigger = true;
        }

        // --- Add Listeners ---
        
        // Listen for the 'X' button press
        if (investigateButtonAction.action != null)
        {
            investigateButtonAction.action.performed += OnInvestigatePressed;
            investigateButtonAction.action.Enable();
        }

        // Listen for the 'Close' button click
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseDetailsPanel);
        }

        // --- Start Hidden ---
        if (hoverIconUI != null)
            hoverIconUI.SetActive(false);
        
        if (detailsPanelUI != null)
            detailsPanelUI.SetActive(false);
    }

    private void OnDestroy()
    {
        // --- Clean up Listeners ---
        if (investigateButtonAction.action != null)
        {
            investigateButtonAction.action.performed -= OnInvestigatePressed;
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseDetailsPanel);
        }
    }

    // Called by the Sphere Collider when it touches another collider
    private void OnTriggerEnter(Collider other)
    // --- THE STRAY 'S' IS REMOVED FROM HERE ---
    {
        // Check if the object we are touching is an "InvestigatableObject"
        if (other.TryGetComponent(out InvestigatableObject obj))
        {
            currentHoveredObject = obj;
            
            // Show the magnifying glass, but only if the details panel isn't already open
            if (detailsPanelUI != null && !detailsPanelUI.activeSelf)
            {
                hoverIconUI.SetActive(true);
            }
        }
    }

    // Called by the Sphere Collider when it stops touching another collider
    private void OnTriggerExit(Collider other)
    {
        // Check if the object we are leaving is the one we were tracking
        if (other.TryGetComponent(out InvestigatableObject obj) && obj == currentHoveredObject)
        {
            currentHoveredObject = null;
            
            // Hide the magnifying glass
            if (hoverIconUI != null)
            {
                hoverIconUI.SetActive(false);
            }
        }
    }

    // Called by the 'X' Button Input Action
    private void OnInvestigatePressed(InputAction.CallbackContext context)
    {
        // If we are hovering an object when 'X' is pressed...
        if (currentHoveredObject != null)
        {
            // ...show its details.
            ShowDetailsPanel(currentHoveredObject);
        }
    }

    // This function populates and displays the details panel
    private void ShowDetailsPanel(InvestigatableObject obj)
    {
        if (detailsPanelUI == null || titleText == null || descriptionText == null)
            return;

        // Hide the magnifying glass
        if (hoverIconUI != null)
        {
            hoverIconUI.SetActive(false);
        }

        // Set the text
        titleText.text = obj.objectTitle;
        descriptionText.text = obj.objectDescription;

        // Show the panel
        detailsPanelUI.SetActive(true);
    }

    // This function is called by the 'Close' button's OnClick event
    public void CloseDetailsPanel()
    {
        if (detailsPanelUI != null)
        {
            detailsPanelUI.SetActive(false);
        }

        // After closing, check if we are *still* hovering the object.
        // If so, re-show the magnifying glass.
        if (currentHoveredObject != null && hoverIconUI != null)
        {
            hoverIconUI.SetActive(true);
        }
    }
}