using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Simplified final evaluation screen with scrollable summary
/// </summary>
public class FinalEvaluationScreen : MonoBehaviour
{
    [Header("Outside Scroll View")]
    public TextMeshProUGUI certificationLevelText;  // "EXPERT" / "PROFICIENT" etc
    public Image certificationBadge;                // Medal/badge image
    
    [Header("Inside Scroll View")]
    public TextMeshProUGUI summaryText;             // Full scrollable summary
    
    [Header("Certification Colors")]
    public Color expertColor = new Color(1f, 0.84f, 0f);        // Gold
    public Color proficientColor = new Color(0.75f, 0.75f, 0.75f);  // Silver
    public Color intermediateColor = new Color(0.8f, 0.5f, 0.2f);   // Bronze
    public Color beginnerColor = new Color(0.5f, 0.5f, 0.5f);       // Gray
    
    [Header("Retry Button")]
    public Button retryButton;
    public string mainMenuSceneName = "MainMenu";
    
    private const float TASK_ACCURACY_WEIGHT = 0.5f;
    private const float SPEED_EFFICIENCY_WEIGHT = 0.3f;
    private const float SAFETY_COMPLIANCE_WEIGHT = 0.2f;
    
    private const float EXPERT_THRESHOLD = 85f;
    private const float PROFICIENT_THRESHOLD = 70f;
    private const float INTERMEDIATE_THRESHOLD = 50f;
    
    private float finalWeightedScore = 0f;
    private float taskAccuracyScore = 0f;
    private float speedEfficiencyScore = 0f;
    private float safetyComplianceScore = 0f;
    
    private void Start()
    {
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(OnRetryClicked);
        }
    }
    
    public void CalculateAndDisplayResults()
    {
        if (ScoreDataManager.Instance == null || ErrorTracker.Instance == null)
        {
            Debug.LogError("ScoreDataManager or ErrorTracker not found!");
            return;
        }
        
        List<ScoreDataManager.LevelScoreData> allLevels = ScoreDataManager.Instance.GetAllLevelData();
        
        if (allLevels.Count == 0)
        {
            Debug.LogWarning("No level data found!");
            return;
        }
        
        CalculateTaskAccuracy(allLevels);
        CalculateSpeedEfficiency(allLevels);
        CalculateSafetyCompliance();
        
        finalWeightedScore = (taskAccuracyScore * TASK_ACCURACY_WEIGHT) +
                            (speedEfficiencyScore * SPEED_EFFICIENCY_WEIGHT) +
                            (safetyComplianceScore * SAFETY_COMPLIANCE_WEIGHT);
        
        DisplayResults(allLevels);
    }
    
    private void CalculateTaskAccuracy(List<ScoreDataManager.LevelScoreData> allLevels)
    {
        int totalTasksCompleted = 0;
        int totalTasksPossible = 0;
        int totalPointsEarned = 0;
        int totalPointsPossible = 0;
        
        foreach (var level in allLevels)
        {
            totalTasksCompleted += level.completedTasks;
            totalTasksPossible += level.totalTasks;
            totalPointsEarned += level.finalScore;
            
            if (level.levelType == "Standard")
            {
                totalPointsPossible += level.perfectScore;
            }
            else if (level.levelType == "Fire")
            {
                totalPointsPossible += level.completedTasks * 10;
            }
            else if (level.levelType == "Fire Evacuation")
            {
                totalPointsPossible += 45;
            }
        }
        
        float completionRate = totalTasksPossible > 0 
            ? (float)totalTasksCompleted / totalTasksPossible * 100f 
            : 0f;
        
        float accuracyRate = totalPointsPossible > 0 
            ? (float)totalPointsEarned / totalPointsPossible * 100f 
            : 0f;
        
        taskAccuracyScore = (completionRate * 0.3f) + (accuracyRate * 0.7f);
        taskAccuracyScore = Mathf.Clamp(taskAccuracyScore, 0f, 100f);
    }
    
    private void CalculateSpeedEfficiency(List<ScoreDataManager.LevelScoreData> allLevels)
    {
        float totalTimeScore = 0f;
        int levelsWithTime = 0;
        
        foreach (var level in allLevels)
        {
            if (level.levelType == "Fire")
            {
                float timeRemainingMinutes = level.timeRemaining / 60f;
                float timeScore = Mathf.Clamp(timeRemainingMinutes * 20f, 0f, 100f);
                totalTimeScore += timeScore;
                levelsWithTime++;
            }
            else if (level.levelType == "Fire Evacuation")
            {
                if (level.completedOnTime)
                {
                    float evacuationScore = Mathf.Clamp((180f - level.evacuationTime) / 180f * 100f, 50f, 100f);
                    totalTimeScore += evacuationScore;
                }
                else
                {
                    totalTimeScore += 0f;
                }
                levelsWithTime++;
            }
        }
        
        speedEfficiencyScore = levelsWithTime > 0 ? totalTimeScore / levelsWithTime : 50f;
        speedEfficiencyScore = Mathf.Clamp(speedEfficiencyScore, 0f, 100f);
    }
    
    private void CalculateSafetyCompliance()
    {
        int totalErrors = ErrorTracker.Instance.disposalErrors + ErrorTracker.Instance.maintenanceErrors + 
                         ErrorTracker.Instance.storageErrors + ErrorTracker.Instance.fireNPCErrors + 
                         ErrorTracker.Instance.fireLeverErrors + ErrorTracker.Instance.fireSmokeDoorErrors + 
                         ErrorTracker.Instance.fireExtinguisherErrors + ErrorTracker.Instance.fireWrongExtinguisherErrors +
                         ErrorTracker.Instance.evacuationTimeExpiredErrors + ErrorTracker.Instance.evacuationFireObstacleErrors +
                         ErrorTracker.Instance.evacuationOxygenErrors + ErrorTracker.Instance.evacuationNPCLeftBehindErrors +
                         ErrorTracker.Instance.evacuationNPCNotRescuedErrors + ErrorTracker.Instance.evacuationNoWetClothErrors;
        
        float errorPenalty = totalErrors * 5f;
        safetyComplianceScore = Mathf.Clamp(100f - errorPenalty, 0f, 100f);
    }
    
    private void DisplayResults(List<ScoreDataManager.LevelScoreData> allLevels)
    {
        string certLevel;
        Color certColor;
        
        if (finalWeightedScore >= EXPERT_THRESHOLD)
        {
            certLevel = "EXPERT";
            certColor = expertColor;
        }
        else if (finalWeightedScore >= PROFICIENT_THRESHOLD)
        {
            certLevel = "PROFICIENT";
            certColor = proficientColor;
        }
        else if (finalWeightedScore >= INTERMEDIATE_THRESHOLD)
        {
            certLevel = "INTERMEDIATE";
            certColor = intermediateColor;
        }
        else
        {
            certLevel = "BEGINNER";
            certColor = beginnerColor;
        }
        
        // Set certification level and badge color
        if (certificationLevelText != null)
        {
            certificationLevelText.text = certLevel;
            certificationLevelText.color = certColor;
        }
        
        if (certificationBadge != null)
        {
            certificationBadge.color = certColor;
        }
        
        // Build full summary text
        if (summaryText != null)
        {
            summaryText.text = BuildSummaryText(allLevels, certLevel);
        }
    }
    
    private string BuildSummaryText(List<ScoreDataManager.LevelScoreData> allLevels, string certLevel)
    {
        string summary = "";
        
        // Header
        summary += "<size=24><b>FIRE SAFETY TRAINING EVALUATION</b></size>\n\n";
        
        // Overall Score
        summary += $"<b>Final Score:</b> {finalWeightedScore:F1}/100\n";
        summary += $"<b>Certification Level:</b> {certLevel}\n\n";
        
        // Certification Message
        summary += "<b>Evaluation:</b>\n";
        if (finalWeightedScore >= EXPERT_THRESHOLD)
        {
            summary += "Outstanding performance! You've mastered all aspects of hospital fire safety protocols. Your accuracy in risk identification, speed in emergency response, and adherence to safety procedures demonstrate exceptional competency. Certified for independent operation.\n\n";
        }
        else if (finalWeightedScore >= PROFICIENT_THRESHOLD)
        {
            summary += "Good performance with minor room for improvement. You've shown solid understanding of fire safety protocols. With attention to the areas noted below, you'll achieve expert-level certification. Certified with supervisor oversight recommended.\n\n";
        }
        else if (finalWeightedScore >= INTERMEDIATE_THRESHOLD)
        {
            summary += "Moderate performance. While you completed most tasks, critical errors in safety protocols and emergency response were identified. Additional training required in the areas noted below before full certification. Recommend repeating relevant phases.\n\n";
        }
        else
        {
            summary += "Unsatisfactory performance. Significant improvement needed across all metrics. Strongly recommended to repeat all training phases and study fire safety procedures before proceeding. Not yet certified for hospital operations.\n\n";
        }
        
        // Performance Breakdown
        summary += "<b>━━━ PERFORMANCE BREAKDOWN ━━━</b>\n\n";
        summary += $"<b>Task Accuracy:</b> {taskAccuracyScore:F1}/100 (Weight: 50%)\n";
        summary += $"  → Contribution: {taskAccuracyScore * TASK_ACCURACY_WEIGHT:F1} points\n\n";
        
        summary += $"<b>Speed & Efficiency:</b> {speedEfficiencyScore:F1}/100 (Weight: 30%)\n";
        summary += $"  → Contribution: {speedEfficiencyScore * SPEED_EFFICIENCY_WEIGHT:F1} points\n\n";
        
        summary += $"<b>Safety Compliance:</b> {safetyComplianceScore:F1}/100 (Weight: 20%)\n";
        summary += $"  → Contribution: {safetyComplianceScore * SAFETY_COMPLIANCE_WEIGHT:F1} points\n\n";
        
        // Phase Breakdown
        summary += "<b>━━━ PHASE BREAKDOWN ━━━</b>\n\n";
        
        var phase1Levels = allLevels.Where(l => l.levelType == "Standard").ToList();
        var phase2Levels = allLevels.Where(l => l.levelType == "Fire").ToList();
        var phase3Levels = allLevels.Where(l => l.levelType == "Fire Evacuation").ToList();
        
        if (phase1Levels.Count > 0)
        {
            int totalScore = phase1Levels.Sum(l => l.finalScore);
            int totalPerfectScore = phase1Levels.Sum(l => l.perfectScore);
            float percentage = totalPerfectScore > 0 ? (float)totalScore / totalPerfectScore * 100f : 0f;
            summary += $"<b>Phase 1 - Risk Identification:</b> {totalScore}/{totalPerfectScore} ({percentage:F1}%)\n";
        }
        else
        {
            summary += "<b>Phase 1 - Risk Identification:</b> Not Completed\n";
        }
        
        if (phase2Levels.Count > 0)
        {
            int totalScore = phase2Levels.Sum(l => l.finalScore);
            int tasksCompleted = phase2Levels.Sum(l => l.completedTasks);
            int totalTasks = phase2Levels.Sum(l => l.totalTasks);
            summary += $"<b>Phase 2 - Fire Response:</b> Score {totalScore} | Tasks {tasksCompleted}/{totalTasks}\n";
        }
        else
        {
            summary += "<b>Phase 2 - Fire Response:</b> Not Completed\n";
        }
        
        if (phase3Levels.Count > 0)
        {
            var evacLevel = phase3Levels[0];
            string status = evacLevel.completedOnTime ? "SUCCESS" : "FAILED";
            summary += $"<b>Phase 3 - Evacuation:</b> {status} | Score {evacLevel.finalScore} | Time {evacLevel.evacuationTime:F1}s\n\n";
        }
        else
        {
            summary += "<b>Phase 3 - Evacuation:</b> Not Completed\n\n";
        }
        
        // Detailed Statistics
        int totalCompleted = allLevels.Sum(l => l.completedTasks);
        int totalPossible = allLevels.Sum(l => l.totalTasks);
        
        int totalMistakes = ErrorTracker.Instance.disposalErrors + ErrorTracker.Instance.maintenanceErrors + 
                           ErrorTracker.Instance.storageErrors + ErrorTracker.Instance.fireNPCErrors + 
                           ErrorTracker.Instance.fireLeverErrors + ErrorTracker.Instance.fireSmokeDoorErrors + 
                           ErrorTracker.Instance.fireExtinguisherErrors + ErrorTracker.Instance.fireWrongExtinguisherErrors +
                           ErrorTracker.Instance.evacuationTimeExpiredErrors + ErrorTracker.Instance.evacuationFireObstacleErrors +
                           ErrorTracker.Instance.evacuationOxygenErrors + ErrorTracker.Instance.evacuationNPCLeftBehindErrors +
                           ErrorTracker.Instance.evacuationNPCNotRescuedErrors + ErrorTracker.Instance.evacuationNoWetClothErrors;
        
        summary += "<b>━━━ OVERALL STATISTICS ━━━</b>\n\n";
        summary += $"<b>Tasks Completed:</b> {totalCompleted}/{totalPossible}\n";
        summary += $"<b>Total Errors:</b> {totalMistakes}\n\n";
        
        // Improvement Feedback
        string improvement = GetImprovementFeedback();
        if (!string.IsNullOrEmpty(improvement))
        {
            summary += "<b>━━━ AREAS FOR IMPROVEMENT ━━━</b>\n\n";
            summary += improvement;
        }
        
        return summary;
    }
    
    private string GetImprovementFeedback()
    {
        var errors = new List<(string type, int count)>
        {
            ("Disposal", ErrorTracker.Instance.disposalErrors),
            ("Maintenance", ErrorTracker.Instance.maintenanceErrors),
            ("Storage", ErrorTracker.Instance.storageErrors),
            ("Fire NPC Evacuation", ErrorTracker.Instance.fireNPCErrors),
            ("Fire Alarm Activation", ErrorTracker.Instance.fireLeverErrors),
            ("Smoke Door Closure", ErrorTracker.Instance.fireSmokeDoorErrors),
            ("Fire Extinguisher Usage", ErrorTracker.Instance.fireExtinguisherErrors + ErrorTracker.Instance.fireWrongExtinguisherErrors),
            ("Evacuation Time Management", ErrorTracker.Instance.evacuationTimeExpiredErrors),
            ("Fire Obstacle Avoidance", ErrorTracker.Instance.evacuationFireObstacleErrors),
            ("Oxygen Management", ErrorTracker.Instance.evacuationOxygenErrors),
            ("NPC Rescue", ErrorTracker.Instance.evacuationNPCLeftBehindErrors + ErrorTracker.Instance.evacuationNPCNotRescuedErrors),
            ("Wet Cloth Usage", ErrorTracker.Instance.evacuationNoWetClothErrors)
        };

        errors.Sort((a, b) => b.count.CompareTo(a.count));

        string feedback = "";
        int feedbackCount = 0;

        for (int i = 0; i < errors.Count && feedbackCount < 3; i++)
        {
            if (errors[i].count > 0)
            {
                if (feedbackCount > 0) feedback += "\n";
                feedback += GetDetailedErrorFeedback(errors[i].type, errors[i].count);
                feedbackCount++;
            }
        }

        if (feedbackCount == 0)
        {
            return "<b>Excellent work!</b> No significant errors detected. Keep maintaining this level of performance!";
        }

        return feedback;
    }

    private string GetDetailedErrorFeedback(string errorType, int count)
    {
        switch (errorType)
        {
            case "Disposal":
                return $"<b>• Waste Disposal ({count} errors):</b>\nReview color-coded bin system: Red = infectious waste, Yellow = hazardous chemicals, Green = general waste, Blue = recyclables.";
            
            case "Maintenance":
                return $"<b>• Equipment Maintenance ({count} errors):</b>\nCheck expiration dates, inspect for cracks/damage, and verify safety certifications.";
            
            case "Storage":
                return $"<b>• Chemical Storage ({count} errors):</b>\nFlammable materials require designated cabinets. Review storage protocols for different chemical classes.";
            
            case "Fire NPC Evacuation":
                return $"<b>• NPC Evacuation Guidance ({count} errors):</b>\nGuide individuals away from danger zones toward designated assembly points.";
            
            case "Fire Alarm Activation":
                return $"<b>• Fire Alarm Response ({count} errors):</b>\nActivate pull stations immediately upon discovering fire. Early warning saves lives.";
            
            case "Smoke Door Closure":
                return $"<b>• Smoke Door Operation ({count} errors):</b>\nClose smoke doors to contain fire spread and protect evacuation routes.";
            
            case "Fire Extinguisher Usage":
                return $"<b>• Fire Extinguisher Operation ({count} errors):</b>\nReview PASS method: Pull pin, Aim low, Squeeze handle, Sweep side to side.";
            
            case "Evacuation Time Management":
                return $"<b>• Evacuation Speed ({count} errors):</b>\nPractice faster decision-making. Know evacuation routes beforehand.";
            
            case "Fire Obstacle Avoidance":
                return $"<b>• Fire Hazard Navigation ({count} errors):</b>\nAvoid direct contact with flames. Stay low to avoid smoke inhalation.";
            
            case "Oxygen Management":
                return $"<b>• Oxygen Preservation ({count} errors):</b>\nAlways use wet cloth over nose/mouth in smoky environments.";
            
            case "NPC Rescue":
                return $"<b>• Patient/NPC Assistance ({count} errors):</b>\nDon't leave vulnerable individuals behind. Adjust pace to ensure everyone evacuates safely.";
            
            case "Wet Cloth Usage":
                return $"<b>• Smoke Protection ({count} errors):</b>\nWet cloth is essential in smoke-filled environments. Always wet and apply before entering smoky areas.";
            
            default:
                return "";
        }
    }
    
    private void OnRetryClicked()
    {
        if (ScoreDataManager.Instance != null)
        {
            ScoreDataManager.Instance.ClearAllData();
        }
        
        if (ErrorTracker.Instance != null)
        {
            ErrorTracker.Instance.ResetAllErrors();
        }
        
        UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
    }
}