using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DepartmentSelector : MonoBehaviour
{
    [System.Serializable]
    public class Department
    {
        public string departmentName;
        public Sprite departmentImage;
        [TextArea(2, 4)]
        public string shortDescription;
        public string trainingSceneToLoad;  // The training level scene
        public string simulationSceneToLoad; // The simulation/risk reduction scene
        public bool isSimulationMode; // Is this entry for simulation mode?
    }

    [Header("UI References")]
    [SerializeField] private Image departmentImageDisplay;
    [SerializeField] private TextMeshProUGUI departmentTitleText;
    [SerializeField] private TextMeshProUGUI shortDescriptionText;
    [SerializeField] private SceneLoader sceneLoader; // Reference to the Start button's SceneLoader
    [SerializeField] private Button startButton; // Reference to the start/play button

    [Header("Lock UI")]
    [SerializeField] private GameObject lockIcon; // Optional: Visual lock icon overlay
    [SerializeField] private TextMeshProUGUI lockMessageText; // Optional: Text explaining why it's locked
    [SerializeField] private Color lockedTextColor = Color.gray;
    [SerializeField] private Color unlockedTextColor = Color.white;

    [Header("Navigation Buttons")]
    [SerializeField] private Button leftArrowButton;
    [SerializeField] private Button rightArrowButton;

    [Header("Department Data")]
    [SerializeField] private Department[] departments;

    private int currentDepartmentIndex = 0;

    private void Start()
    {
        // Set up button listeners
        if (leftArrowButton != null)
            leftArrowButton.onClick.AddListener(PreviousDepartment);
        
        if (rightArrowButton != null)
            rightArrowButton.onClick.AddListener(NextDepartment);

        // Display the first department
        if (departments != null && departments.Length > 0)
        {
            UpdateDepartmentDisplay();
        }
        else
        {
            Debug.LogWarning("No departments assigned to DepartmentSelector!");
        }
    }

    public void NextDepartment()
    {
        if (departments == null || departments.Length == 0) return;

        currentDepartmentIndex++;
        if (currentDepartmentIndex >= departments.Length)
        {
            currentDepartmentIndex = 0; // Loop back to first
        }

        UpdateDepartmentDisplay();
    }

    public void PreviousDepartment()
    {
        if (departments == null || departments.Length == 0) return;

        currentDepartmentIndex--;
        if (currentDepartmentIndex < 0)
        {
            currentDepartmentIndex = departments.Length - 1; // Loop to last
        }

        UpdateDepartmentDisplay();
    }

    private void UpdateDepartmentDisplay()
    {
        if (departments == null || departments.Length == 0 || 
            currentDepartmentIndex < 0 || currentDepartmentIndex >= departments.Length)
        {
            return;
        }

        Department currentDept = departments[currentDepartmentIndex];

        // Update image
        if (departmentImageDisplay != null && currentDept.departmentImage != null)
        {
            departmentImageDisplay.sprite = currentDept.departmentImage;
        }

        // Update title
        if (departmentTitleText != null)
        {
            string displayTitle = currentDept.departmentName;
            if (currentDept.isSimulationMode)
            {
                displayTitle += " - Simulation";
            }
            else
            {
                displayTitle += " - Training";
            }
            departmentTitleText.text = displayTitle;
        }

        // Update description
        if (shortDescriptionText != null)
        {
            shortDescriptionText.text = currentDept.shortDescription;
        }

        // Check if this level is locked and update UI accordingly
        UpdateLockStatus(currentDept);

        // Update the scene loader with the current scene name
        string sceneToLoad = currentDept.isSimulationMode ? 
            currentDept.simulationSceneToLoad : currentDept.trainingSceneToLoad;

        if (sceneLoader != null)
        {
            // Use reflection to set the private sceneToLoad field
            var fieldInfo = typeof(SceneLoader).GetField("sceneToLoad", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(sceneLoader, sceneToLoad);
            }
        }
    }

    private void UpdateLockStatus(Department currentDept)
    {
        bool isLocked = false;
        string lockMessage = "";

        // Check if this is a simulation mode that requires unlocking
        if (currentDept.isSimulationMode && DepartmentSaveData.Instance != null)
        {
            isLocked = !DepartmentSaveData.Instance.IsSimulationUnlocked(currentDept.departmentName);

            if (isLocked)
            {
                int minScore = DepartmentSaveData.Instance.GetMinimumScoreRequired();
                float minCompletion = DepartmentSaveData.Instance.GetMinimumCompletionRequired();
                lockMessage = $"Complete {currentDept.departmentName} Training with {minScore}% score and {minCompletion}% completion to unlock";
            }
        }

        // Update start button interactability
        if (startButton != null)
        {
            startButton.interactable = !isLocked;
        }

        // Update lock icon visibility
        if (lockIcon != null)
        {
            lockIcon.SetActive(isLocked);
        }

        // Update lock message
        if (lockMessageText != null)
        {
            lockMessageText.gameObject.SetActive(isLocked);
            lockMessageText.text = lockMessage;
        }

        // Update text colors to indicate locked/unlocked state
        if (departmentTitleText != null)
        {
            departmentTitleText.color = isLocked ? lockedTextColor : unlockedTextColor;
        }

        if (shortDescriptionText != null)
        {
            shortDescriptionText.color = isLocked ? lockedTextColor : unlockedTextColor;
        }
    }

    // Optional: Public method to get current department info
    public Department GetCurrentDepartment()
    {
        if (departments != null && departments.Length > 0 && 
            currentDepartmentIndex >= 0 && currentDepartmentIndex < departments.Length)
        {
            return departments[currentDepartmentIndex];
        }
        return null;
    }

    // Optional: Method to jump to a specific department by index
    public void SetDepartment(int index)
    {
        if (departments != null && index >= 0 && index < departments.Length)
        {
            currentDepartmentIndex = index;
            UpdateDepartmentDisplay();
        }
    }

    // Helper method to check if player can access current department
    public bool CanAccessCurrentDepartment()
    {
        Department currentDept = GetCurrentDepartment();
        if (currentDept == null) return false;

        if (currentDept.isSimulationMode && DepartmentSaveData.Instance != null)
        {
            return DepartmentSaveData.Instance.IsSimulationUnlocked(currentDept.departmentName);
        }

        return true; // Training modes are always accessible
    }
}