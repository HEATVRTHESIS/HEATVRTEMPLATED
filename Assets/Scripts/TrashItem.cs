
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// This script defines a piece of trash. It should be attached to each
/// individual trash object that needs to be disposed of.
/// </summary>
public class TrashItem : MonoBehaviour
{
    // The type of trash this is, must match the BinType enum.
    public enum TrashType
    {
        Red,
        Green,
        Black,
        YellowWithBlackBand,
        Yellow
    }
    public TrashType trashType;

    // A reference to the parent TaskController, set by the TaskController itself.
    [HideInInspector]
    public TaskController parentTaskController;

    /// <summary>
    /// This method is called by the Bin script when the trash is placed in the correct bin.
    /// </summary>
    public void OnCorrectlyDisposed()
    {
        // This is the crucial part. It tells the parent task to increment the counter.
        if (parentTaskController != null)
        {
            parentTaskController.ItemDisposedOf();
        }
        else
        {
            Debug.LogError("TrashItem has no parent TaskController assigned. The task counter will not be updated.");
        }
    }
}