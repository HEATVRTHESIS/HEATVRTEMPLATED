
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the highlighting for a specific task prefab.
/// Attach this script to the root of your prefab.
/// </summary>
public class TaskController : MonoBehaviour
{
    // An array to hold references to the highlightable objects within this prefab.
    private HighlightableObject[] taskTargets;

    void Awake()
    {
        // Automatically find all HighlightableObject components in the children.
        taskTargets = GetComponentsInChildren<HighlightableObject>();
    }

    /// <summary>
    /// Starts the highlighting for this task.
    /// </summary>
    public void StartTask()
    {
        foreach (var target in taskTargets)
        {
            if (target != null)
            {
                target.SetHighlight(true);
            }
        }
    }

    /// <summary>
    /// Ends the highlighting for this task.
    /// </summary>
    public void EndTask()
    {
        foreach (var target in taskTargets)
        {
            if (target != null)
            {
                target.SetHighlight(false);
            }
        }
    }
}