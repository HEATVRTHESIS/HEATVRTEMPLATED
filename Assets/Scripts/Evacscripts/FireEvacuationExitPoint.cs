using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class FireEvacuationExitPoint : MonoBehaviour
{
    [Header("Exit Settings")]
    [Tooltip("Name of the scene to load when player exits")]
    [SerializeField] private string nextSceneName;
    
    [Tooltip("Delay before loading next scene")]
    public float loadDelay = 1.5f;
    
    [Header("Visual Feedback")]
    [Tooltip("Optional: Material to apply when exit is active")]
    public Material activeMaterial;
    
    [Tooltip("Optional: Particle effect to play on successful exit")]
    public ParticleSystem exitEffect;
    
    private bool hasExited = false;
    
    void Start()
    {
        // Ensure the collider is set to trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        else
        {
            Debug.LogError("FireEvacuationExitPoint requires a Collider component!");
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Check if player entered the exit
        if (hasExited) return;
        
        if (other.CompareTag("Player") || other.GetComponent<Camera>() != null)
        {
            Debug.Log("Player reached exit point!");
            hasExited = true;
            HandlePlayerExit();
        }
    }
    
   void HandlePlayerExit()
{
    // Play exit effect if assigned
    if (exitEffect != null)
    {
        exitEffect.Play();
    }
    
    // Get score tracker FIRST before it's destroyed
    var scoreTracker = FindObjectOfType<FireEvacuationScoreTracker>();
    if (scoreTracker != null)
    {
        // Notify score tracker to calculate final scores
        scoreTracker.RecordSuccessfulExit();
        
        // Save to ScoreDataManager IMMEDIATELY while tracker still exists
        if (ScoreDataManager.Instance != null)
        {
            ScoreDataManager.Instance.SaveFireEvacuationData(scoreTracker);
            Debug.Log("Evacuation data saved!");
        }
        else
        {
            Debug.LogWarning("ScoreDataManager not found!");
        }
    }
    else
    {
        Debug.LogError("FireEvacuationScoreTracker not found!");
    }
    
    // NOW load the next scene
    StartCoroutine(LoadNextSceneCoroutine());
}
    
    IEnumerator LoadNextSceneCoroutine()
    {
        Debug.Log($"Loading next scene: {nextSceneName} in {loadDelay} seconds...");
        
        yield return new WaitForSeconds(loadDelay);
        
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextSceneName);
            
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }
        else
        {
            Debug.LogError("Next scene name is not set!");
        }
    }
    
    // Allow manual scene name setting
    public void SetNextScene(string sceneName)
    {
        nextSceneName = sceneName;
    }
}