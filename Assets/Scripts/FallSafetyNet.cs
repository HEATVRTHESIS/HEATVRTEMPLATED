using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class FallSafetyNet : MonoBehaviour
{
    [Header("Teleport Target")]
    [Tooltip("Target X/Z position for rescued objects.")]
    public Vector2 safeXZ = Vector2.zero;

    [Tooltip("Target Y position for rescued objects.")]
    public float safeHeight = 1.5f;

    [Header("Physics")]
    [Tooltip("Reset rigidbody velocity after teleporting.")]
    public bool resetVelocity = true;

    private void Reset()
    {
        BoxCollider trigger = GetComponent<BoxCollider>();
        trigger.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null)
            return;

        // Only rescue objects that have a BoxCollider.
        BoxCollider targetBoxCollider = other.GetComponent<BoxCollider>();
        if (targetBoxCollider == null)
            return;

        Rigidbody rb = other.attachedRigidbody;
        Transform targetTransform = rb != null ? rb.transform : other.transform;

        targetTransform.position = new Vector3(safeXZ.x, safeHeight, safeXZ.y);

        if (resetVelocity && rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
