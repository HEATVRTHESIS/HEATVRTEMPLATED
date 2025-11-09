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
        public string sceneToLoad;
    }

    [Header("UI References")]
    [SerializeField] private Image departmentImageDisplay;
    [SerializeField] private TextMeshProUGUI departmentTitleText;
    [SerializeField] private TextMeshProUGUI shortDescriptionText;
    [SerializeField] private SceneLoader sceneLoader; // Reference to the Start button's SceneLoader

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
            departmentTitleText.text = currentDept.departmentName;
        }

        // Update description
        if (shortDescriptionText != null)
        {
            shortDescriptionText.text = currentDept.shortDescription;
        }

        // Update the scene loader with the current scene name
        if (sceneLoader != null)
        {
            // Use reflection to set the private sceneToLoad field
            var fieldInfo = typeof(SceneLoader).GetField("sceneToLoad", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(sceneLoader, currentDept.sceneToLoad);
            }
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
}
