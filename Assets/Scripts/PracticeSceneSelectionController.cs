using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drives a 3-button practice scene menu.
/// Phase 3 optionally passes the currently selected department before scene load.
/// </summary>
public class PracticeSceneSelectionController : MonoBehaviour
{
    [System.Serializable]
    public class DepartmentPracticeScenes
    {
        public string departmentName;
        public Sprite departmentImage;
        [TextArea(2, 4)]
        public string shortDescription;
        public string phase1PracticeScene;
        public string phase2PracticeScene;
        public string phase3PracticeScene;
    }

    [Header("Dependencies")]
    [SerializeField] private PracticeFlowSceneLoader practiceSceneLoader;

    [Header("Department UI (This Canvas)")]
    [SerializeField] private Image departmentImageDisplay;
    [SerializeField] private TextMeshProUGUI departmentTitleText;
    [SerializeField] private TextMeshProUGUI departmentDescriptionText;
    [SerializeField] private Button previousDepartmentButton;
    [SerializeField] private Button nextDepartmentButton;

    [Header("Per-Department Practice Scenes")]
    [SerializeField] private DepartmentPracticeScenes[] departmentSceneMappings;

    [Header("Selection")]
    [SerializeField] private int defaultDepartmentIndex = 0;

    [Header("Fallback Scenes (Optional)")]
    [SerializeField] private bool useFallbackIfDepartmentNotMapped = true;
    [SerializeField] private string fallbackPhase1PracticeScene;
    [SerializeField] private string fallbackPhase2PracticeScene;
    [SerializeField] private string fallbackPhase3PracticeScene;

    private int currentDepartmentIndex = 0;

    private void Start()
    {
        if (previousDepartmentButton != null)
            previousDepartmentButton.onClick.AddListener(PreviousDepartment);

        if (nextDepartmentButton != null)
            nextDepartmentButton.onClick.AddListener(NextDepartment);

        if (departmentSceneMappings != null && departmentSceneMappings.Length > 0)
        {
            currentDepartmentIndex = Mathf.Clamp(defaultDepartmentIndex, 0, departmentSceneMappings.Length - 1);
            UpdateDepartmentDisplay();
        }
        else
        {
            Debug.LogWarning("[PracticeSceneSelectionController] No department mappings configured.");
        }
    }

    private void OnDestroy()
    {
        if (previousDepartmentButton != null)
            previousDepartmentButton.onClick.RemoveListener(PreviousDepartment);

        if (nextDepartmentButton != null)
            nextDepartmentButton.onClick.RemoveListener(NextDepartment);
    }

    public void NextDepartment()
    {
        if (departmentSceneMappings == null || departmentSceneMappings.Length == 0) return;

        currentDepartmentIndex++;
        if (currentDepartmentIndex >= departmentSceneMappings.Length)
            currentDepartmentIndex = 0;

        UpdateDepartmentDisplay();
    }

    public void PreviousDepartment()
    {
        if (departmentSceneMappings == null || departmentSceneMappings.Length == 0) return;

        currentDepartmentIndex--;
        if (currentDepartmentIndex < 0)
            currentDepartmentIndex = departmentSceneMappings.Length - 1;

        UpdateDepartmentDisplay();
    }

    public void LoadPracticePhase1()
    {
        LoadPracticeSceneForCurrentDepartment(1, false);
    }

    public void LoadPracticePhase2()
    {
        LoadPracticeSceneForCurrentDepartment(2, false);
    }

    public void LoadPracticePhase3()
    {
        LoadPracticeSceneForCurrentDepartment(3, true);
    }

    private void LoadPracticeSceneForCurrentDepartment(int phaseNumber, bool passDepartment)
    {
        string selectedDepartmentName = GetSelectedDepartmentName();
        string sceneName = GetPracticeSceneForCurrentDepartment(phaseNumber);

        if (practiceSceneLoader == null)
        {
            Debug.LogError("[PracticeSceneSelectionController] PracticeFlowSceneLoader reference is missing.");
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[PracticeSceneSelectionController] Scene name is empty.");
            return;
        }

        practiceSceneLoader.SetSceneToLoad(sceneName);

        if (passDepartment)
        {
            practiceSceneLoader.SetDepartmentToPass(selectedDepartmentName);
            practiceSceneLoader.LoadSceneNormallyWithDepartment();
            return;
        }

        practiceSceneLoader.LoadSceneNormally();
    }

    private void UpdateDepartmentDisplay()
    {
        DepartmentPracticeScenes current = GetCurrentDepartmentMapping();
        if (current == null) return;

        if (departmentImageDisplay != null)
            departmentImageDisplay.sprite = current.departmentImage;

        if (departmentTitleText != null)
            departmentTitleText.text = current.departmentName;

        if (departmentDescriptionText != null)
            departmentDescriptionText.text = current.shortDescription;
    }

    private string GetSelectedDepartmentName()
    {
        DepartmentPracticeScenes current = GetCurrentDepartmentMapping();
        return current != null ? current.departmentName : string.Empty;
    }

    private DepartmentPracticeScenes GetCurrentDepartmentMapping()
    {
        if (departmentSceneMappings == null || departmentSceneMappings.Length == 0)
            return null;

        if (currentDepartmentIndex < 0 || currentDepartmentIndex >= departmentSceneMappings.Length)
            currentDepartmentIndex = 0;

        return departmentSceneMappings[currentDepartmentIndex];
    }

    private string GetPracticeSceneForCurrentDepartment(int phaseNumber)
    {
        DepartmentPracticeScenes current = GetCurrentDepartmentMapping();
        string mappedScene = current != null ? GetSceneForPhase(current, phaseNumber) : string.Empty;

        if (!string.IsNullOrEmpty(mappedScene))
            return mappedScene;

        if (!useFallbackIfDepartmentNotMapped)
        {
            string deptName = current != null ? current.departmentName : "<none>";
            Debug.LogError($"[PracticeSceneSelectionController] Missing mapped scene for department '{deptName}', phase {phaseNumber}.");
            return string.Empty;
        }

        Debug.LogWarning($"[PracticeSceneSelectionController] Using fallback practice scene for phase {phaseNumber}.");
        return GetFallbackSceneForPhase(phaseNumber);
    }

    private static string GetSceneForPhase(DepartmentPracticeScenes mapping, int phaseNumber)
    {
        switch (phaseNumber)
        {
            case 1:
                return mapping.phase1PracticeScene;
            case 2:
                return mapping.phase2PracticeScene;
            case 3:
                return mapping.phase3PracticeScene;
            default:
                return string.Empty;
        }
    }

    private string GetFallbackSceneForPhase(int phaseNumber)
    {
        switch (phaseNumber)
        {
            case 1:
                return fallbackPhase1PracticeScene;
            case 2:
                return fallbackPhase2PracticeScene;
            case 3:
                return fallbackPhase3PracticeScene;
            default:
                return string.Empty;
        }
    }
}
