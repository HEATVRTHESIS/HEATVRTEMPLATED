using UnityEngine;

public class CollisionTest : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("TRIGGER DETECTED WITH: " + other.gameObject.name);
    }
    
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("COLLISION DETECTED WITH: " + collision.gameObject.name);
    }
    
    void Start()
    {
        Debug.Log("=== CollisionTest Started ===");
        Debug.Log("My name: " + gameObject.name);
        Debug.Log("My layer: " + LayerMask.LayerToName(gameObject.layer));
        
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Debug.Log("Has collider: " + col.GetType().Name);
            Debug.Log("Is Trigger: " + col.isTrigger);
        }
        else
        {
            Debug.LogError("NO COLLIDER FOUND!");
        }
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            Debug.Log("Has Rigidbody");
            Debug.Log("Is Kinematic: " + rb.isKinematic);
        }
        else
        {
            Debug.LogError("NO RIGIDBODY FOUND!");
        }
    }
    
    void Update()
    {
        // Visual indicator - draw a debug sphere
        Debug.DrawRay(transform.position, Vector3.up * 2, Color.red);
    }
}