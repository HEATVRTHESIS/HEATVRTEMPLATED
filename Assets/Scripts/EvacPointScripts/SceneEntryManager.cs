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
    public float delayBeforePositioning = 0.1f;
    
    private void Start()
    {
        if (autoPositionPlayer)
        {
            Invoke(nameof(PositionPlayer), delayBeforePositioning);
        }
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