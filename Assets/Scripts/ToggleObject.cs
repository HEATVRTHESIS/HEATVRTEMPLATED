using UnityEngine;
using UnityEngine.InputSystem;

public class ToggleObject : MonoBehaviour
{
    // Reference to the GameObject you want to toggle
    public GameObject objectToToggle;

    // Input action for the left controller X button
    private InputAction toggleAction;

    private void Awake()
    {
        // Create an input action for the left controller X button (secondary button)
        toggleAction = new InputAction(
            name: "Toggle",
            binding: "<XRController>{LeftHand}/secondaryButton"
        );
    }

    // Subscribe to the action when the script is enabled
    private void OnEnable()
    {
        toggleAction.Enable();
        toggleAction.performed += ToggleVisibility;
    }

    // Unsubscribe from the action when the script is disabled
    private void OnDisable()
    {
        toggleAction.performed -= ToggleVisibility;
        toggleAction.Disable();
    }

    // This function is called every time the action is performed
    private void ToggleVisibility(InputAction.CallbackContext context)
    {
        if (objectToToggle != null)
        {
            // Toggle the active state of the GameObject
            objectToToggle.SetActive(!objectToToggle.activeSelf);
        }
    }
}