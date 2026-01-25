using UnityEngine;
using TMPro;

/// <summary>
/// Enhanced centralized score tracking system for Phase 1: Risk Identification and Mitigation
/// Uses ErrorTracker for accurate error counting
/// </summary>
public class ScoreTracker : MonoBehaviour
{
    public static ScoreTracker Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI scoreValueText;
    
    [Header("Evaluation Canvas")]
    public GameObject evaluationCanvas;
    
    [Header("Short Info Fields (4)")]
    public TextMeshProUGUI evalScoreText;           // "Score: 45/50"
    public TextMeshProUGUI evalPercentageText;      // "90%"
    public TextMeshProUGUI evalLevelText;           // "Expert"
    public TextMeshProUGUI evalTasksText;           // "Tasks: 5/5"
    
    [Header("Large Evaluation Box")]
    public TextMeshProUGUI evalMessageText;         // Full evaluation + improvement feedback

    [Header("Scoring Settings")]
    [SerializeField] private int pointsPerCompletion = 10;
    [SerializeField] private int pointsPerError = -5;

    private int completedTasks = 0;
    private int totalTasks = 0;

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
    }

    private void FindAndRegisterAllTasks()
    {
        TaskController[] standardTasks = FindObjectsOfType<TaskController>();
        foreach (var task in standardTasks)
        {
            RegisterTask(task.OnTaskCompleted);
        }

        MaintenanceTaskController[] maintenanceTasks = FindObjectsOfType<MaintenanceTaskController>();
        foreach (var task in maintenanceTasks)
        {
            RegisterTask(task.OnTaskCompleted);
        }

        StorageTaskController[] storageTasks = FindObjectsOfType<StorageTaskController>();
        foreach (var task in storageTasks)
        {
            RegisterTask(task.OnTaskCompleted);
        }
    }

    private void RegisterTask(UnityEngine.Events.UnityEvent completionEvent)
    {
        totalTasks++;
        completionEvent.AddListener(OnTaskCompleted);
    }

    private void OnTaskCompleted()
    {
        completedTasks++;
        UpdateScoreDisplay();
        
        if (completedTasks >= totalTasks)
        {
            ShowEvaluationCanvas();
        }
    }

    // Backward compatibility - legacy scripts still call this
    public void OnTaskError()
    {
        // Do nothing - errors are now tracked by ErrorTracker
        // This method exists only for backward compatibility
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Tasks: {completedTasks} / {totalTasks}";
        }

        if (scoreValueText != null)
        {
            int currentScore = CalculateFinalScore();
            scoreValueText.text = $"Score: {currentScore}";
        }
    }

    private int CalculateFinalScore()
    {
        int maxScore = completedTasks * pointsPerCompletion;
        
        if (ErrorTracker.Instance != null)
        {
            int totalErrors = ErrorTracker.Instance.disposalErrors +
                            ErrorTracker.Instance.maintenanceErrors +
                            ErrorTracker.Instance.storageErrors;
            
            int errorPenalty = totalErrors * Mathf.Abs(pointsPerError);
            return Mathf.Max(0, maxScore - errorPenalty);
        }
        
        return maxScore;
    }

    public int GetPerfectScore()
    {
        return totalTasks * pointsPerCompletion;
    }

    public int GetTotalErrors()
    {
        if (ErrorTracker.Instance != null)
        {
            return ErrorTracker.Instance.disposalErrors +
                   ErrorTracker.Instance.maintenanceErrors +
                   ErrorTracker.Instance.storageErrors;
        }
        return 0;
    }

    private string GetImprovementFeedback()
    {
        if (ErrorTracker.Instance == null) return "";

        int disposal = ErrorTracker.Instance.disposalErrors;
        int maintenance = ErrorTracker.Instance.maintenanceErrors;
        int storage = ErrorTracker.Instance.storageErrors;

        var errors = new System.Collections.Generic.List<(string type, int count)>
        {
            ("Disposal", disposal),
            ("Maintenance", maintenance),
            ("Storage", storage)
        };

        errors.Sort((a, b) => b.count.CompareTo(a.count));

        string feedback = "";
        int feedbackCount = 0;

        if (errors[0].count > 0)
        {
            feedback += GetErrorTypeFeedback(errors[0].type, errors[0].count);
            feedbackCount++;
        }

        if (errors[1].count > 0 && feedbackCount < 2)
        {
            if (feedbackCount > 0) feedback += "\n\n";
            feedback += GetErrorTypeFeedback(errors[1].type, errors[1].count);
        }

        return feedback;
    }

    private string GetErrorTypeFeedback(string errorType, int count)
    {
        switch (errorType)
        {
            case "Disposal":
                return $"<b>Disposal Issues ({count} errors):</b> Review the correct color coding for each trash type. Red for infectious waste, yellow for hazardous materials, green for general waste, and blue for recyclables.";
            
            case "Maintenance":
                return $"<b>Maintenance Issues ({count} errors):</b> Pay closer attention to equipment inspection criteria. Check expiration dates carefully, look for visible damage like cracks or leaks, and verify all safety certifications.";
            
            case "Storage":
                return $"<b>Storage Issues ({count} errors):</b> Ensure items are stored in their designated locations. Flammable materials need proper cabinets, chemicals require specific storage conditions, and equipment must be organized by type.";
            
            default:
                return "";
        }
    }

    private void ShowEvaluationCanvas()
    {
        if (evaluationCanvas == null) return;

        int finalScore = CalculateFinalScore();
        int perfectScore = GetPerfectScore();
        float percentage = perfectScore > 0 ? (float)finalScore / perfectScore * 100f : 0f;
        int totalErrors = GetTotalErrors();

        string evalLevel;
        string evalStatement;

        if (percentage >= 85f)
        {
            evalLevel = "Expert";
            evalStatement = "<b>Exceptional performance.</b> You demonstrated mastery in identifying cracked beakers, checking extinguisher expirations, and segregating complex waste like chemical trash and sharps into their correct bins.";
        }
        else if (percentage >= 70f)
        {
            evalLevel = "Proficient";
            evalStatement = "<b>Minor improvements needed.</b> Accuracy was high, but ensure all items are placed correctly every time to avoid minor point deductions.";
        }
        else if (percentage >= 50f)
        {
            evalLevel = "Intermediate";
            evalStatement = "<b>Critical errors identified.</b> You did not correctly evaluate the condition of the equipment or incorrectly sorted hazardous materials, resulting in breaches of safety protocols.";
        }
        else
        {
            evalLevel = "Beginner";
            evalStatement = "<b>Unsatisfactory.</b> Significant failure in protocol. You must review the Waste Segregation Guide to ensure hospital safety.";
        }

        // Short info fields
        if (evalScoreText != null)
            evalScoreText.text = $"Score: {finalScore}/{perfectScore}";
        
        if (evalPercentageText != null)
            evalPercentageText.text = $"Accuracy: {percentage:F0}%";
        
        if (evalLevelText != null)
            evalLevelText.text = $"Level: {evalLevel}";
        
        if (evalTasksText != null)
            evalTasksText.text = $"Tasks: {completedTasks}/{totalTasks}";

        // Large message box - combine everything
        if (evalMessageText != null)
        {
            string fullMessage = evalStatement;
            
            // Add error breakdown
            if (ErrorTracker.Instance != null && totalErrors > 0)
            {
                fullMessage += $"\n\n<b>Errors ({totalErrors}):</b>";
                fullMessage += $"\n• Disposal: {ErrorTracker.Instance.disposalErrors}";
                fullMessage += $"\n• Maintenance: {ErrorTracker.Instance.maintenanceErrors}";
                fullMessage += $"\n• Storage: {ErrorTracker.Instance.storageErrors}";
            }
            
            // Add improvement feedback
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
            ScoreDataManager.Instance.SaveCurrentLevelData();
        }
    }

    // PUBLIC GETTERS
    public int GetCurrentScore() => CalculateFinalScore();
    public int GetPerfectScoreValue() => GetPerfectScore();
    public int GetMistakeCount() => GetTotalErrors();
    public int GetCompletedTasks() => completedTasks;
    public int GetTotalTasks() => totalTasks;
    public float GetCompletionPercentage() => totalTasks > 0 ? (float)completedTasks / totalTasks * 100f : 0f;
    public float GetAccuracyPercentage() => GetPerfectScore() > 0 ? (float)GetCurrentScore() / GetPerfectScore() * 100f : 0f;
    
    // Legacy compatibility methods
    public int GetStorageErrors() => ErrorTracker.Instance != null ? ErrorTracker.Instance.storageErrors : 0;
    public int GetMaintenanceErrors() => ErrorTracker.Instance != null ? ErrorTracker.Instance.maintenanceErrors : 0;
    public int GetDisposalErrors() => ErrorTracker.Instance != null ? ErrorTracker.Instance.disposalErrors : 0;
    public int GetSafetyViolations() => 0; // Not tracked in Phase 1
}