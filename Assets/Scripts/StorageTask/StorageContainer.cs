using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class StorageContainer : MonoBehaviour
{
    public string containerType;
    public PopupManager popupManager;
    public Transform snapPoint;
    
    // REMOVE the HandleItemDropped method completely!
    // Use trigger detection instead:
    
    private void OnTriggerStay(Collider other)
    {
        StorableItem storableItem = other.GetComponent<StorableItem>();
        
        if (storableItem != null && !storableItem.hasBeenProcessed)
        {
            XRGrabInteractable grabInteractable = other.GetComponent<XRGrabInteractable>();
            
            // Only process when item is released inside the trigger zone
            if (grabInteractable != null && !grabInteractable.isSelected)
            {
                ProcessDroppedItem(storableItem, other.gameObject);
            }
        }
    }
    
    private void ProcessDroppedItem(StorableItem storableItem, GameObject droppedObject)
    {
        // Check if the dropped object's type matches the container
        if (storableItem.storableType == containerType)
        {
            // Correct item has been dropped.
            popupManager.ShowMessage("Correct! Item has been stored.");
            
            // Tell the StorableItem that it has been correctly stored.
            storableItem.OnCorrectlyStored();
            
            // Snap the item to the position of the designated snapPoint
            if (snapPoint != null)
            {
                droppedObject.transform.position = snapPoint.position;
                droppedObject.transform.rotation = snapPoint.rotation;
            }
            else
            {
                Debug.LogWarning("Snap point is not assigned! Item will be snapped to the container's origin.");
                droppedObject.transform.position = transform.position;
            }
            
            // After snapping, disable the item's physics and collider
            Rigidbody rb = droppedObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }
           
            Collider col = droppedObject.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }
        }
        else
        {
            // Wrong item has been dropped in the container's zone.
            popupManager.ShowMessage($"Wrong container! That doesn't go in the '{containerType}' box.");
        }
    }
}