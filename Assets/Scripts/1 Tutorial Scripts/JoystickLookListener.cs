using UnityEngine;
using UnityEngine.XR; // Required for CommonUsages
using UnityEngine.XR.Interaction.Toolkit; // Required for XRController

public class JoystickLookListener : MonoBehaviour
{
    // Assign your DialogueManager here
    public TutorialDialogueManager dialogueManager; 

    // Assign your Right-Hand Controller in the Inspector
    public XRController rightController; 

    private bool conditionMet = false;

    // We'll use a small "deadzone" value
    private float deadzone = 0.2f;

    void Update()
    {
        // If condition is already met, do nothing.
        if (conditionMet) return;

        // Check if the controller is valid and try to get the joystick value
        if (rightController != null && 
            rightController.inputDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 joystickValue))
        {
            // --- THIS IS THE CORRECTED LINE ---
            // Check if the joystick is moved in ANY direction, outside the small deadzone
            if (joystickValue.magnitude > deadzone)
            {
                conditionMet = true;
                Debug.Log("Right joystick moved! Advancing dialogue.");
                dialogueManager.AdvanceDialogue();
            }
        }
    }

    // This is important! Reset the flag when this listener is disabled.
    void OnDisable()
    {
        conditionMet = false;
    }
}