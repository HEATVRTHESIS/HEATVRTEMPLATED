using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestionUIManager : MonoBehaviour
{
    // The singleton instance.
    public static QuestionUIManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject questionCanvas;
    public Image questionImage;
    public TextMeshProUGUI questionTextUI;
    public Button yesButton;
    public Button noButton;

    [Header("Camera Settings")]
    public Transform cameraTransform;
    public float distanceFromObject = 1.0f;
    public float heightOffset = 0.5f;

    [Header("Follow Settings")]
    public float followSpeed = 5f;
    public bool smoothFollow = true;

    // Reference to the task that is currently being asked a question.
    private MaintenanceTaskController currentTask;
    private Transform targetObject;
    private Canvas canvas;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // Get the canvas component
        if (questionCanvas != null)
        {
            canvas = questionCanvas.GetComponent<Canvas>();
            if (canvas != null)
            {
                // Set canvas to world space
                canvas.renderMode = RenderMode.WorldSpace;
                
                // Make the canvas ignore raycasts to prevent hover interference
                canvas.sortingOrder = 1000;
                
                // Disable raycast on all UI elements
                Graphic[] graphics = questionCanvas.GetComponentsInChildren<Graphic>();
                foreach (Graphic graphic in graphics)
                {
                    graphic.raycastTarget = true; // Keep buttons interactable
                }
                
                // Set a reasonable scale for world space
                questionCanvas.transform.localScale = Vector3.one * 0.001f;
            }
            
            // Initially hide the question canvas
            questionCanvas.SetActive(false);
        }
    }

    private void Start()
    {
        // Auto-find camera if not assigned
        if (cameraTransform == null)
        {
            // Try Camera.main first
            cameraTransform = Camera.main?.transform;
            
            // If not found, look for any active camera
            if (cameraTransform == null)
            {
                Camera[] cameras = FindObjectsOfType<Camera>();
                foreach (Camera cam in cameras)
                {
                    if (cam.enabled && cam.gameObject.activeInHierarchy)
                    {
                        cameraTransform = cam.transform;
                        break;
                    }
                }
            }
            
            // Final fallback: look for MainCamera tag
            if (cameraTransform == null)
            {
                GameObject mainCam = GameObject.FindWithTag("MainCamera");
                if (mainCam != null)
                {
                    cameraTransform = mainCam.transform;
                }
            }
        }

        // Subscribe to button events
        if (yesButton != null)
        {
            yesButton.onClick.AddListener(() => OnAnswerClicked(true));
        }
        
        if (noButton != null)
        {
            noButton.onClick.AddListener(() => OnAnswerClicked(false));
        }
    }

    private void Update()
    {
        if (questionCanvas != null && questionCanvas.activeSelf && targetObject != null && cameraTransform != null)
        {
            UpdateQuestionPosition();
        }
    }

    /// <summary>
    /// Displays the question UI for a given task and sets it as the current task.
    /// </summary>
    public void ShowQuestion(MaintenanceTaskController task)
    {
        if (task == null || task.targetObject == null)
        {
            Debug.LogError("Cannot show question: task or targetObject is null!");
            return;
        }

        currentTask = task;
        targetObject = task.targetObject.transform;
        
        // Set the UI content
        if (questionTextUI != null)
        {
            questionTextUI.text = task.questionText;
        }
        
        if (questionImage != null)
        {
            questionImage.sprite = task.isYesTheCorrectAnswer ? task.yesAnswerImage : task.noAnswerImage;
        }
        
        // Position the canvas before showing it
        UpdateQuestionPosition();
        
        // Show the canvas
        questionCanvas.SetActive(true);
        
        Debug.Log($"Question UI shown for task: {task.taskName}");
    }

    /// <summary>
    /// Called when either the 'Yes' or 'No' button is pressed.
    /// </summary>
    private void OnAnswerClicked(bool isYesAnswer)
    {
        if (currentTask != null)
        {
            bool isCorrect = (isYesAnswer == currentTask.isYesTheCorrectAnswer);
            currentTask.AnswerQuestion(isCorrect);
            currentTask = null; // Clear the reference
            targetObject = null;
        }
        
        // Hide the canvas
        questionCanvas.SetActive(false);
    }

    /// <summary>
    /// Updates the position and rotation of the question canvas to hover over the target object
    /// </summary>
    private void UpdateQuestionPosition()
    {
        if (questionCanvas == null || targetObject == null || cameraTransform == null)
            return;
        
        // Calculate position above the target object
        Vector3 basePosition = targetObject.position + Vector3.up * heightOffset;
        
        // Calculate direction from camera to target
        Vector3 cameraToTarget = (basePosition - cameraTransform.position).normalized;
        
        // Position UI at a distance from the object towards the camera
        Vector3 uiPosition = basePosition - cameraToTarget * distanceFromObject;
        
        // Apply position (with optional smoothing)
        if (smoothFollow)
        {
            questionCanvas.transform.position = Vector3.Lerp(
                questionCanvas.transform.position, 
                uiPosition, 
                followSpeed * Time.deltaTime
            );
        }
        else
        {
            questionCanvas.transform.position = uiPosition;
        }
        
        // Make UI face the camera
        Vector3 lookDirection = cameraTransform.position - questionCanvas.transform.position;
        lookDirection.y = 0; // Keep it upright, only rotate on Y-axis
        
        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            // Rotate 180 degrees around Y-axis so the canvas faces the camera correctly
            targetRotation *= Quaternion.Euler(0, 180, 0);
            
            if (smoothFollow)
            {
                questionCanvas.transform.rotation = Quaternion.Slerp(
                    questionCanvas.transform.rotation, 
                    targetRotation, 
                    followSpeed * Time.deltaTime
                );
            }
            else
            {
                questionCanvas.transform.rotation = targetRotation;
            }
        }
    }

    /// <summary>
    /// Manually hide the question UI (useful for cleanup or interruptions)
    /// </summary>
    public void HideQuestion()
    {
        if (questionCanvas != null)
        {
            questionCanvas.SetActive(false);
        }
        
        currentTask = null;
        targetObject = null;
    }
}