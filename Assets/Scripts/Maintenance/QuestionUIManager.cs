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

    // Reference to the task that is currently being asked a question.
    private MaintenanceTaskController currentTask;

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

        // Initially hide the question canvas.
        if (questionCanvas != null)
        {
            questionCanvas.SetActive(false);
        }
    }
    
    // Subscribe to button events
    private void Start()
    {
        yesButton.onClick.AddListener(() => OnAnswerClicked(true));
        noButton.onClick.AddListener(() => OnAnswerClicked(false));
    }

    /// <summary>
    /// Displays the question UI for a given task and sets it as the current task.
    /// </summary>
    public void ShowQuestion(MaintenanceTaskController task)
    {
        currentTask = task;
        questionTextUI.text = task.questionText;
        questionImage.sprite = task.isYesTheCorrectAnswer ? task.yesAnswerImage : task.noAnswerImage;
        questionCanvas.SetActive(true);
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
        }
        
        questionCanvas.SetActive(false);
    }
}