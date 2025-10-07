using UnityEngine;

public class ParticleDebugTest : MonoBehaviour
{
    void OnParticleCollision(GameObject other)
    {
        Debug.Log("PARTICLE HIT DETECTED! Hit object: " + gameObject.name + " | From: " + other.name);
        
        ParticleSystem ps = other.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            Debug.Log("Particle System found: " + ps.name);
            
            // Get collision events
            int numCollisionEvents = ps.GetCollisionEvents(gameObject, new ParticleCollisionEvent[16]);
            Debug.Log("Number of collision events: " + numCollisionEvents);
        }
        else
        {
            Debug.LogWarning("No Particle System component found on: " + other.name);
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Regular collision detected with: " + collision.gameObject.name);
    }
}