using UnityEngine;
using System;

/// <summary>
/// A trigger zone that detects when the player enters it.
/// Used for the movement tutorial stage.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class TutorialTriggerZone : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Tag to identify the player")]
    [SerializeField] private string playerTag = "Player";
    
    [Header("Visual Feedback")]
    [Tooltip("Optional: Renderer to highlight this zone")]
    [SerializeField] private Renderer zoneRenderer;
    
    [Tooltip("Color for the highlighted zone")]
    [SerializeField] private Color highlightColor = Color.green;
    
    // Event triggered when player enters
    public event Action OnPlayerEntered;
    
    private bool hasTriggered = false;
    private BoxCollider triggerCollider;
    
    void Awake()
    {
        // Ensure collider is set as trigger
        triggerCollider = GetComponent<BoxCollider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
        
        // Set up visual feedback
        if (zoneRenderer != null)
        {
            Material mat = zoneRenderer.material;
            mat.color = highlightColor;
            
            // Make it semi-transparent
            if (mat.HasProperty("_Mode"))
            {
                mat.SetFloat("_Mode", 3); // Transparent mode
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }
            
            Color transparentColor = highlightColor;
            transparentColor.a = 0.3f;
            mat.color = transparentColor;
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Check if player entered and hasn't triggered yet
        if (!hasTriggered && other.CompareTag(playerTag))
        {
            hasTriggered = true;
            OnPlayerEntered?.Invoke();
            
            Debug.Log("TutorialTriggerZone: Player entered the movement zone!");
        }
        
        // Also check for XR Rig or Camera Rig
        if (!hasTriggered && (other.name.Contains("XR") || other.name.Contains("Camera")))
        {
            hasTriggered = true;
            OnPlayerEntered?.Invoke();
            
            Debug.Log("TutorialTriggerZone: Player entered the movement zone!");
        }
    }
    
    void OnEnable()
    {
        // Show the zone when enabled
        if (zoneRenderer != null)
        {
            zoneRenderer.enabled = true;
        }
    }
    
    void OnDisable()
    {
        // Hide the zone when disabled
        if (zoneRenderer != null)
        {
            zoneRenderer.enabled = false;
        }
    }
}
