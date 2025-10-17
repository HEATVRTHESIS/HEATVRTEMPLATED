using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Final evaluation screen that calculates BFP-compliant scores across all phases.
/// Uses the weighted formula: Total Score = (Task Accuracy × 0.5) + (Speed Efficiency × 0.3) + (Safety Compliance × 0.2)
/// </summary>
public class FinalEvaluationScreen : MonoBehaviour
{
    [Header("Overall Summary UI")]
    public TextMeshProUGUI totalScoreText;
    public TextMeshProUGUI certificationLevelText;
    public TextMeshProUGUI overallFeedbackText;
    public Image certificationBadge; // Optional: visual badge
    
    [Header("Phase Breakdown UI")]
    public TextMeshProUGUI phase1ScoreText;
    public TextMeshProUGUI phase2ScoreText;
    public TextMeshProUGUI phase3ScoreText;
    
    [Header("Metric Breakdown UI")]
    public TextMeshProUGUI taskAccuracyScoreText;
    public TextMeshProUGUI taskAccuracyPercentageText;
    public TextMeshProUGUI speedEfficiencyScoreText;
    public TextMeshProUGUI speedEfficiencyPercentageText;
    public TextMeshProUGUI safetyComplianceScoreText;
    public TextMeshProUGUI safetyCompliancePercentageText;
    
    [Header("Detailed Statistics UI")]
    public TextMeshProUGUI totalTasksCompletedText;
    public TextMeshProUGUI totalMistakesText;
    public TextMeshProUGUI totalSafetyViolationsText;
    public TextMeshProUGUI totalTimeText;
    
    [Header("Certification Colors")]
    public Color expertColor = new Color(1f, 0.84f, 0f); // Gold
    public Color proficientColor = new Color(0.75f, 0.75f, 0.75f); // Silver
    public Color intermediateColor = new Color(0.8f, 0.5f, 0.2f); // Bronze
    public Color beginnerColor = new Color(0.5f, 0.5f, 0.5f); // Gray
    
    [Header("Retry Button")]
    public Button retryButton;
    public string mainMenuSceneName = "MainMenu";
    
    // BFP Weight Constants
    private const float TASK_ACCURACY_WEIGHT = 0.5f;
    private const float SPEED_EFFICIENCY_WEIGHT = 0.3f;
    private const float SAFETY_COMPLIANCE_WEIGHT = 0.2f;
    
    // Certification thresholds
    private const float EXPERT_THRESHOLD = 85f;
    private const float PROFICIENT_THRESHOLD = 70f;
    private const float INTERMEDIATE_THRESHOLD = 50f;
    
    // Score calculation variables
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
    
    /// <summary>
    /// Main method to calculate and display all results
    /// </summary>
    public void CalculateAndDisplayResults()
    {
        if (ScoreDataManager.Instance == null)
        {
            Debug.LogError("ScoreDataManager not found! Cannot display evaluation.");
            return;
        }
        
        List<ScoreDataManager.LevelScoreData> allLevels = ScoreDataManager.Instance.GetAllLevelData();
        
        if (allLevels.Count == 0)
        {
            Debug.LogWarning("No level data found!");
            return;
        }
        
        Debug.Log($"===== CALCULATING FINAL EVALUATION =====");
        Debug.Log($"Total levels completed: {allLevels.Count}");
        
        // Calculate each metric category
        CalculateTaskAccuracy(allLevels);
        CalculateSpeedEfficiency(allLevels);
        CalculateSafetyCompliance(allLevels);
        
        // Calculate final weighted score
        finalWeightedScore = (taskAccuracyScore * TASK_ACCURACY_WEIGHT) +
                            (speedEfficiencyScore * SPEED_EFFICIENCY_WEIGHT) +
                            (safetyComplianceScore * SAFETY_COMPLIANCE_WEIGHT);
        
        Debug.Log($"Task Accuracy: {taskAccuracyScore:F2}");
        Debug.Log($"Speed Efficiency: {speedEfficiencyScore:F2}");
        Debug.Log($"Safety Compliance: {safetyComplianceScore:F2}");
        Debug.Log($"Final Weighted Score: {finalWeightedScore:F2}");
        
        // Display all results
        DisplayOverallSummary();
        DisplayPhaseBreakdown(allLevels);
        DisplayMetricBreakdown();
        DisplayDetailedStatistics(allLevels);
        
        Debug.Log($"========================================");
    }
    
    /// <summary>
    /// Calculate Task Accuracy Score (0-100)
    /// Based on: correct actions, completion rate, and errors
    /// </summary>
    private void CalculateTaskAccuracy(List<ScoreDataManager.LevelScoreData> allLevels)
    {
        int totalTasksCompleted = 0;
        int totalTasksPossible = 0;
        int totalErrors = 0;
        int totalPointsEarned = 0;
        int totalPointsPossible = 0;
        
        foreach (var level in allLevels)
        {
            totalTasksCompleted += level.completedTasks;
            totalTasksPossible += level.totalTasks;
            
            if (level.levelType == "Standard")
            {
                // Phase 1: Use mistake count and score
                totalErrors += level.mistakeCount;
                totalPointsEarned += level.finalScore;
                totalPointsPossible += level.perfectScore;
            }
            else if (level.levelType == "Fire")
            {
                // Phase 2: Use error count
                totalErrors += level.errorCount;
                // For fire levels, estimate perfect score based on completed tasks
                totalPointsEarned += level.finalScore;
                totalPointsPossible += level.completedTasks * 10; // Assume +10 per task
            }
            else if (level.levelType == "Fire Evacuation")
            {
                // Phase 3: Use error count
                totalErrors += level.errorCount;
                totalPointsEarned += level.finalScore;
                // Evacuation has fixed bonuses
                totalPointsPossible += 45; // 15 (wet cloth) + 15 (NPC) + 15 (time)
            }
        }
        
        // Calculate task completion percentage
        float completionRate = totalTasksPossible > 0 
            ? (float)totalTasksCompleted / totalTasksPossible * 100f 
            : 0f;
        
        // Calculate accuracy based on errors (fewer errors = higher accuracy)
        float errorPenalty = totalErrors * 5f; // Each error worth 5 points penalty
        float maxPossibleWithErrors = totalPointsPossible > 0 ? totalPointsPossible : 100f;
        float accuracyFromErrors = Mathf.Clamp((maxPossibleWithErrors - errorPenalty) / maxPossibleWithErrors * 100f, 0f, 100f);
        
        // Combined task accuracy: 70% completion rate + 30% error-based accuracy
        taskAccuracyScore = (completionRate * 0.7f) + (accuracyFromErrors * 0.3f);
        taskAccuracyScore = Mathf.Clamp(taskAccuracyScore, 0f, 100f);
        
        Debug.Log($"Task Accuracy Calculation: Completed {totalTasksCompleted}/{totalTasksPossible}, Errors: {totalErrors}");
    }
    
    /// <summary>
    /// Calculate Speed Efficiency Score (0-100)
    /// Based on: time remaining, evacuation speed, and completion time
    /// </summary>
    private void CalculateSpeedEfficiency(List<ScoreDataManager.LevelScoreData> allLevels)
    {
        float totalTimeScore = 0f;
        int levelsWithTime = 0;
        
        foreach (var level in allLevels)
        {
            if (level.levelType == "Fire")
            {
                // Phase 2: Time remaining is a positive indicator
                // More time left = better efficiency
                float timeRemainingMinutes = level.timeRemaining / 60f;
                float timeScore = Mathf.Clamp(timeRemainingMinutes * 20f, 0f, 100f); // Scale time to 0-100
                totalTimeScore += timeScore;
                levelsWithTime++;
                
                Debug.Log($"Fire level time score: {timeScore:F2} (Time left: {level.timeRemaining:F1}s)");
            }
            else if (level.levelType == "Fire Evacuation")
            {
                // Phase 3: Completion on time is critical
                if (level.completedOnTime)
                {
                    // Faster evacuation = higher score
                    float evacuationMinutes = level.evacuationTime / 60f;
                    float targetTime = 5f; // Assume 5 minute target
                    
                    if (evacuationMinutes <= targetTime)
                    {
                        // Under target: 80-100 points
                        float speedBonus = (targetTime - evacuationMinutes) / targetTime * 20f;
                        totalTimeScore += Mathf.Clamp(80f + speedBonus, 80f, 100f);
                    }
                    else
                    {
                        // Over target but still completed: 50-80 points
                        float overTime = evacuationMinutes - targetTime;
                        float penalty = Mathf.Min(overTime * 5f, 30f); // Max 30 point penalty
                        totalTimeScore += Mathf.Clamp(80f - penalty, 50f, 80f);
                    }
                }
                else
                {
                    // Failed to complete on time: 0-30 points
                    totalTimeScore += 15f; // Participation credit
                }
                
                levelsWithTime++;
                
                Debug.Log($"Evacuation time score: Completed={level.completedOnTime}, Time={level.evacuationTime:F1}s");
            }
        }
        
        speedEfficiencyScore = levelsWithTime > 0 ? totalTimeScore / levelsWithTime : 0f;
        speedEfficiencyScore = Mathf.Clamp(speedEfficiencyScore, 0f, 100f);
        
        Debug.Log($"Speed Efficiency Calculation: Total={totalTimeScore:F2}, Levels={levelsWithTime}");
    }
    
    /// <summary>
    /// Calculate Safety Compliance Score (0-100)
    /// Based on: safety violations, fire damage, protocol adherence
    /// </summary>
    private void CalculateSafetyCompliance(List<ScoreDataManager.LevelScoreData> allLevels)
    {
        int totalSafetyViolations = 0;
        int safetyBonusPoints = 0;
        int maxSafetyBonus = 0;
        
        foreach (var level in allLevels)
        {
            totalSafetyViolations += level.safetyViolations;
            
            if (level.levelType == "Fire Evacuation")
            {
                // Phase 3: Track safety protocol adherence
                if (level.usedWetCloth)
                {
                    safetyBonusPoints += 15;
                }
                if (level.rescuedNPC)
                {
                    safetyBonusPoints += 15;
                }
                maxSafetyBonus += 30; // Max possible safety bonus
            }
            else if (level.levelType == "Fire")
            {
                // Phase 2: Assume some safety compliance points were possible
                // Estimate based on completed tasks (alarm, door closure, etc.)
                maxSafetyBonus += level.completedTasks * 15;
                // Award points based on low violation count
                int estimatedSafetyPoints = Mathf.Max(0, level.completedTasks * 15 - level.safetyViolations * 10);
                safetyBonusPoints += estimatedSafetyPoints;
            }
        }
        
        // Calculate safety score
        // Base score starts at 100, deduct for violations
        float violationPenalty = totalSafetyViolations * 10f;
        float baseScore = Mathf.Clamp(100f - violationPenalty, 0f, 100f);
        
        // Bonus for protocol adherence
        float bonusScore = maxSafetyBonus > 0 ? (float)safetyBonusPoints / maxSafetyBonus * 100f : 100f;
        
        // Combined: 60% violation-based, 40% bonus-based
        safetyComplianceScore = (baseScore * 0.6f) + (bonusScore * 0.4f);
        safetyComplianceScore = Mathf.Clamp(safetyComplianceScore, 0f, 100f);
        
        Debug.Log($"Safety Compliance Calculation: Violations={totalSafetyViolations}, Base={baseScore:F2}, Bonus={bonusScore:F2}");
    }
    
    /// <summary>
    /// Display overall summary with certification level
    /// </summary>
    private void DisplayOverallSummary()
    {
        // Total Score
        if (totalScoreText != null)
        {
            totalScoreText.text = $"Final Score: {finalWeightedScore:F1}/100";
        }
        
        // Determine certification level
        string certLevel = "";
        string feedback = "";
        Color certColor = beginnerColor;
        
        if (finalWeightedScore >= EXPERT_THRESHOLD)
        {
            certLevel = "EXPERT";
            feedback = "Exceptional performance! You have mastered fire safety protocols and emergency response procedures. You demonstrate excellent judgment, speed, and adherence to safety standards.";
            certColor = expertColor;
        }
        else if (finalWeightedScore >= PROFICIENT_THRESHOLD)
        {
            certLevel = "PROFICIENT";
            feedback = "Strong performance with minor areas for improvement. You have a solid understanding of fire safety procedures. Focus on refining your speed and reducing minor errors to reach expert level.";
            certColor = proficientColor;
        }
        else if (finalWeightedScore >= INTERMEDIATE_THRESHOLD)
        {
            certLevel = "INTERMEDIATE";
            feedback = "Adequate performance with critical areas needing improvement. Review safety protocols, practice faster response times, and focus on accuracy. Additional training recommended before real-world application.";
            certColor = intermediateColor;
        }
        else
        {
            certLevel = "BEGINNER";
            feedback = "Unsatisfactory performance. Significant improvement needed across all metrics. Strongly recommended to repeat all training phases and study fire safety procedures before proceeding.";
            certColor = beginnerColor;
        }
        
        // Certification Level
        if (certificationLevelText != null)
        {
            certificationLevelText.text = $"Certification: {certLevel}";
            certificationLevelText.color = certColor;
        }
        
        // Feedback
        if (overallFeedbackText != null)
        {
            overallFeedbackText.text = feedback;
        }
        
        // Badge color
        if (certificationBadge != null)
        {
            certificationBadge.color = certColor;
        }
    }
    
    /// <summary>
    /// Display individual phase scores
    /// </summary>
    private void DisplayPhaseBreakdown(List<ScoreDataManager.LevelScoreData> allLevels)
    {
        var phase1Levels = allLevels.Where(l => l.levelType == "Standard").ToList();
        var phase2Levels = allLevels.Where(l => l.levelType == "Fire").ToList();
        var phase3Levels = allLevels.Where(l => l.levelType == "Fire Evacuation").ToList();
        
        // Phase 1
        if (phase1ScoreText != null)
        {
            if (phase1Levels.Count > 0)
            {
                int totalScore = phase1Levels.Sum(l => l.finalScore);
                int totalPossible = phase1Levels.Sum(l => l.perfectScore);
                float percentage = totalPossible > 0 ? (float)totalScore / totalPossible * 100f : 0f;
                phase1ScoreText.text = $"Phase 1 - Risk Identification: {totalScore}/{totalPossible} ({percentage:F1}%)";
            }
            else
            {
                phase1ScoreText.text = "Phase 1 - Risk Identification: Not Completed";
            }
        }
        
        // Phase 2
        if (phase2ScoreText != null)
        {
            if (phase2Levels.Count > 0)
            {
                int totalScore = phase2Levels.Sum(l => l.finalScore);
                int tasksCompleted = phase2Levels.Sum(l => l.completedTasks);
                int totalTasks = phase2Levels.Sum(l => l.totalTasks);
                phase2ScoreText.text = $"Phase 2 - Fire Response: Score {totalScore} | Tasks {tasksCompleted}/{totalTasks}";
            }
            else
            {
                phase2ScoreText.text = "Phase 2 - Fire Response: Not Completed";
            }
        }
        
        // Phase 3
        if (phase3ScoreText != null)
        {
            if (phase3Levels.Count > 0)
            {
                var evacLevel = phase3Levels[0]; // Should only be one evacuation level
                string status = evacLevel.completedOnTime ? "SUCCESS" : "FAILED";
                phase3ScoreText.text = $"Phase 3 - Evacuation: {status} | Score {evacLevel.finalScore} | Time {evacLevel.evacuationTime:F1}s";
            }
            else
            {
                phase3ScoreText.text = "Phase 3 - Evacuation: Not Completed";
            }
        }
    }
    
    /// <summary>
    /// Display metric breakdown scores
    /// </summary>
    private void DisplayMetricBreakdown()
    {
        // Task Accuracy
        if (taskAccuracyScoreText != null)
        {
            taskAccuracyScoreText.text = $"Task Accuracy: {taskAccuracyScore:F1}/100";
        }
        if (taskAccuracyPercentageText != null)
        {
            taskAccuracyPercentageText.text = $"Weight: 50% | Contribution: {taskAccuracyScore * TASK_ACCURACY_WEIGHT:F1}";
        }
        
        // Speed Efficiency
        if (speedEfficiencyScoreText != null)
        {
            speedEfficiencyScoreText.text = $"Speed & Efficiency: {speedEfficiencyScore:F1}/100";
        }
        if (speedEfficiencyPercentageText != null)
        {
            speedEfficiencyPercentageText.text = $"Weight: 30% | Contribution: {speedEfficiencyScore * SPEED_EFFICIENCY_WEIGHT:F1}";
        }
        
        // Safety Compliance
        if (safetyComplianceScoreText != null)
        {
            safetyComplianceScoreText.text = $"Safety Compliance: {safetyComplianceScore:F1}/100";
        }
        if (safetyCompliancePercentageText != null)
        {
            safetyCompliancePercentageText.text = $"Weight: 20% | Contribution: {safetyComplianceScore * SAFETY_COMPLIANCE_WEIGHT:F1}";
        }
    }
    
    /// <summary>
    /// Display detailed statistics
    /// </summary>
    private void DisplayDetailedStatistics(List<ScoreDataManager.LevelScoreData> allLevels)
    {
        int totalCompleted = allLevels.Sum(l => l.completedTasks);
        int totalPossible = allLevels.Sum(l => l.totalTasks);
        int totalMistakes = allLevels.Sum(l => l.levelType == "Standard" ? l.mistakeCount : l.errorCount);
        int totalViolations = allLevels.Sum(l => l.safetyViolations);
        
        // Calculate total time spent
        float totalTime = 0f;
        foreach (var level in allLevels)
        {
            if (level.levelType == "Fire Evacuation")
            {
                totalTime += level.evacuationTime;
            }
        }
        
        if (totalTasksCompletedText != null)
        {
            totalTasksCompletedText.text = $"Tasks Completed: {totalCompleted}/{totalPossible}";
        }
        
        if (totalMistakesText != null)
        {
            totalMistakesText.text = $"Total Errors: {totalMistakes}";
        }
        
        if (totalSafetyViolationsText != null)
        {
            totalSafetyViolationsText.text = $"Safety Violations: {totalViolations}";
        }
        
        if (totalTimeText != null)
        {
            int minutes = Mathf.FloorToInt(totalTime / 60f);
            int seconds = Mathf.FloorToInt(totalTime % 60f);
            totalTimeText.text = $"Total Training Time: {minutes:00}:{seconds:00}";
        }
    }
    
    /// <summary>
    /// Handle retry button click
    /// </summary>
    private void OnRetryClicked()
    {
        // Clear all data for fresh start
        if (ScoreDataManager.Instance != null)
        {
            ScoreDataManager.Instance.ClearAllData();
        }
        
        // Load main menu or first level
        UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
    }
    
    /// <summary>
    /// Get detailed performance report as string (useful for logging or saving)
    /// </summary>
    public string GetPerformanceReport()
    {
        return $"=== FIRE SAFETY TRAINING EVALUATION ===\n" +
               $"Final Score: {finalWeightedScore:F2}/100\n" +
               $"Task Accuracy: {taskAccuracyScore:F2}/100 (Weight: 50%)\n" +
               $"Speed Efficiency: {speedEfficiencyScore:F2}/100 (Weight: 30%)\n" +
               $"Safety Compliance: {safetyComplianceScore:F2}/100 (Weight: 20%)\n" +
               $"Certification Level: {GetCertificationLevel()}\n" +
               $"======================================";
    }
    
    private string GetCertificationLevel()
    {
        if (finalWeightedScore >= EXPERT_THRESHOLD) return "EXPERT";
        if (finalWeightedScore >= PROFICIENT_THRESHOLD) return "PROFICIENT";
        if (finalWeightedScore >= INTERMEDIATE_THRESHOLD) return "INTERMEDIATE";
        return "BEGINNER";
    }
}