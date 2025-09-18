using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// Manages a fire alarm task that completes when a threshold is reached.
/// Similar to MaintenanceTaskController but for threshold-based completion.
/// Attach this to your existing fire alarm GameObject.
/// </summary>
public class FireAlarmTaskController : MonoBehaviour
{
    // Public fields to define the task
    public string taskName = "Activate Fire Alarm";
    public string taskDescription = "Trigger the fire alarm when temperature reaches the threshold";

    // A public array for the dialogue lines to display on task completion.
    [Tooltip("The dialogue lines to display when this task is completed.")]
    public string[] taskCompletionDialogue;
    
    // Public fields for the success sound clip.
    [Header("Audio")]
    [Tooltip("The AudioSource component that will play the sound.")]
    public AudioSource audioSource;
    [Tooltip("The audio clip to play when the task is completed.")]
    public AudioClip successSound;

    // Events to notify other scripts of progress and completion
    public UnityEvent<int, int> OnProgressUpdated;
    public UnityEvent OnTaskCompleted;

    // Task completion tracking
    private int completedItems = 0;
    private int totalItems = 1;
    private bool isTaskCompleted = false;

    // Fire alarm specific fields
    [Header("Fire Alarm Settings")]
    [Tooltip("The temperature threshold that triggers the alarm")]
    public float temperatureThreshold = 80f;
    [Tooltip("Current temperature value")]
    public float currentTemperature = 20f;
    [Tooltip("Rate at which temperature increases per second")]
    public float temperatureIncreaseRate = 2f;
    [Tooltip("Whether the heating is currently active")]
    public bool isHeating = false;

    // Highlighting
    public HighlightableObject targetObject; // The fire alarm object itself
    
    // UI reference for showing temperature/progress
    public PopupManager popupManager;

    void Start()
    {
        // If no target object is set, try to get HighlightableObject from this GameObject
        if (targetObject == null)
        {
            targetObject = GetComponent<HighlightableObject>();
        }
    }

    void Update()
    {
        // Only update if task isn't completed
        if (!isTaskCompleted)
        {
            UpdateTemperature();
            CheckThreshold();
        }
    }

    /// <summary>
    /// This is the entry point for the task, called by the TaskListManager.
    /// </summary>
    public void InitializeTask()
    {
        OnProgressUpdated.Invoke(0, totalItems);
        Debug.Log($"Fire alarm task '{taskName}' initialized. Threshold: {temperatureThreshold}°C");
    }

    /// <summary>
    /// Updates the temperature when heating is active
    /// </summary>
    private void UpdateTemperature()
    {
        if (isHeating && currentTemperature < temperatureThreshold + 10f) // Allow slight overshoot
        {
            currentTemperature += temperatureIncreaseRate * Time.deltaTime;
        }
    }

    /// <summary>
    /// Checks if temperature threshold has been reached
    /// </summary>
    private void CheckThreshold()
    {
        if (currentTemperature >= temperatureThreshold)
        {
            CompleteTask();
        }
    }

    /// <summary>
    /// Completes the fire alarm task
    /// </summary>
    public void CompleteTask()
    {
        if (!isTaskCompleted)
        {
            Debug.Log($"Fire alarm task '{taskName}' completed! Temperature reached: {currentTemperature:F1}°C");
            
            isTaskCompleted = true;
            completedItems = 1;
            
            // Update progress and notify completion
            OnProgressUpdated.Invoke(completedItems, totalItems);
            OnTaskCompleted.Invoke();
            
            // End highlighting
            EndTask();

            // Play the success sound
            if (audioSource != null && successSound != null)
            {
                audioSource.PlayOneShot(successSound);
            }

            // Show completion dialogue
            ShowCompletionDialogue();
            
            // Optional: Show completion message
            if (popupManager != null)
            {
                popupManager.ShowMessage($"Fire alarm activated! Temperature: {currentTemperature:F1}°C");
            }
        }
    }

    /// <summary>
    /// Shows the completion dialogue using VRDialogueSystem
    /// </summary>
    private void ShowCompletionDialogue()
    {
        VRDialogueSystem dialogueSystem = FindObjectOfType<VRDialogueSystem>();
        if (dialogueSystem != null && taskCompletionDialogue != null && taskCompletionDialogue.Length > 0)
        {
            dialogueSystem.StartDialog(taskCompletionDialogue);
        }
    }

    /// <summary>
    /// Starts the highlighting for this task.
    /// </summary>
    public void StartTask()
    {
        if (isTaskCompleted) return;

        if (targetObject != null)
        {
            targetObject.SetHighlight(true);
        }
        
        Debug.Log($"Started fire alarm task '{taskName}'. Current temperature: {currentTemperature:F1}°C");
    }

    /// <summary>
    /// Ends the highlighting for this task.
    /// </summary>
    public void EndTask()
    {
        Debug.Log($"Ending fire alarm task '{taskName}' and turning off highlights.");
        
        if (targetObject != null)
        {
            targetObject.SetHighlight(false);
        }
    }

    // PUBLIC METHODS - Call these from your existing fire alarm code or other scripts

    /// <summary>
    /// Start heating the fire alarm
    /// </summary>
    public void StartHeating()
    {
        if (!isTaskCompleted)
        {
            isHeating = true;
            Debug.Log("Fire alarm heating started");
            
            if (popupManager != null)
            {
                popupManager.ShowMessage("Fire alarm heating started...");
            }
        }
    }

    /// <summary>
    /// Stop heating the fire alarm
    /// </summary>
    public void StopHeating()
    {
        isHeating = false;
        Debug.Log("Fire alarm heating stopped");
    }

    /// <summary>
    /// Manually set the temperature (useful for testing or integration with existing systems)
    /// </summary>
    public void SetTemperature(float temperature)
    {
        currentTemperature = temperature;
        Debug.Log($"Temperature manually set to: {temperature:F1}°C");
    }

    /// <summary>
    /// Get current temperature progress (0-1)
    /// </summary>
    public float GetTemperatureProgress()
    {
        return Mathf.Clamp01(currentTemperature / temperatureThreshold);
    }

    /// <summary>
    /// Get current temperature
    /// </summary>
    public float GetCurrentTemperature()
    {
        return currentTemperature;
    }

    /// <summary>
    /// Check if threshold has been reached
    /// </summary>
    public bool IsThresholdReached()
    {
        return currentTemperature >= temperatureThreshold;
    }

    /// <summary>
    /// Force complete the task (for testing)
    /// </summary>
    [ContextMenu("Force Complete Task")]
    public void ForceCompleteTask()
    {
        currentTemperature = temperatureThreshold;
        CompleteTask();
    }

    // Visual debugging in Scene view
    void OnDrawGizmosSelected()
    {
        // Draw temperature threshold visualization
        Gizmos.color = isTaskCompleted ? Color.green : (IsThresholdReached() ? Color.red : Color.yellow);
        Gizmos.DrawWireSphere(transform.position, 1f);
        
        // Show temperature progress
        float progress = GetTemperatureProgress();
        Gizmos.color = Color.Lerp(Color.blue, Color.red, progress);
        Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * progress);
    }
}