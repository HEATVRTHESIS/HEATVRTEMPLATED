using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the UI for the maintenance tasks' questions.
/// This is a Singleton that should be attached to the player or a persistent UI manager object.
/// </summary>
public class MaintenanceUIManager : MonoBehaviour
{
    public static MaintenanceUIManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject questionCanvas;
    public Image questionImage;
    public TextMeshProUGUI questionText;
    public Button yesButton;
    public Button noButton;

    private MaintenanceTaskController currentTask;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Ensure the canvas is hidden at the start.
            questionCanvas.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Set up button listeners.
        yesButton.onClick.AddListener(() => OnAnswerSelected(true));
        noButton.onClick.AddListener(() => OnAnswerSelected(false));
    }

    /// <summary>
    /// Displays the question UI for a given task.
    /// </summary>
    public void ShowQuestion(MaintenanceTaskController task)
    {
        currentTask = task;
        
        // Populate the UI with data from the task controller.
        questionText.text = task.questionText;
        // You'll need to set the image based on which one is the "correct" one.
        // For now, let's just show both images on the buttons themselves.
        // The user can't have two images for one question so you'll need to decide which to display on the main image.
        // The buttons will then be text-based for yes or no.
        // This is a design choice.
        questionImage.sprite = task.yesAnswerImage;
        
        questionCanvas.SetActive(true);
    }
    
    /// <summary>
    /// Called when either the 'Yes' or 'No' button is pressed.
    /// </summary>
    private void OnAnswerSelected(bool isYesAnswer)
    {
        if (currentTask != null)
        {
            currentTask.AnswerQuestion(isYesAnswer);
            // Hide the canvas after the answer is given.
            questionCanvas.SetActive(false);
            currentTask = null;
        }
    }
}