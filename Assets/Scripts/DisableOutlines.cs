using UnityEngine;

public class DisableOutlines : MonoBehaviour
{
    void Update()
    {
        // Find and destroy all HighlightableObject components
        HighlightableObject[] highlightables = FindObjectsOfType<HighlightableObject>();
        foreach (HighlightableObject h in highlightables)
        {
            Destroy(h);
        }

        // Find and destroy all Outline components
        Outline[] outlines = FindObjectsOfType<Outline>();
        foreach (Outline o in outlines)
        {
            Destroy(o);
        }
    }
}