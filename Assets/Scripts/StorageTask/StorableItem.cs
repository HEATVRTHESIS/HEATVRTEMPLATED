using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class StorableItem : MonoBehaviour
{
    public string storableType;
    
    [HideInInspector]
    public StorageTaskController parentTaskController;
    
    [HideInInspector]
    public bool hasBeenProcessed = false;
    
    [HideInInspector]
    public bool hasTriggeredError = false;
    
    public void OnCorrectlyStored()
    {
        if (hasBeenProcessed) return;
        
        hasBeenProcessed = true;
        
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