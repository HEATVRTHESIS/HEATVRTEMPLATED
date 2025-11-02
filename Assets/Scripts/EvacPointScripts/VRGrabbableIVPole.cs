using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
public class VRGrabbableIVPole : MonoBehaviour
{
    [Header("Rigidbody Settings")]
    [Tooltip("Mass of the IV pole in kg")]
    public float mass = 5f;
    [Tooltip("Drag to apply when not grabbed (simulates friction)")]
    public float baseDrag = 2f;
    [Tooltip("Angular drag (rotational friction)")]
    public float baseAngularDrag = 0.5f;

    [Header("Upright Stabilization")]
    [Tooltip("Force strength to keep pole upright")]
    public float uprightForce = 200f;
    [Tooltip("Damping for upright force")]
    public float uprightDamping = 30f;
    [Tooltip("(Not currently used) Lock rotation on X axis")]
    public bool lockYRotation = true;

    [Header("Wheel Settings")]
    [Tooltip("Assign all wheel transforms that should rotate")]
    public Transform[] wheels;
    [Tooltip("Wheel radius in meters (for calculating rotation speed)")]
    public float wheelRadius = 0.05f;
    [Tooltip("Extra wheel rotation multiplier for visual effect")]
    public float wheelRotationMultiplier = 1.0f;

    [Header("Movement Settings")]
    [Tooltip("Maximum velocity the pole can reach")]
    public float maxVelocity = 5f;
    [Tooltip("Drag multiplier when grabbed (lower = easier to push)")]
    public float grabbedDragMultiplier = 0.3f;

    [Header("Audio (Optional)")]
    [Tooltip("AudioSource for rolling sounds")]
    public AudioSource rollingAudioSource;
    [Tooltip("Minimum velocity to start rolling sound")]
    public float rollingAudioThreshold = 0.5f;
    [Tooltip("Volume multiplier based on velocity")]
    public float rollingAudioVolumeMultiplier = 0.3f;

    [Header("Ground Constraint")]
    [Tooltip("Keep pole on ground (prevents lifting)")]
    public bool constrainToGround = true;

    private Rigidbody rb;
    private XRGrabInteractable grabInteractable;
    private Vector3 previousPosition;
    private bool isGrabbed = false;
    private float[] wheelRotations; // Track individual wheel rotations

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();

        // Initialize wheel rotations array
        if (wheels != null && wheels.Length > 0)
        {
            wheelRotations = new float[wheels.Length];
        }
    }

    void Start()
    {
        // Configure Rigidbody
        rb.mass = mass;
        rb.drag = baseDrag;
        rb.angularDrag = baseAngularDrag;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.isKinematic = false;
        
        // Stop any initial rotation/movement
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // If constraining to ground, freeze Y position from the start
        if (constrainToGround)
        {
            rb.constraints = RigidbodyConstraints.FreezePositionY;
        }

        // Subscribe to grab events
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);

        previousPosition = transform.position;

        // Setup audio if present
        if (rollingAudioSource != null)
        {
            rollingAudioSource.loop = true;
            rollingAudioSource.playOnAwake = false;
            rollingAudioSource.volume = 0f;
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        
        // Reduce drag when grabbed to make it easier to push
        rb.drag = baseDrag * grabbedDragMultiplier;
        
        // Always freeze Y position to prevent lifting
        if (constrainToGround)
        {
            rb.constraints = RigidbodyConstraints.FreezePositionY;
        }
        
        Debug.Log("IV Pole grabbed");
    }

    void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed = false;
        
        // Restore normal drag
        rb.drag = baseDrag;
        
        // Keep Y position frozen if constraining to ground
        if (constrainToGround)
        {
            rb.constraints = RigidbodyConstraints.FreezePositionY;
        }
        
        Debug.Log("IV Pole released");
    }

    void FixedUpdate()
    {
        // Constrain to ground if enabled
        if (constrainToGround)
        {
            ConstrainToGround();
        }

        // Apply upright force to keep pole standing
        ApplyUprightForce();

        // Limit velocity
        if (rb.velocity.magnitude > maxVelocity)
        {
            rb.velocity = rb.velocity.normalized * maxVelocity;
        }

        // Calculate movement for wheel rotation
        Vector3 movement = transform.position - previousPosition;
        float movementDistance = movement.magnitude;

        // Rotate wheels based on movement
        RotateWheels(movementDistance);

        // Handle rolling audio
        UpdateRollingAudio();

        previousPosition = transform.position;
    }

    /// <summary>
    /// Keeps the pole constrained to the ground (no lifting)
    /// </summary>
    void ConstrainToGround()
    {
        // Y position is already frozen by constraints
        // Just ensure no upward velocity (safety measure)
        Vector3 velocity = rb.velocity;
        if (velocity.y > 0f)
        {
            velocity.y = 0f;
            rb.velocity = velocity;
        }
    }

    /// <summary>
    /// Applies force to keep the pole upright at X=-90 rotation
    /// </summary>
    void ApplyUprightForce()
    {
        // Get current rotation
        Quaternion currentRotation = transform.rotation;
        
        // Target rotation: X=-90, keep Y rotation for spinning, Z=0
        Quaternion targetRotation = Quaternion.Euler(-90f, currentRotation.eulerAngles.y, 0f);
        
        // Calculate difference
        Quaternion rotationDifference = targetRotation * Quaternion.Inverse(currentRotation);
        
        // Convert to angle-axis
        rotationDifference.ToAngleAxis(out float angle, out Vector3 axis);
        
        // Normalize angle to [-180, 180]
        if (angle > 180f)
            angle -= 360f;
        
        // Apply torque to correct tilt
        if (angle != 0f && axis != Vector3.zero)
        {
            Vector3 torque = axis.normalized * (angle * Mathf.Deg2Rad) * uprightForce;
            rb.AddTorque(torque - rb.angularVelocity * uprightDamping);
        }
    }

    /// <summary>
    /// Rotates wheel transforms based on movement
    /// </summary>
    void RotateWheels(float distance)
    {
        if (wheels == null || wheels.Length == 0 || wheelRadius <= 0f)
            return;

        // Calculate rotation angle based on distance traveled
        // circumference = 2 * π * radius
        // rotation (degrees) = (distance / circumference) * 360
        float rotationDegrees = (distance / (2f * Mathf.PI * wheelRadius)) * 360f * wheelRotationMultiplier;

        // Apply rotation to each wheel
        for (int i = 0; i < wheels.Length; i++)
        {
            if (wheels[i] != null)
            {
                wheelRotations[i] += rotationDegrees;
                wheels[i].localRotation = Quaternion.Euler(wheelRotations[i], 0f, 0f);
            }
        }
    }

    /// <summary>
    /// Updates rolling audio based on velocity
    /// </summary>
    void UpdateRollingAudio()
    {
        if (rollingAudioSource == null)
            return;

        float velocity = rb.velocity.magnitude;

        if (velocity > rollingAudioThreshold)
        {
            if (!rollingAudioSource.isPlaying)
            {
                rollingAudioSource.Play();
            }

            // Adjust volume based on velocity
            float targetVolume = Mathf.Clamp01(velocity * rollingAudioVolumeMultiplier);
            rollingAudioSource.volume = Mathf.Lerp(rollingAudioSource.volume, targetVolume, Time.deltaTime * 5f);
        }
        else
        {
            if (rollingAudioSource.isPlaying)
            {
                // Fade out
                rollingAudioSource.volume = Mathf.Lerp(rollingAudioSource.volume, 0f, Time.deltaTime * 5f);
                
                if (rollingAudioSource.volume < 0.01f)
                {
                    rollingAudioSource.Stop();
                    rollingAudioSource.volume = 0f;
                }
            }
        }
    }

    /// <summary>
    /// Get current velocity of the pole
    /// </summary>
    public float GetVelocity()
    {
        return rb != null ? rb.velocity.magnitude : 0f;
    }

    /// <summary>
    /// Check if pole is currently grabbed
    /// </summary>
    public bool IsGrabbed()
    {
        return isGrabbed;
    }

    /// <summary>
    /// Apply force to the pole (useful for external interactions)
    /// </summary>
    public void AddForce(Vector3 force, ForceMode mode = ForceMode.Force)
    {
        if (rb != null)
        {
            rb.AddForce(force, mode);
        }
    }

    /// <summary>
    /// Reset the pole velocities (let upright force handle orientation naturally)
    /// </summary>
    public void ResetToUpright()
    {
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw upright direction
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2f);

        // Draw velocity direction if in play mode
        if (Application.isPlaying && rb != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + rb.velocity);
        }
    }
}