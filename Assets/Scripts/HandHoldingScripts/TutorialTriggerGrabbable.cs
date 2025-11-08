using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Attach this to any XR Grab Interactable object to trigger tutorial indicators and audio.
/// On first grab: plays audio message
/// On every grab: shows visual indicator
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class TutorialTriggerGrabbable : MonoBehaviour
{
    [Header("Object Identity")]
    [Tooltip("Unique name for this tutorial trigger. Leave empty to use GameObject name.")]
    public string uniqueTriggerId = "";
    
    [Header("Indicator Settings")]
    [Tooltip("Where to show the indicator when this object is grabbed (e.g., trash can location)")]
    public Transform indicatorTarget;
    
    [Tooltip("Text to display above the arrow indicator")]
    [TextArea(2, 4)]
    public string indicatorText = "Place object here";
    
    [Header("Audio Settings - Choose One")]
    [Tooltip("Text message to speak using RT-Voice (first grab only)")]
    [TextArea(3, 6)]
    public string audioMessageTTS = "";
    
    [Tooltip("OR use a pre-recorded audio clip (first grab only)")]
    public AudioClip audioClip;
    
    [Header("Behavior")]
    [Tooltip("Hide indicator when object is released/dropped")]
    public bool hideIndicatorOnDrop = true;
    
    [Tooltip("Only show indicator while object is grabbed")]
    public bool onlyShowWhileGrabbed = false;
    
    private VRTutorialManager tutorialManager;
    private XRGrabInteractable grabInteractable;
    private string triggerId;
    
    private void Awake()
    {
        // Get the grab interactable component
        grabInteractable = GetComponent<XRGrabInteractable>();
        
        // Set up unique ID
        triggerId = string.IsNullOrEmpty(uniqueTriggerId) ? gameObject.name : uniqueTriggerId;
    }
    
    private void Start()
    {
        // Find tutorial manager
        tutorialManager = FindObjectOfType<VRTutorialManager>();
        if (tutorialManager == null)
        {
            Debug.LogError($"[TutorialTriggerGrabbable] VRTutorialManager not found in scene! Attached to: {gameObject.name}");
            enabled = false;
            return;
        }
        
        // Validate setup
        if (indicatorTarget == null)
        {
            Debug.LogWarning($"[TutorialTriggerGrabbable] No indicator target assigned on {gameObject.name}");
        }
        
        if (string.IsNullOrEmpty(audioMessageTTS) && audioClip == null)
        {
            Debug.LogWarning($"[TutorialTriggerGrabbable] No audio message or clip assigned on {gameObject.name}. Only indicator will show.");
        }
    }
    
    private void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnObjectGrabbed);
            grabInteractable.selectExited.AddListener(OnObjectReleased);
        }
    }
    
    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnObjectGrabbed);
            grabInteractable.selectExited.RemoveListener(OnObjectReleased);
        }
    }
    
    /// <summary>
    /// Called when the object is grabbed
    /// </summary>
    private void OnObjectGrabbed(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
    {
        if (tutorialManager == null || indicatorTarget == null) return;
        
        // Don't show if onlyShowWhileGrabbed is false (we'll show it on release instead)
        if (!onlyShowWhileGrabbed) return;
        
        // Trigger the tutorial manager
        tutorialManager.OnObjectGrabbed(
            triggerId,
            indicatorTarget,
            indicatorText,
            audioMessageTTS,
            audioClip
        );
        
        Debug.Log($"[TutorialTriggerGrabbable] Object grabbed: {gameObject.name}");
    }
    
    /// <summary>
    /// Called when the object is released
    /// </summary>
    private void OnObjectReleased(UnityEngine.XR.Interaction.Toolkit.SelectExitEventArgs args)
    {
        if (tutorialManager == null) return;
        
        // Show indicator on release if not showing while grabbed
        if (!onlyShowWhileGrabbed && indicatorTarget != null)
        {
            tutorialManager.OnObjectGrabbed(
                triggerId,
                indicatorTarget,
                indicatorText,
                audioMessageTTS,
                audioClip
            );
            
            Debug.Log($"[TutorialTriggerGrabbable] Object released, showing indicator: {gameObject.name}");
        }
        // Hide indicator if configured to do so
        else if (hideIndicatorOnDrop)
        {
            tutorialManager.DestroyIndicator();
            Debug.Log($"[TutorialTriggerGrabbable] Object released, hiding indicator: {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Manually trigger the tutorial (can be called from other scripts or Unity Events)
    /// </summary>
    public void ManuallyTriggerTutorial()
    {
        if (tutorialManager != null && indicatorTarget != null)
        {
            tutorialManager.OnObjectGrabbed(
                triggerId,
                indicatorTarget,
                indicatorText,
                audioMessageTTS,
                audioClip
            );
        }
    }
    
    /// <summary>
    /// Force hide the indicator
    /// </summary>
    public void HideIndicator()
    {
        if (tutorialManager != null)
        {
            tutorialManager.DestroyIndicator();
        }
    }
    
    /// <summary>
    /// Update the indicator target at runtime
    /// </summary>
    public void SetIndicatorTarget(Transform newTarget)
    {
        indicatorTarget = newTarget;
    }
    
    /// <summary>
    /// Update the indicator text at runtime
    /// </summary>
    public void SetIndicatorText(string newText)
    {
        indicatorText = newText;
    }
    
    /// <summary>
    /// Check if this trigger has been activated before
    /// </summary>
    public bool HasBeenTriggered()
    {
        if (tutorialManager != null)
        {
            return tutorialManager.HasBeenTriggered(triggerId);
        }
        return false;
    }
}