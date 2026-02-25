using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach this to a button in your main menu to allow players to replay the tutorial.
/// Resets tutorial completion status and reloads the main menu scene.
/// </summary>
public class ReplayTutorialButton : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu"; // The scene with the tutorial
    
    [Header("UI References")]
    [SerializeField] private Button replayButton;

    void Start()
    {
        if (replayButton == null)
        {
            replayButton = GetComponent<Button>();
        }

        if (replayButton != null)
        {
            replayButton.onClick.AddListener(OnReplayTutorialClicked);
        }
        else
        {
            Debug.LogError("ReplayTutorialButton: No button component found!");
        }
    }

    /// <summary>
    /// Called when the replay tutorial button is clicked
    /// </summary>
    void OnReplayTutorialClicked()
    {
        if (TutorialSaveData.Instance == null)
        {
            Debug.LogError("TutorialSaveData not found! Make sure it exists in the scene.");
            return;
        }

        Debug.Log("Resetting tutorial and reloading scene...");
        
        // Reset the tutorial completion status
        TutorialSaveData.Instance.ResetTutorial();
        
        // Reload the main menu scene to restart the tutorial
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// Alternative method to reset tutorial without reloading scene
    /// Useful if you want to just reset the flag for next time
    /// </summary>
    public void ResetTutorialOnly()
    {
        if (TutorialSaveData.Instance != null)
        {
            TutorialSaveData.Instance.ResetTutorial();
            Debug.Log("Tutorial reset - will play again next time you enter the scene");
        }
    }
}
