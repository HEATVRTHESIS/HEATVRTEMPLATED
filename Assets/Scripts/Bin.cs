using UnityEngine;
using System.Collections; // Required for Coroutines

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

    // Public variable to set the bin type in the Inspector
    public BinType binType;

    // A reference to the PopupManager script to show feedback messages
    public PopupManager popupManager;

    // Called when another collider enters this object's trigger collider
    private void OnTriggerEnter(Collider other)
    {
        // Check if the entering object has the TrashItem script
        TrashItem trashItem = other.GetComponent<TrashItem>();

        // If it's a piece of trash
        if (trashItem != null)
        {
            // Check if the trash type matches the bin type
            if ((int)trashItem.trashType == (int)binType)
            {
                // Correct trash type
                // You can add effects here like particle systems or sounds
                popupManager.ShowMessage("Correct! Well done.");

                // Destroy the trash object
                Destroy(other.gameObject);
            }
            else
            {
                // Incorrect trash type
                popupManager.ShowMessage("Wrong bin! That doesn't go there.");

                // To make the bin "reject" the trash, you could add code here
                // to push the object away or simply do nothing, allowing the user
                // to pick it up again.
            }
        }
    }
}
