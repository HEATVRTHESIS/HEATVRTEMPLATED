using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Debug script to track if an object is persisting across scenes when it shouldn't.
/// Attach this to GameManagers to see what's happening.
/// </summary>
public class DebugPersistence : MonoBehaviour
{
    private string originalScene;
    private int instanceID;

    void Awake()
    {
        originalScene = SceneManager.GetActiveScene().name;
        instanceID = GetInstanceID();
        
        Debug.Log($"[DebugPersistence] GameObject '{gameObject.name}' created in scene '{originalScene}' with InstanceID: {instanceID}");
        
        // Check if this object is somehow marked as DontDestroyOnLoad
        if (gameObject.scene.name == "DontDestroyOnLoad")
        {
            Debug.LogError($"[DebugPersistence] WARNING! '{gameObject.name}' is ALREADY in DontDestroyOnLoad scene at Awake!");
            Debug.LogError($"Something is calling DontDestroyOnLoad on this object or its parent!");
            
            // Log all components to find the culprit
            Component[] components = GetComponents<Component>();
            Debug.Log($"Components on '{gameObject.name}':");
            foreach (var comp in components)
            {
                Debug.Log($"  - {comp.GetType().Name}");
            }
        }
    }

    void Start()
    {
        Debug.Log($"[DebugPersistence] '{gameObject.name}' Start() called. Current scene: {SceneManager.GetActiveScene().name}");
        
        if (gameObject.scene.name == "DontDestroyOnLoad")
        {
            Debug.LogError($"[DebugPersistence] '{gameObject.name}' is in DontDestroyOnLoad scene!");
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[DebugPersistence] Scene '{scene.name}' loaded. GameObject '{gameObject.name}' (from '{originalScene}') still exists!");
        Debug.Log($"[DebugPersistence] GameObject scene: {gameObject.scene.name}");
        
        if (scene.name != originalScene)
        {
            Debug.LogWarning($"[DebugPersistence] ALERT! '{gameObject.name}' persisted from '{originalScene}' to '{scene.name}'!");
            Debug.LogWarning($"This object should have been destroyed but wasn't!");
        }
    }

    void OnDestroy()
    {
        Debug.Log($"[DebugPersistence] '{gameObject.name}' (InstanceID: {instanceID}) is being destroyed in scene: {SceneManager.GetActiveScene().name}");
    }
}