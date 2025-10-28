using UnityEngine;
using UnityEngine.InputSystem;

public class TaskListToggle : MonoBehaviour
{
    [Tooltip("The Canvas or Panel GameObject to toggle")]
    public GameObject taskListPanel;

    [Tooltip("The 'Y' Button (Secondary) action")]
    public InputActionProperty toggleAction;

    void Start()
    {
        // Start with the panel hidden
        if (taskListPanel != null)
        {
            taskListPanel.SetActive(false);
        }

        // Listen for the button press
        if (toggleAction.action != null)
        {
            // We use .performed so it only fires once per press
            toggleAction.action.performed += OnTogglePressed;
            toggleAction.action.Enable();
        }
    }

    // Clean up the listener when the object is destroyed
    private void OnDestroy()
    {
        if (toggleAction.action != null)
        {
            toggleAction.action.performed -= OnTogglePressed;
        }
    }

    // This function is called when the 'Y' button is pressed
    private void OnTogglePressed(InputAction.CallbackContext context)
    {
        if (taskListPanel != null)
        {
            // This is the toggle logic:
            // Set the panel to be the opposite of its current state.
            bool isActive = taskListPanel.activeSelf;
            taskListPanel.SetActive(!isActive);
        }
    }
}