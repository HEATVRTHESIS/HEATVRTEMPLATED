using UnityEngine;
using UnityEngine.InputSystem;

public class ImageCloseHandler : MonoBehaviour
{
    [Tooltip("The Input Action for the button to close the image.")]
    public InputActionProperty closeImageAction;

    private void OnEnable()
    {
        // When the image becomes active, start listening for input
        closeImageAction.action.Enable();
        closeImageAction.action.performed += OnCloseButtonPressed;
    }

    private void OnDisable()
    {
        // When the image becomes inactive, stop listening for input
        closeImageAction.action.performed -= OnCloseButtonPressed;
        closeImageAction.action.Disable();
    }

    private void OnCloseButtonPressed(InputAction.CallbackContext context)
    {
        // Deactivate the image pop-up
        gameObject.SetActive(false);
    }
}