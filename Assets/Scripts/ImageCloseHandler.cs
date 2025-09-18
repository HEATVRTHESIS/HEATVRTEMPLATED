using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ImageCloseHandler : MonoBehaviour
{
    [Tooltip("The Input Action for the button to close the image.")]
    public InputActionProperty closeImageAction;

    [Tooltip("The UI Image component to display the images.")]
    public Image imageDisplay;

    [Tooltip("The array of images to cycle through in order.")]
    public Sprite[] orderedImages;

    private int _currentImageIndex = 0;

    private void OnEnable()
    {
        // When the pop-up becomes active, start listening for input
        closeImageAction.action.Enable();
        closeImageAction.action.performed += OnCloseButtonPressed;

        // Reset the index and display the first image
        _currentImageIndex = 0;
        UpdateImageDisplay();
    }

    private void OnDisable()
    {
        // When the pop-up becomes inactive, stop listening for input
        closeImageAction.action.performed -= OnCloseButtonPressed;
        closeImageAction.action.Disable();
    }

    private void OnCloseButtonPressed(InputAction.CallbackContext context)
    {
        // Check if there are more images to show
        if (_currentImageIndex < orderedImages.Length - 1)
        {
            // Increment the index and show the next image
            _currentImageIndex++;
            UpdateImageDisplay();
        }
        else
        {
            // We're at the last image, so close the pop-up
            gameObject.SetActive(false);
        }
    }

    private void UpdateImageDisplay()
    {
        // Make sure we have a reference to the Image component and images to display
        if (imageDisplay != null && orderedImages.Length > 0)
        {
            imageDisplay.sprite = orderedImages[_currentImageIndex];
        }
    }
}