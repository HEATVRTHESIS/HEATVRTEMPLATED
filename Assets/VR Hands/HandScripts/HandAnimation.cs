using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
public class HandAnimation : MonoBehaviour
{
    [SerializeField] private InputActionReference gripAction;
    [SerializeField] private InputActionReference pinchAction;
    private Animator animator;

    private void Awake() 
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        // Enable actions before subscribing
        if (gripAction != null && gripAction.action != null)
        {
            gripAction.action.Enable();
            gripAction.action.performed += Gripping;
            gripAction.action.canceled += GripRelease;
        }

        if (pinchAction != null && pinchAction.action != null)
        {
            pinchAction.action.Enable();
            pinchAction.action.performed += Pinching;
            pinchAction.action.canceled += PinchRelease;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe and disable actions
        if (gripAction != null && gripAction.action != null)
        {
            gripAction.action.performed -= Gripping;
            gripAction.action.canceled -= GripRelease;
            gripAction.action.Disable();
        }

        if (pinchAction != null && pinchAction.action != null)
        {
            pinchAction.action.performed -= Pinching;
            pinchAction.action.canceled -= PinchRelease;
            pinchAction.action.Disable();
        }
    }

    private void Gripping(InputAction.CallbackContext obj) => animator.SetFloat("Grip", obj.ReadValue<float>());

    private void GripRelease(InputAction.CallbackContext obj) => animator.SetFloat("Grip", 0f);

    private void Pinching(InputAction.CallbackContext obj) => animator.SetFloat("Pinch", obj.ReadValue<float>());

    private void PinchRelease(InputAction.CallbackContext obj) => animator.SetFloat("Pinch", 0f);
}