
using UnityEngine;
using System.Collections;

public class Bin : MonoBehaviour
{
    // Public enum to define the types of bins, must match the TrashItem enum
    public enum BinType
    {
        Red,
        Green,
        Black,
        YellowWithBlackBand,
        Yellow
    }

    public BinType binType;
    public PopupManager popupManager;

    private void OnTriggerEnter(Collider other)
    {
        TrashItem trashItem = other.GetComponent<TrashItem>();

        if (trashItem != null)
        {
            if ((int)trashItem.trashType == (int)binType)
            {
                popupManager.ShowMessage("Correct! Well done.");

                // Notify the TrashItem that it was correctly disposed of.
                trashItem.OnCorrectlyDisposed();

                // Destroy the trash object
                Destroy(other.gameObject);
            }
            else
            {
                popupManager.ShowMessage("Wrong bin! That doesn't go there.");
                 ScoreTracker.Instance.OnTaskError();
            }
        }
    }
}