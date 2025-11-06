using UnityEngine;


/// <summary>
/// Place this script on any object you want to be investigatable.
/// This object MUST also have an XR Interactable component (like XR Simple Interactable).
/// </summary>
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable))]
public class InvestigatableObject : MonoBehaviour
{
    [Header("Object Details")]
    public string objectTitle = "New Object";
    
    [TextArea(3, 10)]
    public string objectDescription = "This is a default description. Fill this in.";
}