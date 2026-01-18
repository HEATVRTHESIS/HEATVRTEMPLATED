using UnityEngine;
using System.Collections;

public class Bin : MonoBehaviour
{
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
                trashItem.OnCorrectlyDisposed();
                Destroy(other.gameObject);
            }
            else
            {
                popupManager.ShowMessage("Wrong bin! That doesn't go there.");
                ScoreTracker.Instance.OnTaskError();

                // Track the error
                if (ErrorTracker.Instance != null)
                    ErrorTracker.Instance.RecordDisposalError();

                // Call the TRASH ITEM's parent task controller, not the bin's
                if (trashItem.parentTaskController != null)
                    trashItem.parentTaskController.OnDisposalError(trashItem.trashType, binType);
            }
        }
    }
}