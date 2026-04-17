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
        if (ScoreDataManager.Instance == null)
        {
            Debug.LogError("ScoreDataManager not found!");
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
        CalculateSafetyCompliance(allLevels);
        
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
            
            totalPointsPossible += GetLevelPerfectScore(level);
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
    
    private void CalculateSafetyCompliance(List<ScoreDataManager.LevelScoreData> allLevels)
    {
        int totalErrors = GetTotalSavedErrors(allLevels);
        
        float errorPenalty = totalErrors * 5f;
        safetyComplianceScore = Mathf.Clamp(100f - errorPenalty, 0f, 100f);
    }
    
    private void DisplayResults(List<ScoreDataManager.LevelScoreData> allLevels)
    {
        if (allLevels.Count == 1)
        {
            DisplaySinglePhaseResults(allLevels[0]);
            return;
        }

        DisplayAggregateResults(allLevels);
    }

    private void DisplaySinglePhaseResults(ScoreDataManager.LevelScoreData level)
    {
        float perfectScore = GetLevelPerfectScore(level);
        float percentage = perfectScore > 0f ? Mathf.Clamp((float)level.finalScore / perfectScore * 100f, 0f, 100f) : 0f;

        string certLevel;
        Color certColor;

        if (percentage >= EXPERT_THRESHOLD)
        {
            certLevel = "EXPERT";
            certColor = expertColor;
        }
        else if (percentage >= PROFICIENT_THRESHOLD)
        {
            certLevel = "PROFICIENT";
            certColor = proficientColor;
        }
        else if (percentage >= INTERMEDIATE_THRESHOLD)
        {
            certLevel = "INTERMEDIATE";
            certColor = intermediateColor;
        }
        else
        {
            certLevel = "BEGINNER";
            certColor = beginnerColor;
        }

        if (certificationLevelText != null)
        {
            certificationLevelText.text = certLevel;
            certificationLevelText.color = certColor;
        }

        if (certificationBadge != null)
        {
            certificationBadge.color = certColor;
        }

        if (summaryText != null)
        {
            summaryText.text = BuildSinglePhaseSummary(level, certLevel, percentage, perfectScore);
        }
    }

    private void DisplayAggregateResults(List<ScoreDataManager.LevelScoreData> allLevels)
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

    private string BuildSinglePhaseSummary(ScoreDataManager.LevelScoreData level, string certLevel, float percentage, float perfectScore)
    {
        string phaseTitle = GetPhaseTitle(level.levelType);
        string evaluationStatement = GetPhaseEvaluationStatement(level, percentage);
        int totalErrors = GetSavedErrorCount(level);

        string summary = "";
        summary += $"<size=24><b>{phaseTitle}</b></size>\n\n";
        summary += $"<b>Final Score:</b> {level.finalScore}/{Mathf.RoundToInt(perfectScore)}\n";
        summary += $"<b>Certification Level:</b> {certLevel}\n\n";
        summary += "<b>Evaluation:</b>\n";
        summary += evaluationStatement + "\n\n";
        summary += "<b>━━━ PERFORMANCE BREAKDOWN ━━━</b>\n\n";
        summary += $"<b>Task Completion:</b> {level.completedTasks}/{level.totalTasks} ({level.completionPercentage:F1}%)\n";
        summary += $"<b>Score Accuracy:</b> {percentage:F1}%\n";
        summary += $"<b>Total Errors:</b> {totalErrors}\n\n";

        string improvement = GetImprovementFeedbackForSinglePhase(level);
        if (!string.IsNullOrEmpty(improvement))
        {
            summary += "<b>━━━ AREAS FOR IMPROVEMENT ━━━</b>\n\n";
            summary += improvement;
        }

        return summary;
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
            int totalPerfectScore = phase1Levels.Sum(GetLevelPerfectScore);
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
        
        int totalMistakes = GetTotalSavedErrors(allLevels);
        
        summary += "<b>━━━ OVERALL STATISTICS ━━━</b>\n\n";
        summary += $"<b>Tasks Completed:</b> {totalCompleted}/{totalPossible}\n";
        summary += $"<b>Total Errors:</b> {totalMistakes}\n\n";
        
        // Improvement Feedback
        string improvement = GetImprovementFeedback(allLevels);
        if (!string.IsNullOrEmpty(improvement))
        {
            summary += "<b>━━━ AREAS FOR IMPROVEMENT ━━━</b>\n\n";
            summary += improvement;
        }
        
        return summary;
    }
    
    private string GetImprovementFeedback(List<ScoreDataManager.LevelScoreData> allLevels)
    {
        var errors = BuildSavedErrorBreakdown(allLevels);

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

    private string GetImprovementFeedbackForSinglePhase(ScoreDataManager.LevelScoreData level)
    {
        var errors = BuildSavedErrorBreakdown(new List<ScoreDataManager.LevelScoreData> { level });
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

    private List<(string type, int count)> BuildSavedErrorBreakdown(List<ScoreDataManager.LevelScoreData> allLevels)
    {
        int disposal = 0;
        int maintenance = 0;
        int storage = 0;
        int fireNpc = 0;
        int fireLever = 0;
        int fireSmokeDoor = 0;
        int fireExtinguisher = 0;
        int fireWrongExtinguisher = 0;
        int evacTimeExpired = 0;
        int evacFireObstacle = 0;
        int evacOxygen = 0;
        int evacNpcLeftBehind = 0;
        int evacNpcNotRescued = 0;
        int evacNoWetCloth = 0;

        foreach (var level in allLevels)
        {
            disposal += level.disposalErrors;
            maintenance += level.maintenanceErrors;
            storage += level.storageErrors;
            fireNpc += level.fireNPCErrors;
            fireLever += level.fireLeverErrors;
            fireSmokeDoor += level.fireSmokeDoorErrors;
            fireExtinguisher += level.fireExtinguisherErrors;
            fireWrongExtinguisher += level.fireWrongExtinguisherErrors;
            evacTimeExpired += level.evacuationTimeExpiredErrors;
            evacFireObstacle += level.evacuationFireObstacleErrors;
            evacOxygen += level.evacuationOxygenErrors;
            evacNpcLeftBehind += level.evacuationNPCLeftBehindErrors;
            evacNpcNotRescued += level.evacuationNPCNotRescuedErrors;
            evacNoWetCloth += level.evacuationNoWetClothErrors;
        }

        return new List<(string type, int count)>
        {
            ("Disposal", disposal),
            ("Maintenance", maintenance),
            ("Storage", storage),
            ("Fire NPC Evacuation", fireNpc),
            ("Fire Alarm Activation", fireLever),
            ("Smoke Door Closure", fireSmokeDoor),
            ("Fire Extinguisher Usage", fireExtinguisher + fireWrongExtinguisher),
            ("Evacuation Time Management", evacTimeExpired),
            ("Fire Obstacle Avoidance", evacFireObstacle),
            ("Oxygen Management", evacOxygen),
            ("NPC Rescue", evacNpcLeftBehind + evacNpcNotRescued),
            ("Wet Cloth Usage", evacNoWetCloth)
        };
    }

    private int GetTotalSavedErrors(List<ScoreDataManager.LevelScoreData> allLevels)
    {
        int total = 0;
        foreach (var level in allLevels)
        {
            total += GetSavedErrorCount(level);
        }

        return total;
    }

    private int GetSavedErrorCount(ScoreDataManager.LevelScoreData level)
    {
        return level.storageErrors + level.maintenanceErrors + level.disposalErrors +
               level.fireNPCErrors + level.fireLeverErrors + level.fireSmokeDoorErrors +
               level.fireExtinguisherErrors + level.fireWrongExtinguisherErrors +
               level.evacuationTimeExpiredErrors + level.evacuationFireObstacleErrors +
               level.evacuationOxygenErrors + level.evacuationNPCLeftBehindErrors +
               level.evacuationNPCNotRescuedErrors + level.evacuationNoWetClothErrors;
    }

    private int GetLevelPerfectScore(ScoreDataManager.LevelScoreData level)
    {
        if (level.perfectScore > 0)
        {
            return level.perfectScore;
        }

        if (level.levelType == "Fire")
        {
            return level.totalTasks * 10;
        }

        if (level.levelType == "Fire Evacuation")
        {
            return level.totalTasks * 15;
        }

        return level.totalTasks * 10;
    }

    private string GetPhaseTitle(string levelType)
    {
        if (levelType == "Standard") return "PHASE 1: RISK IDENTIFICATION";
        if (levelType == "Fire") return "PHASE 2: FIRE RESPONSE";
        if (levelType == "Fire Evacuation") return "PHASE 3: HOSPITAL-WIDE EVACUATION";
        return "TRAINING EVALUATION";
    }

    private string GetPhaseEvaluationStatement(ScoreDataManager.LevelScoreData level, float percentage)
    {
        if (level.levelType == "Fire Evacuation")
        {
            if (percentage >= EXPERT_THRESHOLD)
            {
                return "<b>Exceptional performance.</b> You evacuated under the 2-minute target, maintained a safe oxygen meter using a wet cloth, and successfully followed the compass to the exit.";
            }

            if (percentage >= PROFICIENT_THRESHOLD)
            {
                return "<b>Minor improvements needed.</b> You reached the exit safely, but could improve your score by initiating the evacuation faster or assisting colleagues more efficiently.";
            }

            if (percentage >= INTERMEDIATE_THRESHOLD)
            {
                return "<b>Critical errors identified.</b> Safety violations occurred. You may have ignored your oxygen meter or failed to avoid burning debris, leading to health depletion.";
            }

            return "<b>Unsatisfactory.</b> You failed to evacuate safely. Ensure you use a wet cloth for breathing and avoid obstacles to prevent loss of consciousness in future attempts.";
        }

        if (level.levelType == "Fire")
        {
            if (percentage >= EXPERT_THRESHOLD)
            {
                return "<b>Exceptional performance.</b> You reacted instantly, activated the fire alarm and shutter levers, and extinguished the fire using the correct PASS technique within the target time.";
            }

            if (percentage >= PROFICIENT_THRESHOLD)
            {
                return "<b>Minor improvements needed.</b> Good response, but your speed in suppressing the fire or communicating with civilians to evacuate could be faster to maximize efficiency points.";
            }

            if (percentage >= INTERMEDIATE_THRESHOLD)
            {
                return "<b>Critical errors identified.</b> You missed critical safety steps such as closing doors or failed to identify the correct extinguisher type, leading to a significant score reduction.";
            }

            return "<b>Unsatisfactory.</b> Failure to follow fire protocols. You likely engaged hazards or failed to activate the alarm system promptly, compromising the safety of the department.";
        }

        if (percentage >= EXPERT_THRESHOLD)
        {
            return "<b>Exceptional performance.</b> You demonstrated mastery in identifying hazards, checking equipment conditions, and segregating materials into their correct locations.";
        }

        if (percentage >= PROFICIENT_THRESHOLD)
        {
            return "<b>Minor improvements needed.</b> Accuracy was high, but ensure all items are handled correctly every time to avoid minor point deductions.";
        }

        if (percentage >= INTERMEDIATE_THRESHOLD)
        {
            return "<b>Critical errors identified.</b> You did not correctly evaluate equipment condition or incorrectly handled safety materials, resulting in breaches of protocol.";
        }

        return "<b>Unsatisfactory.</b> Significant failure in protocol. Review the training instructions before proceeding.";
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