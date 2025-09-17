using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// This script defines a storable item. It should be attached to each
/// individual object that needs to be stored.
/// </summary>
public class StorableItem : MonoBehaviour
{
    // The type of storable item this is, must match the StorageContainer type.
    public string storableType;
    // A reference to the parent StorageTaskController, set by the TaskController itself.
    [HideInInspector]
    public StorageTaskController parentTaskController;
    
    // Flag to prevent multiple processing of the same item
    [HideInInspector]
    public bool hasBeenProcessed = false;
    
    /// <summary>
    /// This method is called by the StorageContainer script when the item is placed in the correct container.
    /// </summary>
    public void OnCorrectlyStored()
    {
        // Prevent duplicate processing
        if (hasBeenProcessed) return;
        
        hasBeenProcessed = true;
        
        // This is the crucial part. It tells the parent task to increment the counter.
        if (parentTaskController != null)
        {
            parentTaskController.ItemStored();
        }
        else
        {
            Debug.LogError("StorableItem has no parent StorageTaskController assigned. The task counter will not be updated.");
        }
    }
}