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
    
    [Header("Error Dialogue")]
    [Tooltip("The dialogue lines to display when timer expires.")]
    public string[] timeExpiredDialogue;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip errorSound;
    
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

        if (ErrorTracker.Instance != null)
            ErrorTracker.Instance.RecordEvacuationTimeExpiredError();

        if (audioSource != null && errorSound != null)
            audioSource.PlayOneShot(errorSound);

        VRDialogueSystem dialogueSystem = FindObjectOfType<VRDialogueSystem>();
        if (dialogueSystem != null && timeExpiredDialogue != null && timeExpiredDialogue.Length > 0)
        {
            dialogueSystem.StartDialog(timeExpiredDialogue);
        }
        
        Time.timeScale = 0f;
        
        if (failureCanvas != null)
        {
            failureCanvas.SetActive(true);
        }
        
        OnTimeExpired?.Invoke();
        
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
            return targetEvacuationTime - elapsedTime;
        }
        else
        {
            return (targetEvacuationTime - elapsedTime) * 0.5f;
        }
    }
}