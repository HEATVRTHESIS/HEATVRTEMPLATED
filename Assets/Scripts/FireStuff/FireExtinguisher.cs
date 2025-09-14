using UnityEngine;
using Ignis;
using System.Collections.Generic; // Required for List<ParticleCollisionEvent>

/// <summary>
/// This script handles the fire extinguishing logic for a fire extinguisher object.
/// It integrates the core logic from the official Ignis package's extinguishing components.
/// It can operate in two modes:
/// 1. Particle System Collision: When the particles collide with a flammable object.
/// 2. Spherecast Extinguish: Extinguishing all flammable objects within a certain radius.
/// </summary>
public class FireExtinguisher : MonoBehaviour
{
    // The Particle System component that creates the extinguishing liquid.
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

    // A reference to the Transform of the fire extinguisher nozzle.
    private Transform nozzleTransform;
    
    // List to store particle collision events, which is more efficient than creating a new list each time.
    private List<ParticleCollisionEvent> collisionEvents;

    void Start()
    {
        // Check for required components.
        if (extinguishingParticles == null)
        {
            Debug.LogError("FireExtinguisher script requires a ParticleSystem to be assigned in the Inspector.");
            return;
        }

        // Get the transform of the nozzle, which is assumed to be the same as the Particle System's.
        nozzleTransform = extinguishingParticles.transform;

        // Initialize the list for collision events.
        collisionEvents = new List<ParticleCollisionEvent>();
    }

    /// <summary>
    /// This method can be called from an external script or an event to start the extinguishing effect.
    /// </summary>
    public void StartExtinguishing()
    {
        if (extinguishingParticles != null && !extinguishingParticles.isPlaying)
        {
            extinguishingParticles.Play();
        }
    }

    /// <summary>
    /// This method can be called from an external script or an event to stop the extinguishing effect.
    /// </summary>
    public void StopExtinguishing()
    {
        if (extinguishingParticles != null && extinguishingParticles.isPlaying)
        {
            extinguishingParticles.Stop();
        }
    }

    void Update()
    {
        // This Update loop now only handles the Spherecast logic, which needs to run every frame.
        if (extinguishingParticles != null && extinguishingParticles.isPlaying && useSpherecastMethod)
        {
            // This is the Spherecast method, adapted from the Ignis package's SphereExtinguish.cs.
            // It's more of an "area of effect" approach.
            // It finds all flammable objects within a sphere and extinguishes them.
            Collider[] hits = Physics.OverlapSphere(nozzleTransform.position, spherecastRadius, -1);
            
            if (hits.Length > 0)
            {
                foreach (Collider hit in hits)
                {
                    // Use GetComponentInParent to handle complex object hierarchies.
                    Ignis.FlammableObject flamObj = hit.gameObject.GetComponentInParent<Ignis.FlammableObject>();
                    if (flamObj != null)
                    {
                        flamObj.IncrementalExtinguish(hit.ClosestPointOnBounds(nozzleTransform.position), spherecastRadius, 0);
                    }
                }
            }
        }
    }

    /// <summary>
    /// This is the Particle Collision method, adapted from the Ignis package's ParticleExtinguish.cs.
    /// It is automatically called by Unity when a particle from this system collides with another object.
    /// This works best for a precise, "hose spray" effect.
    /// </summary>
    /// <param name="other">The GameObject the particle collided with.</param>
    void OnParticleCollision(GameObject other)
    {
        // Get the collision events for the current collision.
        int numCollisionEvents = extinguishingParticles.GetCollisionEvents(other, collisionEvents);

        // Check if the collided object has a FlammableObject component.
        Ignis.FlammableObject flamObj = other.GetComponentInParent<Ignis.FlammableObject>();
        
        if (flamObj != null)
        {
            for (int i = 0; i < numCollisionEvents; i++)
            {
                // Get the intersection point from the collision event.
                Vector3 pos = collisionEvents[i].intersection;
                // Use IncrementalExtinguish to reduce the flame on the object at the collision point.
                flamObj.IncrementalExtinguish(pos, particleExtinguishRadius, 0);
            }
        }
    }
}
