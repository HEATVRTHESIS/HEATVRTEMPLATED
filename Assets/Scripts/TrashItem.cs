using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit; // Required for XR-specific components

public class TrashItem : MonoBehaviour
{
    // Public enum to define the different types of trash
    public enum TrashType
    {
        Red,
        Green,
        Black,
        YellowWithBlackBand,
        Yellow
    }

    // Public variable to set the trash type in the Inspector
    public TrashType trashType;
}
