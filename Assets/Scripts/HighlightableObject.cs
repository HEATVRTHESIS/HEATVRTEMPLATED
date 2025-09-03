using UnityEngine;

/// <summary>
/// A wrapper script to control the Outline component from the QuickOutline package.
/// Attach this script to any object you want to be highlightable.
/// </summary>
public class HighlightableObject : MonoBehaviour
{
    // A public variable to set the highlight color.
    public Color highlightColor = Color.yellow;
    // A public variable to set the highlight width.
    public float highlightWidth = 5f;

    // Private reference to the Outline component from the package.
    private Outline outlineComponent;

    void Awake()
    {
        // Get the Outline component from the same GameObject.
        outlineComponent = GetComponent<Outline>();

        // Make sure the Outline component exists before trying to use it.
        if (outlineComponent == null)
        {
            Debug.LogError("The Outline script is not attached to this GameObject. Cannot highlight.");
        }
    }

    /// <summary>
    /// This method is called when the component becomes enabled.
    /// This is where we ensure the outline is configured properly.
    /// </summary>
    void OnEnable()
    {
        if (outlineComponent != null)
        {
            outlineComponent.OutlineColor = highlightColor;
            outlineComponent.OutlineWidth = highlightWidth;
            // The Outline component's own OnEnable handles turning the highlight on.
        }
    }

    /// <summary>
    /// Turns the object's highlight on or off.
    /// </summary>
    /// <param name="isHighlighted">True to turn on the highlight, false to turn it off.</param>
    public void SetHighlight(bool isHighlighted)
    {
        if (outlineComponent != null)
        {
            // The fix is here. We now directly enable or disable the Outline component.
            outlineComponent.enabled = isHighlighted;
        }
    }
}
