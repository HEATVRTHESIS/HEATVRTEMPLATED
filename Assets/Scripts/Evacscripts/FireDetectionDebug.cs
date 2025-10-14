using UnityEngine;
using Ignis;

public class FireDetectionDebug : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[FireDebug] Trigger ENTER with: {other.gameObject.name}");
        Component[] components = other.GetComponents<Component>();
        string componentNames = "";
        foreach (Component comp in components)
        {
            componentNames += comp.GetType().Name + ", ";
        }
        Debug.Log($"[FireDebug] Other has components: {componentNames}");
        
        // Check for FlammableObject
        FlammableObject flammable = other.GetComponent<FlammableObject>();
        if (flammable != null)
        {
            Debug.Log($"[FireDebug] Found FlammableObject! OnFire: {flammable.onFire}");
        }
        
        // Check for FireTrigger
        FireTrigger fireTrigger = other.GetComponent<FireTrigger>();
        if (fireTrigger != null)
        {
            Debug.Log($"[FireDebug] Found FireTrigger! FlameObj: {fireTrigger.flameObj?.gameObject.name ?? "NULL"}");
            if (fireTrigger.flameObj != null)
            {
                Debug.Log($"[FireDebug] FireTrigger's FlammableObject OnFire: {fireTrigger.flameObj.onFire}");
            }
        }
        
        // Check parent
        if (other.transform.parent != null)
        {
            Debug.Log($"[FireDebug] Parent: {other.transform.parent.name}");
            FlammableObject parentFlammable = other.transform.parent.GetComponent<FlammableObject>();
            if (parentFlammable != null)
            {
                Debug.Log($"[FireDebug] Found FlammableObject on PARENT! OnFire: {parentFlammable.onFire}");
            }
        }
    }
    
    void OnTriggerStay(Collider other)
    {
        Debug.Log($"[FireDebug] Trigger STAY with: {other.gameObject.name}");
    }
    
    void OnTriggerExit(Collider other)
    {
        Debug.Log($"[FireDebug] Trigger EXIT with: {other.gameObject.name}");
    }
    
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"[FireDebug] COLLISION ENTER (not trigger!) with: {collision.gameObject.name}");
    }
}