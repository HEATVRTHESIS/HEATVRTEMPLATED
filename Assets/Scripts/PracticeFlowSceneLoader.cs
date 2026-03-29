using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Scene loader for optional practice flow.
///
/// Supports two paths:
/// 1) Training -> Practice: set an override for what scene should load after practice.
/// 2) Main Menu -> Practice: no override set, so practice uses its default next scene.
/// </summary>
public class PracticeFlowSceneLoader : MonoBehaviour
{
    private const string NextSceneOverrideKey = "PracticeFlow.NextSceneOverride";

    [Header("Default Target Scene")]
    [SerializeField]
    private string sceneToLoad;

    [Header("Practice Session Reset")]
    [Tooltip("Reset ErrorTracker before entering practice so previous scene errors do not carry over.")]
    [SerializeField]
    private bool resetErrorTrackerOnEnterPractice = true;

    [Tooltip("Clear ScoreDataManager level list before entering practice so saved scores do not stack.")]
    [SerializeField]
    private bool clearScoreDataOnEnterPractice = true;

    [Tooltip("Reset only Fire (Phase 2) errors before entering practice. Use this to keep Phase 1 data while preventing Phase 2 stacking.")]
    [SerializeField]
    private bool resetFireErrorsOnEnterPractice = false;

    [Tooltip("Clear only Fire (Phase 2) saved scores before entering practice. Use this to keep Phase 1 scores while preventing Phase 2 stacking.")]
    [SerializeField]
    private bool clearFireScoreDataOnEnterPractice = false;

    [Header("Optional Department Handoff")]
    [Tooltip("If set, this department value is passed before loading via the Next button flow.")]
    [SerializeField]
    private string departmentNameToPass;

    [Header("Loading Screen")]
    [SerializeField]
    private GameObject loadingScreen;

    [SerializeField]
    private Image progressBarImage;

    /// <summary>
    /// Load sceneToLoad and clear any previous override.
    /// Use this when entering practice from main menu.
    /// </summary>
    public void LoadSceneNormally()
    {
        ClearNextSceneOverride();
        ResetPracticeSessionState();
        StartCoroutine(LoadSceneCoroutine(sceneToLoad));
    }

    /// <summary>
    /// Same as LoadSceneNormally, but also pushes the configured department value before loading.
    /// Useful when loading Phase 3 directly from a menu.
    /// </summary>
    public void LoadSceneNormallyWithDepartment()
    {
        if (!string.IsNullOrEmpty(departmentNameToPass))
        {
            DepartmentData.selectedDepartment = departmentNameToPass;
            Debug.Log($"[PracticeFlowSceneLoader] Passing department before load: {departmentNameToPass}");
        }

        LoadSceneNormally();
    }

    /// <summary>
    /// Load sceneToLoad and store what should be loaded after practice.
    /// Pass the real training continuation scene name from the button.
    /// </summary>
    public void LoadSceneAndSetNextOverride(string nextSceneAfterPractice)
    {
        if (!string.IsNullOrEmpty(nextSceneAfterPractice))
        {
            PlayerPrefs.SetString(NextSceneOverrideKey, nextSceneAfterPractice);
            PlayerPrefs.Save();
            Debug.Log($"[PracticeFlowSceneLoader] Stored next-scene override: {nextSceneAfterPractice}");
        }
        else
        {
            ClearNextSceneOverride();
            Debug.LogWarning("[PracticeFlowSceneLoader] Override was empty. Falling back to default practice flow.");
        }

        ResetPracticeSessionState();
        StartCoroutine(LoadSceneCoroutine(sceneToLoad));
    }

    /// <summary>
    /// Load stored override if present; otherwise load sceneToLoad.
    /// Use this on the practice scene's Next button.
    /// </summary>
    public void LoadNextUsingOverrideOrDefault()
    {
        string targetScene = sceneToLoad;

        if (PlayerPrefs.HasKey(NextSceneOverrideKey))
        {
            string overrideScene = PlayerPrefs.GetString(NextSceneOverrideKey, "");
            if (!string.IsNullOrEmpty(overrideScene))
            {
                targetScene = overrideScene;
                Debug.Log($"[PracticeFlowSceneLoader] Using stored override scene: {targetScene}");
            }

            // One-shot behavior: consume and clear after first use.
            PlayerPrefs.DeleteKey(NextSceneOverrideKey);
            PlayerPrefs.Save();
        }
        else
        {
            Debug.Log("[PracticeFlowSceneLoader] No override found. Using default scene.");
        }

        StartCoroutine(LoadSceneCoroutine(targetScene));
    }

    /// <summary>
    /// Same logic as LoadNextUsingOverrideOrDefault, but first sets DepartmentData.selectedDepartment.
    /// Use this when transitioning from Phase 2 to a Phase 3 scene that needs department context.
    /// </summary>
    public void LoadNextUsingOverrideOrDefaultWithDepartment()
    {
        if (!string.IsNullOrEmpty(departmentNameToPass))
        {
            DepartmentData.selectedDepartment = departmentNameToPass;
            Debug.Log($"[PracticeFlowSceneLoader] Passing department to next scene: {departmentNameToPass}");
        }
        else
        {
            Debug.LogWarning("[PracticeFlowSceneLoader] Department handoff requested, but departmentNameToPass is empty.");
        }

        LoadNextUsingOverrideOrDefault();
    }

    /// <summary>
    /// Sets department value in code, if you prefer not to hardcode it in the inspector.
    /// </summary>
    public void SetDepartmentToPass(string departmentName)
    {
        departmentNameToPass = departmentName;
    }

    public void SetSceneToLoad(string sceneName)
    {
        sceneToLoad = sceneName;
    }

    public void ClearNextSceneOverride()
    {
        if (PlayerPrefs.HasKey(NextSceneOverrideKey))
        {
            PlayerPrefs.DeleteKey(NextSceneOverrideKey);
            PlayerPrefs.Save();
            Debug.Log("[PracticeFlowSceneLoader] Cleared next-scene override.");
        }
    }

    private void ResetPracticeSessionState()
    {
        if (resetErrorTrackerOnEnterPractice && ErrorTracker.Instance != null)
        {
            ErrorTracker.Instance.ResetAllErrors();
            Debug.Log("[PracticeFlowSceneLoader] Reset ErrorTracker for a fresh practice run.");
        }
        else if (resetFireErrorsOnEnterPractice && ErrorTracker.Instance != null)
        {
            ErrorTracker.Instance.ResetFireErrors();
            Debug.Log("[PracticeFlowSceneLoader] Reset Fire (Phase 2) errors for practice run.");
        }

        if (clearScoreDataOnEnterPractice && ScoreDataManager.Instance != null)
        {
            ScoreDataManager.Instance.ClearAllData();
            Debug.Log("[PracticeFlowSceneLoader] Cleared ScoreDataManager for a fresh practice run.");
        }
        else if (clearFireScoreDataOnEnterPractice && ScoreDataManager.Instance != null)
        {
            int removed = ScoreDataManager.Instance.ClearLevelDataByType("Fire");
            Debug.Log($"[PracticeFlowSceneLoader] Cleared {removed} Fire (Phase 2) score entries for practice run.");
        }
    }

    private IEnumerator LoadSceneCoroutine(string targetScene)
    {
        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogError("[PracticeFlowSceneLoader] Target scene is empty.");
            yield break;
        }

        if (loadingScreen != null)
            loadingScreen.SetActive(true);

        if (progressBarImage != null)
        {
            progressBarImage.type = Image.Type.Filled;
            progressBarImage.fillMethod = Image.FillMethod.Horizontal;
            progressBarImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            progressBarImage.fillAmount = 0f;
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene);
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
        {
            if (progressBarImage != null)
                progressBarImage.fillAmount = asyncLoad.progress / 0.9f;

            yield return null;
        }

        if (progressBarImage != null)
            progressBarImage.fillAmount = 1f;

        yield return new WaitForSeconds(0.1f);

        asyncLoad.allowSceneActivation = true;

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);
        System.GC.Collect();
    }
}
