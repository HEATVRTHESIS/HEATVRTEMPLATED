using UnityEngine;
using UnityEngine.InputSystem;

public class ToggleObject : MonoBehaviour
{
    // Reference to the Input Action you created
    public InputActionReference toggleAction;

    // Reference to the GameObject you want to toggle
    public GameObject objectToToggle;

    // Subscribe to the action when the script is enabled
    private void OnEnable()
    {
        toggleAction.action.Enable();
        toggleAction.action.performed += ToggleVisibility;
    }

    // Unsubscribe from the action when the script is disabled
    private void OnDisable()
    {
        toggleAction.action.performed -= ToggleVisibility;
        toggleAction.action.Disable();
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