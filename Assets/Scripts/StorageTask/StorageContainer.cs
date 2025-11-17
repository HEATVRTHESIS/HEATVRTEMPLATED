using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class StorageContainer : MonoBehaviour
{
    public string containerType;
    public PopupManager popupManager;
    public Transform[] snapPoints; // Use an array for multiple snap points
    
    private int nextSnapPointIndex = 0; // Tracks the next available slot
    
    private void OnTriggerEnter(Collider other)
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

    private void OnTriggerExit(Collider other)
    {
        StorableItem storableItem = other.GetComponent<StorableItem>();
        
        // Reset the hasBeenProcessed flag when the item leaves the container
        // This allows the same item to be re-evaluated if dropped in the container again
        if (storableItem != null)
        {
            // Only reset if it was a wrong item (not correctly stored)
            // Correctly stored items should stay processed
            if (storableItem.storableType != containerType)
            {
                storableItem.hasBeenProcessed = false;
            }
        }
    }
    
    private void ProcessDroppedItem(StorableItem storableItem, GameObject droppedObject)
    {
        // Mark as processed immediately to prevent repeated triggers
        storableItem.hasBeenProcessed = true;

        // Check if the dropped object's type matches the container
        if (storableItem.storableType == containerType)
        {
            // Check if there are still available snap points
            if (nextSnapPointIndex < snapPoints.Length)
            {
                // Correct item has been dropped.
                popupManager.ShowMessage("Correct! Item stored.");

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
            
            // Note: hasBeenProcessed is already set to true at the start of this method
            // It will be reset to false when the item exits the trigger (OnTriggerExit)
        }
    }
}