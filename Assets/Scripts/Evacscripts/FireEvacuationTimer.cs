using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class FireEvacuationTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    [Tooltip("Time limit for evacuation in seconds (180s = 3 minutes)")]
    public float evacuationTimeLimit = 180f;
    
    [Tooltip("Target evacuation time for bonus points (seconds)")]
    public float targetEvacuationTime = 180f;
    
    private float currentTime;
    private bool isTimerRunning = true;
    private bool timeExpired = false;
    
    [Header("UI References")]
    [Tooltip("TextMeshPro text to display timer")]
    public TextMeshProUGUI timerText;
    
    [Tooltip("Canvas to show when time runs out")]
    public GameObject failureCanvas;
    
    [Header("XR Rig Reference")]
    [Tooltip("Reference to XR Rig or player movement controller - NOT REQUIRED")]
    public GameObject xrRig;
    
    [Header("Events")]
    public UnityEvent OnTimeExpired;
    
    void Start()
    {
        currentTime = evacuationTimeLimit;
        
        if (failureCanvas != null)
        {
            failureCanvas.SetActive(false);
        }
        
        UpdateTimerDisplay();
    }
    
    void Update()
    {
        if (!isTimerRunning) return;
        
        currentTime -= Time.deltaTime;
        
        if (currentTime <= 0 && !timeExpired)
        {
            currentTime = 0;
            timeExpired = true;
            HandleTimeExpired();
        }
        
        UpdateTimerDisplay();
    }
    
    void UpdateTimerDisplay()
    {
        if (timerText == null) return;
        
        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);
        
        timerText.text = string.Format("Time: {0:00}:{1:00}", minutes, seconds);
        
        // Change color based on time remaining
        if (currentTime <= 30f)
        {
            timerText.color = Color.red;
        }
        else if (currentTime <= 60f)
        {
            timerText.color = Color.yellow;
        }
        else
        {
            timerText.color = Color.white;
        }
    }
    
    void HandleTimeExpired()
    {
        Debug.Log("TIME EXPIRED! Evacuation failed.");
        
        isTimerRunning = false;
        
        // Stop time (this will prevent player movement)
        Time.timeScale = 0f;
        
        // Show failure canvas
        if (failureCanvas != null)
        {
            failureCanvas.SetActive(true);
        }
        
        // Invoke event
        OnTimeExpired?.Invoke();
        
        // Record failure in scoring system
        var scoreTracker = FindObjectOfType<FireEvacuationScoreTracker>();
        if (scoreTracker != null)
        {
            scoreTracker.RecordTimeExpired();
        }
    }
    
    public void StopTimer()
    {
        isTimerRunning = false;
    }
    
    public void ResumeTimer()
    {
        isTimerRunning = true;
    }
    
    public float GetTimeRemaining()
    {
        return currentTime;
    }
    
    public float GetElapsedTime()
    {
        return evacuationTimeLimit - currentTime;
    }
    
    public bool HasTimeExpired()
    {
        return timeExpired;
    }
    
    public float GetTimeBonus()
    {
        float elapsedTime = GetElapsedTime();
        
        if (elapsedTime <= targetEvacuationTime)
        {
            // +1 point per second under target
            return targetEvacuationTime - elapsedTime;
        }
        else
        {
            // -0.5 points per second over target
            return (targetEvacuationTime - elapsedTime) * 0.5f;
        }
    }
}