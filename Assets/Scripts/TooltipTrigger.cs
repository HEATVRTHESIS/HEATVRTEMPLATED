using UnityEngine;


public class TooltipTrigger : MonoBehaviour
{
    [Header("Tooltip Content")]
    public Sprite controllerButtonImage;
    [TextArea(3, 5)]
    public string tooltipDescription = "Press to grab";
    
    [Header("Stability")]
    public float actionCooldown = 0.15f;
    
    [Header("Grab State Control")]
    public bool hideWhenGrabbed = true;
    
    private VRTooltipManager tooltipManager;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private float lastActionTime;
    
    private void Start()
    {
        // Find the tooltip manager in the scene
        tooltipManager = FindObjectOfType<VRTooltipManager>();
        if (tooltipManager == null)
        {
            Debug.LogError("VRTooltipManager not found in scene!");
        }
        
        // Get the grab interactable component to check grab state
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
    }
    
    // Simple public methods you can call from Unity Events
    public void CreateTooltip()
    {
        if (Time.time - lastActionTime < actionCooldown)
        {
            return;
        }
        
        // Don't show tooltip if object is being grabbed (and setting is enabled)
        if (hideWhenGrabbed && IsBeingGrabbed())
        {
            return;
        }
        
        lastActionTime = Time.time;
        
        if (tooltipManager != null)
        {
            tooltipManager.CreateTooltip(transform, controllerButtonImage, tooltipDescription);
        }
    }
    
    public void DestroyTooltip()
    {
        if (Time.time - lastActionTime < actionCooldown)
        {
            return;
        }
        
        lastActionTime = Time.time;
        
        if (tooltipManager != null)
        {
            tooltipManager.DestroyTooltip();
        }
    }
    
    // Check if the object is currently being grabbed
    private bool IsBeingGrabbed()
    {
        if (grabInteractable != null)
        {
            return grabInteractable.isSelected;
        }
        return false;
    }
    
    // Force hide tooltip when grabbed (call this from Select Entered event if needed)
    public void OnObjectGrabbed()
    {
        if (hideWhenGrabbed)
        {
            DestroyTooltip();
        }
    }
    
    // Alternative methods with custom content
    public void CreateTooltipWithText(string customText)
    {
        if (tooltipManager != null && !IsBeingGrabbed())
        {
            tooltipManager.CreateTooltip(transform, controllerButtonImage, customText);
        }
    }
    
    public void CreateCustomTooltip(Sprite customImage, string customText)
    {
        if (tooltipManager != null && !IsBeingGrabbed())
        {
            tooltipManager.CreateTooltip(transform, customImage, customText);
        }
    }
    
    // Public methods for manual control
    public void ShowTooltip()
    {
        CreateTooltip();
    }
    
    public void HideTooltip()
    {
        DestroyTooltip();
    }
    
    // Method to update tooltip content dynamically
    public void UpdateTooltipContent(Sprite newImage, string newDescription)
    {
        controllerButtonImage = newImage;
        tooltipDescription = newDescription;
        
        if (tooltipManager != null && tooltipManager.IsTooltipActive())
        {
            tooltipManager.UpdateTooltipContent(newImage, newDescription);
        }
    }
}