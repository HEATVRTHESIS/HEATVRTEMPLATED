using UnityEngine;
using TMPro;
using System;
using System.Collections;

public class FireTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float totalTime = 180f;
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerDisplay;
    
    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color criticalColor = Color.red;
    
    private float currentTime;
    private bool isRunning = false;
    private bool isPaused = false;
    private bool hasExpired = false;
    
    public event Action OnTimerExpired;
    public event Action<float> OnTimerTick;
    
    void Start()
    {
        ResetTimer();
        StartTimer();
    }
    
    void Update()
    {
        if (isRunning && !isPaused && !hasExpired)
        {
            currentTime -= Time.deltaTime;
            OnTimerTick?.Invoke(currentTime);
            UpdateTimerDisplay();
            
            if (currentTime <= 0f)
            {
                TimerExpired();
            }
        }
    }
    
    public void StartTimer()
    {
        if (!hasExpired)
        {
            isRunning = true;
            isPaused = false;
        }
    }
    
    public void PauseTimer()
    {
        isPaused = true;
    }
    
    public void ResumeTimer()
    {
        if (!hasExpired)
        {
            isPaused = false;
        }
    }
    
    public void StopTimer()
    {
        isRunning = false;
        isPaused = false;
    }
    
    public void ResetTimer()
    {
        currentTime = totalTime;
        hasExpired = false;
        isRunning = false;
        isPaused = false;
        UpdateTimerDisplay();
    }
    
    public void AddTime(float seconds)
    {
        if (!hasExpired)
        {
            currentTime += seconds;
            UpdateTimerDisplay();
        }
    }
    
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
                UpdateTimerDisplay();
            }
        }
    }
    
    private void TimerExpired()
    {
        currentTime = 0f;
        hasExpired = true;
        isRunning = false;
        
        UpdateTimerDisplay();
        
        // Check incomplete tasks and trigger error dialogues
        StartCoroutine(TriggerIncompleteTaskDialogues());
        
        OnTimerExpired?.Invoke();
    }

    /// <summary>
    /// Triggers error dialogues for incomplete tasks sequentially with delays
    /// </summary>
    private IEnumerator TriggerIncompleteTaskDialogues()
    {
        // Find all incomplete fire tasks
        PullDownTrigger[] levers = FindObjectsOfType<PullDownTrigger>();
        SmokeDoorLeverController[] smokeDoors = FindObjectsOfType<SmokeDoorLeverController>();
        FireExtinguisherController[] extinguishers = FindObjectsOfType<FireExtinguisherController>();

        // Wait a moment before starting dialogues
        yield return new WaitForSeconds(1f);

        // Track if VRDialogueSystem is available
        VRDialogueSystem dialogueSystem = FindObjectOfType<VRDialogueSystem>();
        if (dialogueSystem == null)
        {
            Debug.LogWarning("VRDialogueSystem not found - error dialogues cannot play");
            yield break;
        }

        // Trigger fire alarm lever error dialogues
        foreach (var lever in levers)
        {
            if (!lever.IsTaskCompleted())
            {
                lever.OnTimerExpiredIncomplete();
                yield return new WaitForSeconds(3f);
            }
        }

        // Trigger smoke door lever error dialogues
        foreach (var smokeDoor in smokeDoors)
        {
            if (!smokeDoor.IsTaskCompleted())
            {
                smokeDoor.OnTimerExpiredIncomplete();
                yield return new WaitForSeconds(3f);
            }
        }

        // Trigger extinguisher error dialogues
        foreach (var extinguisher in extinguishers)
        {
            if (!extinguisher.IsTaskCompleted())
            {
                extinguisher.OnTimerExpiredIncomplete();
                yield return new WaitForSeconds(3f);
            }
        }
    }
    
    private void UpdateTimerDisplay()
    {
        if (timerDisplay != null)
        {
            string timeText = FormatTime(currentTime);
            timerDisplay.text = timeText;
            UpdateTimerColor();
        }
    }
    
    private void UpdateTimerColor()
    {
        if (timerDisplay != null)
        {
            if (currentTime <= 10f)
            {
                timerDisplay.color = criticalColor;
            }
            else if (currentTime <= 30f)
            {
                timerDisplay.color = warningColor;
            }
            else
            {
                timerDisplay.color = normalColor;
            }
        }
    }
    
    private string FormatTime(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{minutes:00}:{secs:00}";
    }
    
    public float GetTimeRemaining() => Mathf.Max(0f, currentTime);
    public float GetTimeElapsed() => totalTime - currentTime;
    public float GetProgress() => (totalTime - currentTime) / totalTime;
    public bool IsRunning() => isRunning && !isPaused;
    public bool IsPaused() => isPaused;
    public bool HasExpired() => hasExpired;
    public string GetFormattedTime() => FormatTime(currentTime);
    
    [ContextMenu("Start Timer")]
    private void TestStartTimer() => StartTimer();
    
    [ContextMenu("Pause Timer")]
    private void TestPauseTimer() => PauseTimer();
    
    [ContextMenu("Resume Timer")]
    private void TestResumeTimer() => ResumeTimer();
    
    [ContextMenu("Add 30 seconds")]
    private void TestAddTime() => AddTime(30f);
    
    [ContextMenu("Remove 30 seconds")]
    private void TestRemoveTime() => RemoveTime(30f);
    
    [ContextMenu("Force Expire")]
    private void TestExpireTimer()
    {
        currentTime = 0f;
        TimerExpired();
    }
    
    [ContextMenu("Reset Timer")]
    private void TestResetTimer() => ResetTimer();
    
    void OnDrawGizmosSelected()
    {
        Vector3 center = transform.position;
        float radius = 2f;
        
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(center, radius);
        
        if (totalTime > 0)
        {
            float progress = GetProgress();
            Gizmos.color = hasExpired ? Color.red : (currentTime <= 30f ? Color.yellow : Color.green);
            Gizmos.DrawWireSphere(center, radius * progress);
        }
        
        Gizmos.color = isRunning ? (isPaused ? Color.yellow : Color.green) : Color.red;
        Gizmos.DrawWireCube(center + Vector3.up * 3f, Vector3.one * 0.5f);
    }
}