using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneEntryManager : MonoBehaviour
{
    [Header("Entry Points")]
    public Transform defaultEntryPoint;
    public Transform[] namedEntryPoints;
    public string[] entryPointNames;
    
    [Header("Player Settings")]
    public bool autoPositionPlayer = true;
    public float delayBeforePositioning = 0.05f; // Reduced delay
    public bool instantPosition = true; // NEW: Position before first frame renders
    
    private void Awake()
    {
        // Position player immediately if instant positioning is enabled
        if (autoPositionPlayer && instantPosition)
        {
            PositionPlayerImmediate();
        }
    }
    
    private void Start()
    {
        // Only position on Start if not using instant positioning
        if (autoPositionPlayer && !instantPosition)
        {
            Invoke(nameof(PositionPlayer), delayBeforePositioning);
        }
    }
    
    void PositionPlayerImmediate()
    {
        GameObject player = FindVRPlayer();
        if (player == null)
        {
            Debug.LogWarning("Player not found for instant positioning");
            return;
        }
        
        Transform targetEntry = GetEntryPoint();
        if (targetEntry == null)
        {
            Debug.LogWarning("No entry point found for instant positioning");
            return;
        }
        
        // Disable character controller if present
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        
        // Position player immediately
        player.transform.position = targetEntry.position;
        player.transform.rotation = targetEntry.rotation;
        
        Debug.Log($"Player instantly positioned at entry point: {targetEntry.name}");
        
        // Re-enable character controller
        if (cc != null) cc.enabled = true;
    }
    
    void PositionPlayer()
    {
        GameObject player = FindVRPlayer();
        if (player == null) return;
        
        Transform targetEntry = GetEntryPoint();
        if (targetEntry == null) return;
        
        // Disable character controller if present
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        
        // Position player
        player.transform.position = targetEntry.position;
        player.transform.rotation = targetEntry.rotation;
        
        // Re-enable character controller
        if (cc != null) cc.enabled = true;
        
        Debug.Log($"Player positioned at entry point: {targetEntry.name}");
    }
    
    Transform GetEntryPoint()
    {
        // Check if a specific entry point was requested
        string requestedEntry = PlayerPrefs.GetString("RequestedEntryPoint", "");
        
        if (!string.IsNullOrEmpty(requestedEntry))
        {
            // Clear the request
            PlayerPrefs.DeleteKey("RequestedEntryPoint");
            
            // Find the named entry point
            for (int i = 0; i < entryPointNames.Length; i++)
            {
                if (entryPointNames[i] == requestedEntry && i < namedEntryPoints.Length)
                {
                    return namedEntryPoints[i];
                }
            }
        }
        
        // Return default entry point
        return defaultEntryPoint;
    }
    
    GameObject FindVRPlayer()
    {
        GameObject player = GameObject.FindWithTag("Player");
        
        if (player == null)
        {
            player = GameObject.Find("XR Origin") ?? 
                     GameObject.Find("XR Rig") ?? 
                     GameObject.Find("XRRig");
        }
        
        return player;
    }
    
    // Call this before loading a scene to specify entry point
    public static void SetRequestedEntryPoint(string entryPointName)
    {
        PlayerPrefs.SetString("RequestedEntryPoint", entryPointName);
    }
}