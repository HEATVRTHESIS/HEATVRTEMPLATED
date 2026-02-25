using UnityEngine;

/// <summary>
/// Manages tutorial completion status using PlayerPrefs.
/// Persists between game sessions.
/// </summary>
public class TutorialSaveData : MonoBehaviour
{
    public static TutorialSaveData Instance { get; private set; }

    private const string TUTORIAL_COMPLETED_KEY = "TutorialCompleted";
    private bool tutorialCompleted = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadTutorialStatus();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Load tutorial completion status from PlayerPrefs
    /// </summary>
    private void LoadTutorialStatus()
    {
        tutorialCompleted = PlayerPrefs.GetInt(TUTORIAL_COMPLETED_KEY, 0) == 1;
        Debug.Log($"Tutorial Status Loaded: {(tutorialCompleted ? "COMPLETED" : "NOT COMPLETED")}");
    }

    /// <summary>
    /// Save tutorial completion status to PlayerPrefs
    /// </summary>
    private void SaveTutorialStatus()
    {
        PlayerPrefs.SetInt(TUTORIAL_COMPLETED_KEY, tutorialCompleted ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log($"Tutorial Status Saved: {(tutorialCompleted ? "COMPLETED" : "NOT COMPLETED")}");
    }

    /// <summary>
    /// Mark tutorial as completed
    /// </summary>
    public void CompleteTutorial()
    {
        tutorialCompleted = true;
        SaveTutorialStatus();
        Debug.Log("✓ Tutorial marked as COMPLETED and saved!");
    }

    /// <summary>
    /// Check if tutorial has been completed
    /// </summary>
    public bool IsTutorialCompleted()
    {
        return tutorialCompleted;
    }

    /// <summary>
    /// Reset tutorial completion (allows player to replay it)
    /// </summary>
    public void ResetTutorial()
    {
        tutorialCompleted = false;
        SaveTutorialStatus();
        Debug.Log("Tutorial status RESET - player will see tutorial again");
    }

    /// <summary>
    /// Manually set tutorial as completed (for testing/skip)
    /// </summary>
    public void SkipTutorial()
    {
        tutorialCompleted = true;
        SaveTutorialStatus();
        Debug.Log("Tutorial SKIPPED - marked as completed");
    }
}
