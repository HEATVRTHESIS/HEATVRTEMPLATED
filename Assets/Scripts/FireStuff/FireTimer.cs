using UnityEngine;
using TMPro;
using System;

/// <summary>
/// Countdown timer for fire training scenarios.
/// Counts down from 3 minutes and triggers completion when time expires.
/// </summary>
public class FireTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float totalTime = 180f; // 3 minutes in seconds
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerDisplay;
    
    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = Color.yellow; // Last 30 seconds
    [SerializeField] private Color criticalColor = Color.red;   // Last 10 seconds
    
    // Timer state
    private float currentTime;
    private bool isRunning = false;
    private bool isPaused = false;
    private bool hasExpired = false;
    
    // Events
    public event Action OnTimerExpired;
    public event Action<float> OnTimerTick; // Passes remaining time
    
    void Start()
    {
        // Initialize timer
        ResetTimer();
        
        // Start the timer automatically
        StartTimer();
    }
    
    void Update()
    {
        if (isRunning && !isPaused && !hasExpired)
        {
            // Update timer
            currentTime -= Time.deltaTime;
            
            // Trigger tick event
            OnTimerTick?.Invoke(currentTime);
            
            // Update display
            UpdateTimerDisplay();
            
            // Check for expiration
            if (currentTime <= 0f)
            {
                TimerExpired();
            }
        }
    }
    
    /// <summary>
    /// Start the timer
    /// </summary>
    public void StartTimer()
    {
        if (!hasExpired)
        {
            isRunning = true;
            isPaused = false;
            Debug.Log($"Fire timer started: {FormatTime(currentTime)}");
        }
    }
    
    /// <summary>
    /// Pause the timer
    /// </summary>
    public void PauseTimer()
    {
        isPaused = true;
        Debug.Log($"Fire timer paused at: {FormatTime(currentTime)}");
    }
    
    /// <summary>
    /// Resume the timer
    /// </summary>
    public void ResumeTimer()
    {
        if (!hasExpired)
        {
            isPaused = false;
            Debug.Log($"Fire timer resumed at: {FormatTime(currentTime)}");
        }
    }
    
    /// <summary>
    /// Stop the timer completely
    /// </summary>
    public void StopTimer()
    {
        isRunning = false;
        isPaused = false;
        Debug.Log("Fire timer stopped");
    }
    
    /// <summary>
    /// Reset the timer to full time
    /// </summary>
    public void ResetTimer()
    {
        currentTime = totalTime;
        hasExpired = false;
        isRunning = false;
        isPaused = false;
        UpdateTimerDisplay();
        Debug.Log($"Fire timer reset to: {FormatTime(currentTime)}");
    }
    
    /// <summary>
    /// Add time to the current timer (bonus time)
    /// </summary>
    public void AddTime(float seconds)
    {
        if (!hasExpired)
        {
            currentTime += seconds;
            Debug.Log($"Added {seconds}s to timer. New time: {FormatTime(currentTime)}");
            UpdateTimerDisplay();
        }
    }
    
    /// <summary>
    /// Remove time from the current timer (penalty time)
    /// </summary>
    public void RemoveTime(float seconds)
    {
        if (!hasExpired)
        {
            currentTime -= seconds;
            if (currentTime <= 0f)
            {
                TimerExpired();
            }
            else
            {
                Debug.Log($"Removed {seconds}s from timer. New time: {FormatTime(currentTime)}");
                UpdateTimerDisplay();
            }
        }
    }
    
    /// <summary>
    /// Called when timer reaches zero
    /// </summary>
    private void TimerExpired()
    {
        currentTime = 0f;
        hasExpired = true;
        isRunning = false;
        
        UpdateTimerDisplay();
        
        Debug.Log("Fire timer expired!");
        
        // Trigger expiration event
        OnTimerExpired?.Invoke();
    }
    
    /// <summary>
    /// Update the timer display UI
    /// </summary>
    private void UpdateTimerDisplay()
    {
        if (timerDisplay != null)
        {
            // Format the time
            string timeText = FormatTime(currentTime);
            timerDisplay.text = timeText;
            
            // Update color based on remaining time
            UpdateTimerColor();
        }
    }
    
    /// <summary>
    /// Update timer color based on remaining time
    /// </summary>
    private void UpdateTimerColor()
    {
        if (timerDisplay != null)
        {
            if (currentTime <= 10f) // Critical - last 10 seconds
            {
                timerDisplay.color = criticalColor;
            }
            else if (currentTime <= 30f) // Warning - last 30 seconds
            {
                timerDisplay.color = warningColor;
            }
            else // Normal
            {
                timerDisplay.color = normalColor;
            }
        }
    }
    
    /// <summary>
    /// Format time as MM:SS
    /// </summary>
    private string FormatTime(float seconds)
    {
        // Ensure we don't show negative time
        seconds = Mathf.Max(0f, seconds);
        
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{minutes:00}:{secs:00}";
    }
    
    // Public getters
    
    /// <summary>
    /// Get remaining time in seconds
    /// </summary>
    public float GetTimeRemaining()
    {
        return Mathf.Max(0f, currentTime);
    }
    
    /// <summary>
    /// Get elapsed time in seconds
    /// </summary>
    public float GetTimeElapsed()
    {
        return totalTime - currentTime;
    }
    
    /// <summary>
    /// Get progress as percentage (0-1)
    /// </summary>
    public float GetProgress()
    {
        return (totalTime - currentTime) / totalTime;
    }
    
    /// <summary>
    /// Check if timer is currently running
    /// </summary>
    public bool IsRunning()
    {
        return isRunning && !isPaused;
    }
    
    /// <summary>
    /// Check if timer is paused
    /// </summary>
    public bool IsPaused()
    {
        return isPaused;
    }
    
    /// <summary>
    /// Check if timer has expired
    /// </summary>
    public bool HasExpired()
    {
        return hasExpired;
    }
    
    /// <summary>
    /// Get formatted time string for display
    /// </summary>
    public string GetFormattedTime()
    {
        return FormatTime(currentTime);
    }
    
    // Context menu methods for testing
    
    [ContextMenu("Start Timer")]
    private void TestStartTimer()
    {
        StartTimer();
    }
    
    [ContextMenu("Pause Timer")]
    private void TestPauseTimer()
    {
        PauseTimer();
    }
    
    [ContextMenu("Resume Timer")]
    private void TestResumeTimer()
    {
        ResumeTimer();
    }
    
    [ContextMenu("Add 30 seconds")]
    private void TestAddTime()
    {
        AddTime(30f);
    }
    
    [ContextMenu("Remove 30 seconds")]
    private void TestRemoveTime()
    {
        RemoveTime(30f);
    }
    
    [ContextMenu("Force Expire")]
    private void TestExpireTimer()
    {
        currentTime = 0f;
        TimerExpired();
    }
    
    [ContextMenu("Reset Timer")]
    private void TestResetTimer()
    {
        ResetTimer();
    }
    
    // Visual debugging
    void OnDrawGizmosSelected()
    {
        // Draw a visual representation of timer progress
        Vector3 center = transform.position;
        float radius = 2f;
        
        // Draw full circle
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(center, radius);
        
        // Draw progress arc
        if (totalTime > 0)
        {
            float progress = GetProgress();
            Gizmos.color = hasExpired ? Color.red : (currentTime <= 30f ? Color.yellow : Color.green);
            
            // Simple visual feedback - could be enhanced with actual arc drawing
            Gizmos.DrawWireSphere(center, radius * progress);
        }
        
        // Show timer state
        Gizmos.color = isRunning ? (isPaused ? Color.yellow : Color.green) : Color.red;
        Gizmos.DrawWireCube(center + Vector3.up * 3f, Vector3.one * 0.5f);
    }
}