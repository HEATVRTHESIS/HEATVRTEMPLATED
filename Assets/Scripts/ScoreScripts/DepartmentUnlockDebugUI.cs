using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Debug UI for testing department unlock system.
/// Attach to a Canvas and assign UI elements in the inspector.
/// </summary>
public class DepartmentUnlockDebugUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button unlockMedTechButton;
    [SerializeField] private Button unlockERButton;
    [SerializeField] private Button unlockDietaryButton;
    [SerializeField] private Button resetAllButton;
    [SerializeField] private Button refreshButton;

    void Start()
    {
        if (unlockMedTechButton != null)
            unlockMedTechButton.onClick.AddListener(() => UnlockDepartment("MedTech"));
        
        if (unlockERButton != null)
            unlockERButton.onClick.AddListener(() => UnlockDepartment("ER"));
        
        if (unlockDietaryButton != null)
            unlockDietaryButton.onClick.AddListener(() => UnlockDepartment("Dietary"));
        
        if (resetAllButton != null)
            resetAllButton.onClick.AddListener(ResetAll);
        
        if (refreshButton != null)
            refreshButton.onClick.AddListener(RefreshStatus);

        RefreshStatus();
    }

    private void UnlockDepartment(string deptName)
    {
        if (DepartmentSaveData.Instance == null)
        {
            Debug.LogError("DepartmentSaveData not found!");
            return;
        }

        DepartmentSaveData.Instance.UnlockSimulation(deptName);
        Debug.Log($"Manually unlocked {deptName} Simulation");
        RefreshStatus();
    }

    private void ResetAll()
    {
        if (DepartmentSaveData.Instance == null)
        {
            Debug.LogError("DepartmentSaveData not found!");
            return;
        }

        DepartmentSaveData.Instance.ResetAllProgress();
        Debug.Log("All progress reset!");
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        if (statusText == null || DepartmentSaveData.Instance == null) return;

        string status = "<b>=== DEPARTMENT UNLOCK STATUS ===</b>\n\n";

        string[] departments = { "MedTech", "ER", "Dietary" };
        
        foreach (string dept in departments)
        {
            bool trainingComplete = DepartmentSaveData.Instance.IsTrainingCompleted(dept);
            bool simUnlocked = DepartmentSaveData.Instance.IsSimulationUnlocked(dept);
            int bestScore = DepartmentSaveData.Instance.GetBestScore(dept);

            status += $"<b>{dept}:</b>\n";
            status += $"  Training: {(trainingComplete ? "✓ Complete" : "✗ Not Complete")}\n";
            status += $"  Simulation: {(simUnlocked ? "🔓 UNLOCKED" : "🔒 LOCKED")}\n";
            status += $"  Best Score: {bestScore}\n\n";
        }

        status += $"<b>Requirements:</b>\n";
        status += $"Score: {DepartmentSaveData.Instance.GetMinimumScoreRequired()}%\n";
        status += $"Completion: {DepartmentSaveData.Instance.GetMinimumCompletionRequired()}%\n";

        statusText.text = status;
    }

    // Update status periodically
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            RefreshStatus();
        }
    }
}
