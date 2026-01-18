using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class StorageContainer : MonoBehaviour
{
    public string containerType;
    public PopupManager popupManager;
    public Transform[] snapPoints;
    
    private int nextSnapPointIndex = 0;
    
    private void OnTriggerStay(Collider other)
    {
        StorableItem storableItem = other.GetComponent<StorableItem>();
        
        if (storableItem != null && !storableItem.hasBeenProcessed)
        {
            XRGrabInteractable grabInteractable = other.GetComponent<XRGrabInteractable>();
            
            if (grabInteractable != null && !grabInteractable.isSelected)
            {
                ProcessDroppedItem(storableItem, other.gameObject);
            }
        }
    }
    
    private void ProcessDroppedItem(StorableItem storableItem, GameObject droppedObject)
    {
        if (storableItem.storableType == containerType)
        {
            if (nextSnapPointIndex < snapPoints.Length)
            {
                popupManager.ShowMessage("Correct! Item stored.");
                storableItem.OnCorrectlyStored();

                Transform currentSnapPoint = snapPoints[nextSnapPointIndex];
                droppedObject.transform.position = currentSnapPoint.position;
                droppedObject.transform.rotation = currentSnapPoint.rotation;

                Rigidbody rb = droppedObject.GetComponent<Rigidbody>();
                if (rb != null)
                    rb.isKinematic = true;

                Collider col = droppedObject.GetComponent<Collider>();
                if (col != null)
                    col.enabled = false;

                nextSnapPointIndex++;
            }
            else
            {
                popupManager.ShowMessage("Container is full!");
            }
        }
        else
        {
            // Prevent error spam
            if (storableItem.hasTriggeredError) return;
            
            storableItem.hasTriggeredError = true;
            
            popupManager.ShowMessage($"Wrong container! That doesn't go in the '{containerType}' box.");
            ScoreTracker.Instance.OnTaskError();
            
            if (ErrorTracker.Instance != null)
                ErrorTracker.Instance.RecordStorageError();
            
            if (storableItem.parentTaskController != null)
                storableItem.parentTaskController.OnStorageError(storableItem.storableType, containerType);
        }
    }
}