using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Enhanced Fire training score tracker with evaluation canvas
/// </summary>
public class FireScoreTracker : MonoBehaviour
{
    public static FireScoreTracker Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI taskCountText;
    public TextMeshProUGUI scoreText;
    
    [Header("Evaluation Canvas")]
    public GameObject evaluationCanvas;
    
    [Header("Short Info Fields (4)")]
    public TextMeshProUGUI evalScoreText;           // "Score: 45"
    public TextMeshProUGUI evalPercentageText;      // "90%"
    public TextMeshProUGUI evalLevelText;           // "Expert"
    public TextMeshProUGUI evalTasksText;           // "Tasks: 5/5"
    
    [Header("Large Evaluation Box")]
    public TextMeshProUGUI evalMessageText;         // Full evaluation + improvement feedback
    
    [Header("Timer Reference")]
    public FireTimer fireTimer;
    
    [Header("Scoring Settings")]
    [SerializeField] private int correctActionPoints = 10;
    [SerializeField] private int errorPenalty = -5;
    [SerializeField] private int safetyCompliancePoints = 15;
    [SerializeField] private int safetyViolationPenalty = -10;
    
    private int currentScore = 0;
    private int completedTasks = 0;
    private int totalTasks = 0;
    private int errorCount = 0;
    private int safetyViolations = 0;
    
    private bool fireExtinguisherUsed = false;
    private bool fireAlarmPulled = false;
    private bool fireDoorClosed = false;
    private bool npcEvacuated = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        FindAndRegisterAllTasks();
        UpdateScoreDisplay();
        
        if (evaluationCanvas != null)
        {
            evaluationCanvas.SetActive(false);
        }
        
        if (fireTimer == null)
        {
            fireTimer = FindObjectOfType<FireTimer>();
        }
        
        if (fireTimer != null)
        {
            fireTimer.OnTimerExpired += OnTimerExpired;
        }
    }

    private void FindAndRegisterAllTasks()
    {
        CustomTaskController[] customTasks = FindObjectsOfType<CustomTaskController>();
        foreach (var task in customTasks)
        {
            RegisterTask(task);
        }
    }

    private void RegisterTask(CustomTaskController task)
    {
        totalTasks++;
        task.OnTaskCompleted.AddListener(() => OnTaskCompleted(task));
    }

    private void OnTaskCompleted(CustomTaskController task)
    {
        completedTasks++;
        currentScore += correctActionPoints;
        
        UpdateScoreDisplay();
        CheckForCompletion();
    }

    public void OnCorrectAction(string actionType)
    {
        currentScore += correctActionPoints;
        UpdateScoreDisplay();
    }

    public void OnTaskError(string errorType)
    {
        errorCount++;
        currentScore += errorPenalty;
        currentScore = Mathf.Max(0, currentScore);
        UpdateScoreDisplay();
    }

    public void OnSafetyCompliance(string complianceType)
    {
        currentScore += safetyCompliancePoints;
        UpdateScoreDisplay();
    }

    public void OnSafetyViolation(string violationType)
    {
        safetyViolations++;
        currentScore += safetyViolationPenalty;
        currentScore = Mathf.Max(0, currentScore);
        UpdateScoreDisplay();
    }

    public void OnFireExtinguisherCompleted()
    {
        fireExtinguisherUsed = true;
    }
    
    public void OnFireAlarmPulled()
    {
        fireAlarmPulled = true;
    }
    
    public void OnFireDoorClosed()
    {
        fireDoorClosed = true;
    }
    
    public void OnNPCEvacuated()
    {
        npcEvacuated = true;
    }

    public void OnWrongNPCResponse()
    {
        OnTaskError("Wrong NPC Response");
    }

    public void OnWrongExtinguisherType()
    {
        OnTaskError("Wrong Extinguisher Type");
    }

    private void UpdateScoreDisplay()
    {
        if (taskCountText != null)
        {
            taskCountText.text = $"Tasks: {completedTasks}/{totalTasks}";
        }
        
        if (scoreText != null)
        {
            scoreText.text = $"Score: {currentScore}";
        }
    }

    private void CheckForCompletion()
    {
        if (completedTasks >= totalTasks)
        {
            ShowEvaluationCanvas();
        }
    }

    private void OnTimerExpired()
    {
        ShowEvaluationCanvas();
    }

    private string GetImprovementFeedback()
    {
        if (ErrorTracker.Instance == null) return "";

        var errors = new List<(string type, int count)>
        {
            ("Fire NPC Evacuation", ErrorTracker.Instance.fireNPCErrors),
            ("Fire Alarm Activation", ErrorTracker.Instance.fireLeverErrors),
            ("Smoke Door Closure", ErrorTracker.Instance.fireSmokeDoorErrors),
            ("Fire Extinguisher", ErrorTracker.Instance.fireExtinguisherErrors + ErrorTracker.Instance.fireWrongExtinguisherErrors)
        };

        errors.Sort((a, b) => b.count.CompareTo(a.count));

        string feedback = "";
        int feedbackCount = 0;

        for (int i = 0; i < errors.Count && feedbackCount < 2; i++)
        {
            if (errors[i].count > 0)
            {
                if (feedbackCount > 0) feedback += "\n\n";
                feedback += GetErrorTypeFeedback(errors[i].type, errors[i].count);
                feedbackCount++;
            }
        }

        return feedback;
    }

    private string GetErrorTypeFeedback(string errorType, int count)
    {
        switch (errorType)
        {
            case "Fire NPC Evacuation":
                return $"<b>• NPC Evacuation Guidance ({count} errors):</b> Remember proper evacuation routes and communication protocols. Guide individuals away from danger zones toward designated assembly points.";
            
            case "Fire Alarm Activation":
                return $"<b>• Fire Alarm Response ({count} errors):</b> Activate pull stations immediately upon discovering fire. Don't delay—early warning saves lives.";
            
            case "Smoke Door Closure":
                return $"<b>• Smoke Door Operation ({count} errors):</b> Close smoke doors to contain fire spread and protect evacuation routes. This is a critical life safety measure.";
            
            case "Fire Extinguisher":
                return $"<b>• Fire Extinguisher Operation ({count} errors):</b> Review PASS method: Pull pin, Aim low, Squeeze handle, Sweep side to side. Use correct extinguisher type for fire class.";
            
            default:
                return "";
        }
    }

    private void ShowEvaluationCanvas()
    {
        if (evaluationCanvas == null) return;

        int totalErrors = 0;
        if (ErrorTracker.Instance != null)
        {
            totalErrors = ErrorTracker.Instance.fireNPCErrors +
                         ErrorTracker.Instance.fireLeverErrors +
                         ErrorTracker.Instance.fireSmokeDoorErrors +
                         ErrorTracker.Instance.fireExtinguisherErrors +
                         ErrorTracker.Instance.fireWrongExtinguisherErrors;
        }

        // Calculate max possible score and actual percentage correctly
        int maxPossibleScore = completedTasks * correctActionPoints;
        int finalScore = maxPossibleScore - (totalErrors * Mathf.Abs(errorPenalty));
        finalScore = Mathf.Max(0, finalScore);
        
        float percentage = maxPossibleScore > 0 ? (float)finalScore / maxPossibleScore * 100f : 0f;

        string evalLevel;
        string evalStatement;

        if (percentage >= 85f)
        {
            evalLevel = "Expert";
            evalStatement = "<b>Exceptional performance.</b> You reacted instantly, activated the fire alarm and shutter levers, and extinguished the fire using the correct PASS technique within the target time.";
        }
        else if (percentage >= 70f)
        {
            evalLevel = "Proficient";
            evalStatement = "<b>Minor improvements needed.</b> Good response, but your speed in suppressing the fire or communicating with civilians to evacuate could be faster to maximize efficiency points.";
        }
        else if (percentage >= 50f)
        {
            evalLevel = "Intermediate";
            evalStatement = "<b>Critical errors identified.</b> You missed critical safety steps such as closing doors or failed to identify the correct extinguisher type, leading to a significant score reduction.";
        }
        else
        {
            evalLevel = "Beginner";
            evalStatement = "<b>Unsatisfactory.</b> Failure to follow fire protocols. You likely engaged hazards or failed to activate the alarm system promptly, compromising the safety of the department.";
        }

        if (evalScoreText != null)
            evalScoreText.text = $"Score: {finalScore}/{maxPossibleScore}";
        
        if (evalPercentageText != null)
            evalPercentageText.text = $"Accuracy: {percentage:F0}%";
        
        if (evalLevelText != null)
            evalLevelText.text = $"Level: {evalLevel}";
        
        if (evalTasksText != null)
            evalTasksText.text = $"Tasks: {completedTasks}/{totalTasks}";

        if (evalMessageText != null)
        {
            string fullMessage = evalStatement;
            
            if (ErrorTracker.Instance != null && totalErrors > 0)
            {
                fullMessage += $"\n\n<b>Errors ({totalErrors}):</b>";
                fullMessage += $"\n• Fire NPC: {ErrorTracker.Instance.fireNPCErrors}";
                fullMessage += $"\n• Fire Alarm: {ErrorTracker.Instance.fireLeverErrors}";
                fullMessage += $"\n• Smoke Door: {ErrorTracker.Instance.fireSmokeDoorErrors}";
                fullMessage += $"\n• Extinguisher: {ErrorTracker.Instance.fireExtinguisherErrors + ErrorTracker.Instance.fireWrongExtinguisherErrors}";
            }
            
            string improvement = GetImprovementFeedback();
            if (!string.IsNullOrEmpty(improvement))
            {
                fullMessage += "\n\n" + improvement;
            }
            else if (totalErrors == 0)
            {
                fullMessage += "\n\n<b>Perfect execution!</b> No errors detected.";
            }
            
            evalMessageText.text = fullMessage;
        }

        evaluationCanvas.SetActive(true);

        if (ScoreDataManager.Instance != null)
        {
            ScoreDataManager.Instance.SaveFireLevelData();
        }
    }

    // PUBLIC GETTERS
    public int GetCurrentScore() => currentScore;
    public int GetCompletedTasks() => completedTasks;
    public int GetTotalTasks() => totalTasks;
    public int GetErrorCount() => errorCount;
    public int GetSafetyViolations() => safetyViolations;
    public float GetCompletionPercentage() => totalTasks > 0 ? (float)completedTasks / totalTasks * 100f : 0f;
    
    public bool WasFireExtinguisherUsed() => fireExtinguisherUsed;
    public bool WasFireAlarmPulled() => fireAlarmPulled;
    public bool WasFireDoorClosed() => fireDoorClosed;
    public bool WasNPCEvacuated() => npcEvacuated;
}