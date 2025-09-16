using UnityEngine;
using UnityEngine.Events;

public class PullDownTrigger : MonoBehaviour
{
    // The Hinge Joint component on this GameObject.
    private HingeJoint hingeJoint; 

    // This value should be set to your Hinge Joint's Max Angle.
    public float successAngle = 92f; 

    // The event to be triggered when the pull is successful.
    public UnityEvent onPullSuccess;

    private bool _isPulled = false;

    private void Start()
    {
        // Get the Hinge Joint component from this GameObject.
        hingeJoint = GetComponent<HingeJoint>();

        // Ensure the hinge joint exists.
        if (hingeJoint == null)
        {
            Debug.LogError("Hinge Joint not found on this GameObject. The script will not work.");
        }
    }

    private void Update()
    {
        // Check if the task hasn't been completed yet.
        if (!_isPulled)
        {
            // Check if the current angle has reached the success threshold.
            if (hingeJoint.angle >= successAngle - 5.0f) // Subtract a small tolerance to account for slight variations.
            {
                // Trigger the success event and set the flag to prevent repeated triggers.
                onPullSuccess?.Invoke();
                _isPulled = true;
            }
        }
    }
}