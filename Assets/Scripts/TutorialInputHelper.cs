using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

/// <summary>
/// Helper script to detect thumbstick input and trigger tutorial turning events.
/// Attach this to your XR Rig or any GameObject in the scene.
/// This is a separate helper so you can customize the input detection without touching TutorialManager.
/// </summary>
public class TutorialInputHelper : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the TutorialManager")]
    [SerializeField] private TutorialManager tutorialManager;
    
    [Header("Settings")]
    [Tooltip("Threshold for detecting thumbstick movement (0-1)")]
    [SerializeField] private float thumbstickThreshold = 0.7f;
    
    [Tooltip("Cooldown time between detections (prevents multiple triggers)")]
    [SerializeField] private float detectionCooldown = 0.5f;
    
    private InputDevice rightController;
    private bool rightControllerFound = false;
    private float lastDetectionTime = 0f;
    
    // Flags to prevent multiple triggers
    private bool rightDetected = false;
    private bool leftDetected = false;
    private bool downDetected = false;
    
    void Update()
    {
        // Find right controller if not found yet
        if (!rightControllerFound)
        {
            rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (rightController.isValid)
            {
                rightControllerFound = true;
                Debug.Log("TutorialInputHelper: Right controller found!");
            }
            return;
        }
        
        // Check if enough time has passed since last detection
        if (Time.time - lastDetectionTime < detectionCooldown)
        {
            return;
        }
        
        // Get thumbstick input
        if (rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 thumbstick))
        {
            // Detect RIGHT movement
            if (thumbstick.x > thumbstickThreshold && !rightDetected)
            {
                rightDetected = true;
                lastDetectionTime = Time.time;
                
                if (tutorialManager != null)
                {
                    Debug.Log("TutorialInputHelper: Right thumbstick detected - calling OnTurnedRight()");
                    tutorialManager.OnTurnedRight();
                }
            }
            else if (thumbstick.x < thumbstickThreshold * 0.5f)
            {
                rightDetected = false; // Reset when thumbstick returns to center
            }
            
            // Detect LEFT movement
            if (thumbstick.x < -thumbstickThreshold && !leftDetected)
            {
                leftDetected = true;
                lastDetectionTime = Time.time;
                
                if (tutorialManager != null)
                {
                    Debug.Log("TutorialInputHelper: Left thumbstick detected - calling OnTurnedLeft()");
                    tutorialManager.OnTurnedLeft();
                }
            }
            else if (thumbstick.x > -thumbstickThreshold * 0.5f)
            {
                leftDetected = false; // Reset when thumbstick returns to center
            }
            
            // Detect DOWN movement
            if (thumbstick.y < -thumbstickThreshold && !downDetected)
            {
                downDetected = true;
                lastDetectionTime = Time.time;
                
                if (tutorialManager != null)
                {
                    Debug.Log("TutorialInputHelper: Down thumbstick detected - calling OnTurnedDown()");
                    tutorialManager.OnTurnedDown();
                }
            }
            else if (thumbstick.y > -thumbstickThreshold * 0.5f)
            {
                downDetected = false; // Reset when thumbstick returns to center
            }
        }
    }
    
    void OnValidate()
    {
        // Auto-find TutorialManager if not assigned
        if (tutorialManager == null)
        {
            tutorialManager = FindObjectOfType<TutorialManager>();
        }
    }
}
