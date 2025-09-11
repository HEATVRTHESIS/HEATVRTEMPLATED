using UnityEngine;


public class StorageContainer : MonoBehaviour
{
    public string containerType;
    public PopupManager popupManager;
    public Transform snapPoint;

    // This method is called by the XR Grab Interactable's OnSelectExited event
    public void HandleItemDropped(UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable droppedInteractable)
    {
        // Get the StorableItem component from the dropped object
        StorableItem storableItem = droppedInteractable.GetComponent<StorableItem>();

        // Check if the dropped object is a StorableItem and if its type matches the container
        if (storableItem != null && storableItem.storableType == containerType)
        {
            // Correct item has been dropped.
            popupManager.ShowMessage("Correct! Item has been stored.");

            // 🌟 CRITICAL FIX: Tell the StorableItem that it has been correctly stored.
            // This is the call that updates your TaskController and task list UI.
            storableItem.OnCorrectlyStored();

            // Snap the item to the position of the designated snapPoint
            if (snapPoint != null)
            {
                droppedInteractable.transform.position = snapPoint.position;
                droppedInteractable.transform.rotation = snapPoint.rotation;
            }
            else
            {
                Debug.LogWarning("Snap point is not assigned! Item will be snapped to the container's origin.");
                droppedInteractable.transform.position = transform.position;
            }

            // After snapping, disable the item's physics and collider
            Rigidbody rb = droppedInteractable.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }
            
            Collider col = droppedInteractable.GetComponent<Collider>();
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