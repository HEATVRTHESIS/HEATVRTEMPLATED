using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class StorageContainer : MonoBehaviour
{
    public string containerType;
    public PopupManager popupManager;
    public Transform[] snapPoints; // Use an array for multiple snap points
    
    private int nextSnapPointIndex = 0; // Tracks the next available slot
    
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
            // Check if there are still available snap points
            if (nextSnapPointIndex < snapPoints.Length)
            {
                // Correct item has been dropped.
                popupManager.ShowMessage("Correct! Item has been stored.");

                // Tell the StorableItem that it has been correctly stored.
                storableItem.OnCorrectlyStored();

                // Get the current snap point from the array
                Transform currentSnapPoint = snapPoints[nextSnapPointIndex];

                // Snap the item to the position of the designated snapPoint
                droppedObject.transform.position = currentSnapPoint.position;
                droppedObject.transform.rotation = currentSnapPoint.rotation;

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

                // Increment the index to move to the next snap point
                nextSnapPointIndex++;
            }
            else
            {
                // Container is full
                popupManager.ShowMessage("Container is full!");
            }
        }
        else
        {
            // Wrong item has been dropped in the container's zone.
            popupManager.ShowMessage($"Wrong container! That doesn't go in the '{containerType}' box.");
             ScoreTracker.Instance.OnTaskError();
        }
    }
}