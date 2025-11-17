using UnityEngine;
using Ignis;
using System.Collections.Generic;

/// <summary>
/// DEBUG VERSION - Enhanced fire extinguisher with fire class validation and extensive logging.
/// </summary>
public class FireExtinguisher : MonoBehaviour
{
    [Header("Extinguisher Type")]
    [Tooltip("What type of extinguisher is this?")]
    public FireClass extinguisherType = FireClass.ABC;
    
    [Header("Particle System")]
    [Tooltip("The particle system that represents the extinguishing liquid.")]
    public ParticleSystem extinguishingParticles;

    [Header("Extinguisher Properties")]
    [Tooltip("How much fire is extinguished with each particle hit or spherecast.")]
    public float extinguishAmount = 0.5f;

    [Tooltip("How large is the extinguishing area for each particle.")]
    public float particleExtinguishRadius = 0.1f;

    [Header("Spherecast Extinguisher Settings")]
    [Tooltip("Enable this to use the spherecast method instead of particle collisions.")]
    public bool useSpherecastMethod = false;

    [Tooltip("The radius for the extinguishing spherecast.")]
    public float spherecastRadius = 2.0f;
    
    [Header("Wrong Extinguisher Feedback")]
    [Tooltip("Popup manager for showing error messages")]
    public PopupManager popupManager;
    
    [Tooltip("Cooldown between wrong extinguisher error messages (seconds)")]
    public float errorMessageCooldown = 3f;

    // Private variables
    private Transform nozzleTransform;
    private List<ParticleCollisionEvent> collisionEvents;
    private Dictionary<GameObject, float> lastErrorTime = new Dictionary<GameObject, float>();

    void Start()
    {
        Debug.Log("===== FIRE EXTINGUISHER START =====");
        Debug.Log($"Extinguisher Type: {extinguisherType}");
        
        // Check for required components
        if (extinguishingParticles == null)
        {
            Debug.LogError("❌ FireExtinguisher: ParticleSystem is NULL!");
            return;
        }
        else
        {
            Debug.Log($"✓ ParticleSystem found: {extinguishingParticles.name}");
        }

        // Get the transform of the nozzle
        nozzleTransform = extinguishingParticles.transform;

        // Initialize the list for collision events
        collisionEvents = new List<ParticleCollisionEvent>();
        
        Debug.Log($"✓ Fire Extinguisher initialized as {extinguisherType} type");
        Debug.Log("===================================");
    }

    /// <summary>
    /// Start the extinguishing effect
    /// </summary>
    public void StartExtinguishing()
    {
        Debug.Log($"StartExtinguishing() called on {extinguisherType} extinguisher");
        if (extinguishingParticles != null && !extinguishingParticles.isPlaying)
        {
            extinguishingParticles.Play();
            Debug.Log("Particles started");
        }
    }

    /// <summary>
    /// Stop the extinguishing effect
    /// </summary>
    public void StopExtinguishing()
    {
        Debug.Log($"StopExtinguishing() called");
        if (extinguishingParticles != null && extinguishingParticles.isPlaying)
        {
            extinguishingParticles.Stop();
            Debug.Log("Particles stopped");
        }
    }

    void Update()
    {
        // Spherecast method for area-of-effect extinguishing
        if (extinguishingParticles != null && extinguishingParticles.isPlaying && useSpherecastMethod)
        {
            Collider[] hits = Physics.OverlapSphere(nozzleTransform.position, spherecastRadius, -1);
            
            if (hits.Length > 0)
            {
                Debug.Log($"Spherecast found {hits.Length} colliders");
                foreach (Collider hit in hits)
                {
                    TryExtinguishObject(hit.gameObject, hit.ClosestPointOnBounds(nozzleTransform.position));
                }
            }
        }
    }

    /// <summary>
    /// Particle collision method for precise extinguishing
    /// </summary>
    void OnParticleCollision(GameObject other)
    {
        Debug.Log("===== PARTICLE COLLISION DETECTED =====");
        Debug.Log($"Collided with: {other.name}");
        Debug.Log($"This extinguisher type: {extinguisherType}");
        
        // Get the collision events
        int numCollisionEvents = extinguishingParticles.GetCollisionEvents(other, collisionEvents);
        Debug.Log($"Number of collision events: {numCollisionEvents}");

        if (numCollisionEvents > 0)
        {
            // Use the first collision point
            Vector3 pos = collisionEvents[0].intersection;
            Debug.Log($"Collision position: {pos}");
            TryExtinguishObject(other, pos);
        }
        else
        {
            Debug.LogWarning("No collision events found!");
        }
        
        Debug.Log("======================================");
    }

    /// <summary>
    /// Attempts to extinguish a fire, checking if the extinguisher type is correct
    /// </summary>
    private void TryExtinguishObject(GameObject target, Vector3 collisionPoint)
    {
        Debug.Log($"TryExtinguishObject called for: {target.name}");
        
        // Get the FlammableObject component
        Ignis.FlammableObject flamObj = target.GetComponentInParent<Ignis.FlammableObject>();
        
        if (flamObj == null)
        {
            Debug.Log($"❌ No FlammableObject found on {target.name} or its parents");
            return;
        }
        else
        {
            Debug.Log($"✓ FlammableObject found on {flamObj.gameObject.name}");
        }
        
        // Get the fire class classifier
        FlammableMaterialClassifier classifier = flamObj.GetComponent<FlammableMaterialClassifier>();
        
        if (classifier == null)
        {
            Debug.LogWarning($"⚠️ No FlammableMaterialClassifier found on {flamObj.gameObject.name}, adding default ABC");
            // No classifier found, assume ABC class (default behavior)
            classifier = flamObj.gameObject.AddComponent<FlammableMaterialClassifier>();
            classifier.fireClass = FireClass.ABC;
        }
        else
        {
            Debug.Log($"✓ FlammableMaterialClassifier found - Fire Class: {classifier.GetFireClass()}");
        }
        
        Debug.Log($"Checking: {extinguisherType} extinguisher vs {classifier.GetFireClass()} fire");
        
        // Check if this extinguisher can put out this fire
        if (classifier.CanBeExtinguishedBy(extinguisherType))
        {
            Debug.Log("✓ CORRECT EXTINGUISHER - Extinguishing fire!");
            // Correct extinguisher type - extinguish the fire
            flamObj.IncrementalExtinguish(collisionPoint, particleExtinguishRadius, 0);
        }
        else
        {
            Debug.LogError("❌ WRONG EXTINGUISHER TYPE!");
            Debug.LogError($"Cannot use {extinguisherType} extinguisher on {classifier.GetFireClass()} fire!");
            // Wrong extinguisher type - show error and track it
            OnWrongExtinguisherUsed(target, classifier.GetFireClass());
        }
    }

    /// <summary>
    /// Called when the wrong extinguisher type is used on a fire
    /// </summary>
    private void OnWrongExtinguisherUsed(GameObject target, FireClass fireClass)
    {
        Debug.Log("===== WRONG EXTINGUISHER ERROR =====");
        Debug.Log($"Target: {target.name}");
        Debug.Log($"Fire Class: {fireClass}");
        Debug.Log($"Extinguisher Type: {extinguisherType}");
        
        // Check cooldown to avoid spamming errors
        if (lastErrorTime.ContainsKey(target))
        {
            float timeSinceLastError = Time.time - lastErrorTime[target];
            Debug.Log($"Time since last error: {timeSinceLastError}s (cooldown: {errorMessageCooldown}s)");
            
            if (timeSinceLastError < errorMessageCooldown)
            {
                Debug.Log("Still in cooldown, skipping error tracking");
                return; // Still in cooldown
            }
        }
        
        lastErrorTime[target] = Time.time;
        
        // Track the error in FireScoreTracker
        if (FireScoreTracker.Instance != null)
        {
            Debug.Log("✓ Calling FireScoreTracker.OnWrongExtinguisherType()");
            FireScoreTracker.Instance.OnWrongExtinguisherType();
        }
        else
        {
            Debug.LogError("❌ FireScoreTracker.Instance is NULL!");
        }
        
        // Show feedback message
        string errorMessage = GetErrorMessage(fireClass);
        Debug.Log($"Error Message: {errorMessage}");
        
        if (popupManager != null)
        {
            Debug.Log("✓ Showing popup message");
            popupManager.ShowMessage(errorMessage);
        }
        else
        {
            Debug.LogWarning("⚠️ PopupManager is NULL - no popup will be shown");
        }
        
        Debug.Log("====================================");
    }

    /// <summary>
    /// Get appropriate error message based on fire class
    /// </summary>
    private string GetErrorMessage(FireClass fireClass)
    {
        switch (fireClass)
        {
            case FireClass.K:
                return "Wrong extinguisher! Use a Class K extinguisher for oil/grease fires!";
            case FireClass.ABC:
                return "Wrong extinguisher! Use a Class ABC extinguisher for this fire!";
            default:
                return "Wrong extinguisher type!";
        }
    }

    /// <summary>
    /// Get the extinguisher type
    /// </summary>
    public FireClass GetExtinguisherType()
    {
        return extinguisherType;
    }

    // Visual debugging in Scene view
    void OnDrawGizmosSelected()
    {
        if (nozzleTransform != null)
        {
            // Show extinguisher type with color
            Gizmos.color = extinguisherType == FireClass.ABC ? Color.red : Color.blue;
            Gizmos.DrawWireSphere(nozzleTransform.position, 0.3f);
            
            // Show spherecast radius if enabled
            if (useSpherecastMethod)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(nozzleTransform.position, spherecastRadius);
            }
        }
    }
}