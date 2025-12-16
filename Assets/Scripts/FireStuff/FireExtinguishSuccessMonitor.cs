using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Reflection;

/// <summary>
/// Monitors child flammable objects and notifies when all fires are extinguished.
/// Put this script on a parent empty GameObject that contains flammable objects as children.
/// Each FireExtinguisherController should have its own dedicated FireExtinguishSuccessMonitor.
/// </summary>
public class FireExtinguishSuccessMonitor : MonoBehaviour
{
    [Header("Success Settings")]
    [Tooltip("Sound to play when all fires are successfully extinguished")]
    public AudioClip successSound;
    
    [Tooltip("Audio source to play the success sound (optional - will create one if not assigned)")]
    public AudioSource audioSource;
    
    [Header("Task Controller Integration")]
    [Tooltip("Fire extinguisher controller to notify when task is complete")]
    public FireExtinguisherController fireExtinguisherController;
    
    [Header("Success Events")]
    [Tooltip("Unity event triggered when all fires are extinguished")]
    public UnityEvent OnAllFiresExtinguishedEvent;
    
    [Header("Monitoring Settings")]
    [Tooltip("How often to check fire status (seconds)")]
    public float checkInterval = 0.5f;
    
    [Tooltip("Delay before checking for success after a fire is extinguished (prevents false positives)")]
    public float successDelay = 1f;

    // Private variables
    private List<MonoBehaviour> childFlammables = new List<MonoBehaviour>();
    private bool wasAnyObjectOnFire = false;
    private bool successTriggered = false;
    private float lastExtinguishTime = 0f;

    void Start()
    {
        // Find all flammable objects in children
        FindChildFlammables();
        
        // Set up audio source if not assigned
        if (audioSource == null && successSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        // Validate fire extinguisher controller assignment
        if (fireExtinguisherController == null)
        {
            Debug.LogError($"FireExtinguishSuccessMonitor on {gameObject.name}: No FireExtinguisherController assigned! " +
                          "You must manually assign the controller in the inspector.");
        }
        else
        {
            // Verify the controller references this monitor
            if (fireExtinguisherController.successMonitor != this)
            {
                Debug.LogWarning($"FireExtinguishSuccessMonitor on {gameObject.name}: The assigned controller doesn't reference this monitor back!");
            }
        }
        
        // Start monitoring
        InvokeRepeating(nameof(CheckFireStatus), checkInterval, checkInterval);
        
        Debug.Log($"Fire Monitor on {gameObject.name} initialized. Found {childFlammables.Count} flammable objects.");
    }

    /// <summary>
    /// Finds all FlammableObject components in child objects
    /// </summary>
    void FindChildFlammables()
    {
        childFlammables.Clear();
        
        // Find all MonoBehaviour components and filter for FlammableObject
        MonoBehaviour[] allBehaviours = GetComponentsInChildren<MonoBehaviour>();
        
        foreach (var behaviour in allBehaviours)
        {
            // Check if this component is a FlammableObject by its type name
            if (behaviour != null && behaviour.GetType().Name == "FlammableObject")
            {
                // Don't include the parent object itself
                if (behaviour.gameObject != gameObject)
                {
                    childFlammables.Add(behaviour);
                    Debug.Log($"Fire Monitor on {gameObject.name}: Found flammable object: {behaviour.gameObject.name}");
                }
            }
        }
        
        if (childFlammables.Count == 0)
        {
            Debug.LogWarning($"Fire Monitor on {gameObject.name}: No flammable objects found in children! " +
                           "Make sure FlammableObject components are on child GameObjects.");
        }
    }

    /// <summary>
    /// Checks the fire status of all child flammable objects
    /// </summary>
    void CheckFireStatus()
    {
        if (childFlammables.Count == 0)
        {
            return;
        }

        // If already triggered, don't check again
        if (successTriggered)
        {
            return;
        }

        bool anyObjectOnFire = false;
        int onFireCount = 0;

        // Check each flammable object's fire status
        foreach (var flammable in childFlammables)
        {
            if (flammable != null)
            {
                bool isOnFire = IsObjectOnFire(flammable);
                
                if (isOnFire)
                {
                    anyObjectOnFire = true;
                    onFireCount++;
                }
            }
        }

        // Track if any object was on fire at some point
        if (anyObjectOnFire)
        {
            wasAnyObjectOnFire = true;
        }

        // Check for success condition
        if (wasAnyObjectOnFire && !anyObjectOnFire && !successTriggered)
        {
            Debug.Log($"Fire Monitor on {gameObject.name}: Success conditions met! All {childFlammables.Count} fires extinguished.");
            TriggerSuccess();
        }
    }

    /// <summary>
    /// Checks if a specific FlammableObject is currently on fire
    /// Uses the direct onFire boolean field from the Ignis FlammableObject
    /// </summary>
    bool IsObjectOnFire(MonoBehaviour flammable)
    {
        if (flammable == null) return false;

        try
        {
            var type = flammable.GetType();
            
            // Primary method: Check the onFire boolean field directly
            var onFireField = type.GetField("onFire", BindingFlags.Public | BindingFlags.Instance);
            if (onFireField != null && onFireField.FieldType == typeof(bool))
            {
                bool isOnFire = (bool)onFireField.GetValue(flammable);
                return isOnFire;
            }

            // Fallback method: Check onFireTimer (should be > 0 when burning)
            var onFireTimerField = type.GetField("onFireTimer", BindingFlags.Public | BindingFlags.Instance);
            if (onFireTimerField != null && onFireTimerField.FieldType == typeof(float))
            {
                float onFireTimer = (float)onFireTimerField.GetValue(flammable);
                return onFireTimer > 0f;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Fire Monitor on {gameObject.name}: Reflection failed for {flammable.name}: {e.Message}");
        }

        return false;
    }

    /// <summary>
    /// Triggers the success sequence
    /// </summary>
    void TriggerSuccess()
    {
        if (successTriggered) return;
        
        successTriggered = true;
        
        // Play success sound
        if (audioSource != null && successSound != null)
        {
            audioSource.clip = successSound;
            audioSource.Play();
            Debug.Log($"Fire Monitor on {gameObject.name}: Success sound played.");
        }

        // Trigger success events
        OnAllFiresExtinguished();
    }

    /// <summary>
    /// Called when all fires are successfully extinguished
    /// Override this or add UnityEvents if you want additional success behaviors
    /// </summary>
    protected virtual void OnAllFiresExtinguished()
    {
        Debug.Log($"Fire Monitor on {gameObject.name}: Fire extinguishing task completed successfully!");
        
        // Notify the fire extinguisher controller
        if (fireExtinguisherController != null)
        {
            fireExtinguisherController.OnAllFiresExtinguished();
        }
        else
        {
            Debug.LogError($"Fire Monitor on {gameObject.name}: No FireExtinguisherController assigned to notify of completion!");
        }
        
        // Trigger Unity Event
        OnAllFiresExtinguishedEvent?.Invoke();
    }

    /// <summary>
    /// Manually reset the monitor (useful for restarting scenarios)
    /// </summary>
    public void ResetMonitor()
    {
        wasAnyObjectOnFire = false;
        successTriggered = false;
        lastExtinguishTime = 0f;
        
        // Re-find child flammables in case the hierarchy changed
        FindChildFlammables();
        
        Debug.Log($"Fire Monitor on {gameObject.name} reset.");
    }

    /// <summary>
    /// Check if all fires are currently extinguished
    /// </summary>
    public bool AreAllFiresExtinguished()
    {
        if (childFlammables.Count == 0) return false;
        
        foreach (var flammable in childFlammables)
        {
            if (flammable != null && IsObjectOnFire(flammable))
            {
                return false;
            }
        }
        
        return wasAnyObjectOnFire; // Only true if there were fires at some point
    }

    /// <summary>
    /// Get the current status of the fire extinguishing task
    /// </summary>
    public string GetCurrentStatus()
    {
        if (childFlammables.Count == 0)
            return "No flammable objects found";
            
        if (!wasAnyObjectOnFire)
            return "Waiting for fires to start...";
            
        if (successTriggered)
            return "All fires extinguished - Task completed!";
            
        int fireCount = 0;
        foreach (var flammable in childFlammables)
        {
            if (flammable != null && IsObjectOnFire(flammable))
                fireCount++;
        }
        
        return $"{fireCount} fires still burning";
    }

    /// <summary>
    /// Manual test method - call this from inspector or another script
    /// </summary>
    [ContextMenu("Test Success Sound")]
    public void TestSuccessSound()
    {
        Debug.Log($"Testing success sound on {gameObject.name} manually...");
        wasAnyObjectOnFire = true; // Simulate that there was fire
        successTriggered = false; // Reset flag
        TriggerSuccess();
    }

    /// <summary>
    /// Force complete the task (for testing)
    /// </summary>
    [ContextMenu("Force Complete Task")]
    public void ForceCompleteTask()
    {
        Debug.Log($"Forcing task completion on {gameObject.name}...");
        wasAnyObjectOnFire = true;
        successTriggered = false;
        TriggerSuccess();
    }

    /// <summary>
    /// Force check current status - useful for debugging
    /// </summary>
    [ContextMenu("Debug Current Status")]
    public void DebugCurrentStatus()
    {
        Debug.Log($"=== FIRE MONITOR DEBUG ({gameObject.name}) ===");
        Debug.Log($"Child Flammables Found: {childFlammables.Count}");
        
        foreach (var flammable in childFlammables)
        {
            if (flammable != null)
            {
                Debug.Log($"Flammable Object: {flammable.name}");
                Debug.Log($"  - Type: {flammable.GetType().Name}");
                Debug.Log($"  - On Fire: {IsObjectOnFire(flammable)}");
            }
        }
        
        Debug.Log($"Was Any On Fire: {wasAnyObjectOnFire}");
        Debug.Log($"Success Triggered: {successTriggered}");
        Debug.Log($"Fire Controller Assigned: {fireExtinguisherController != null}");
        if (fireExtinguisherController != null)
        {
            Debug.Log($"Fire Controller Name: {fireExtinguisherController.gameObject.name}");
        }
        Debug.Log($"Current Status: {GetCurrentStatus()}");
        Debug.Log("=== END DEBUG ===");
    }

    void OnDestroy()
    {
        CancelInvoke();
    }

    // Optional: Draw debug information in scene view
    void OnDrawGizmosSelected()
    {
        if (childFlammables != null)
        {
            foreach (var flammable in childFlammables)
            {
                if (flammable != null)
                {
                    Gizmos.color = IsObjectOnFire(flammable) ? Color.red : Color.green;
                    Gizmos.DrawWireCube(flammable.transform.position, Vector3.one * 0.5f);
                }
            }
        }
        
        // Draw connection to fire extinguisher controller
        if (fireExtinguisherController != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, fireExtinguisherController.transform.position);
        }
        else
        {
            // Warning if no controller assigned
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.3f);
        }
        
        // Show overall status
        Gizmos.color = successTriggered ? Color.green : (wasAnyObjectOnFire ? Color.yellow : Color.gray);
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}