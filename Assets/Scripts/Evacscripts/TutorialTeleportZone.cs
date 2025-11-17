using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System;

/// <summary>
/// A teleportation zone that detects when the player teleports to it.
/// Used for the teleportation tutorial stage.
/// </summary>
public class TutorialTeleportZone : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Tag to identify the player")]
    [SerializeField] private string playerTag = "Player";
    
    [Header("Visual Feedback")]
    [Tooltip("Renderer to highlight this teleport area")]
    [SerializeField] private Renderer platformRenderer;
    
    [Tooltip("Color for the highlighted platform")]
    [SerializeField] private Color highlightColor = Color.cyan;
    
    [Header("Teleportation Area")]
    [Tooltip("The teleportation area component (if using XR Toolkit)")]
    [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationArea teleportArea;
    
    // Event triggered when player teleports here
    public event Action OnPlayerTeleported;
    
    private bool hasTriggered = false;
    private Material originalMaterial;
    private Material highlightMaterial;
    
    void Awake()
    {
        // Ensure there's a teleportation area
        if (teleportArea == null)
        {
            teleportArea = GetComponent<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationArea>();
        }
        
        // If still null, add one
        if (teleportArea == null)
        {
            teleportArea = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationArea>();
        }
        
        // Set up visual feedback
        SetupHighlight();
    }
    
    void SetupHighlight()
    {
        if (platformRenderer != null)
        {
            originalMaterial = platformRenderer.material;
            
            // Create a new material for highlighting
            highlightMaterial = new Material(originalMaterial);
            highlightMaterial.color = highlightColor;
            
            // Make it emissive for better visibility
            if (highlightMaterial.HasProperty("_EmissionColor"))
            {
                highlightMaterial.EnableKeyword("_EMISSION");
                highlightMaterial.SetColor("_EmissionColor", highlightColor * 0.5f);
            }
            
            platformRenderer.material = highlightMaterial;
        }
    }
    
    void OnEnable()
    {
        // Subscribe to teleportation events if using XR Toolkit
        if (teleportArea != null)
        {
            teleportArea.selectEntered.AddListener(OnTeleportToArea);
        }
    }
    
    void OnDisable()
    {
        // Unsubscribe from events
        if (teleportArea != null)
        {
            teleportArea.selectEntered.RemoveListener(OnTeleportToArea);
        }
        
        // Restore original material
        if (platformRenderer != null && originalMaterial != null)
        {
            platformRenderer.material = originalMaterial;
        }
    }
    
    /// <summary>
    /// Called when player teleports to this area
    /// </summary>
    void OnTeleportToArea(SelectEnterEventArgs args)
    {
        if (!hasTriggered)
        {
            hasTriggered = true;
            OnPlayerTeleported?.Invoke();
            
            Debug.Log("TutorialTeleportZone: Player teleported to the platform!");
            
            // Optional: Flash the platform to show success
            StartCoroutine(FlashPlatform());
        }
    }
    
    /// <summary>
    /// Alternative detection using trigger collider
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag(playerTag))
        {
            hasTriggered = true;
            OnPlayerTeleported?.Invoke();
            
            Debug.Log("TutorialTeleportZone: Player entered the teleport zone!");
        }
    }
    
    /// <summary>
    /// Visual feedback when player successfully teleports
    /// </summary>
    System.Collections.IEnumerator FlashPlatform()
    {
        if (platformRenderer == null) yield break;
        
        Color originalColor = highlightMaterial.color;
        Color flashColor = Color.white;
        
        float duration = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Flash between original and white
            Color currentColor = Color.Lerp(originalColor, flashColor, Mathf.PingPong(t * 4, 1));
            highlightMaterial.color = currentColor;
            
            if (highlightMaterial.HasProperty("_EmissionColor"))
            {
                highlightMaterial.SetColor("_EmissionColor", currentColor * 0.5f);
            }
            
            yield return null;
        }
        
        // Restore color
        highlightMaterial.color = originalColor;
        if (highlightMaterial.HasProperty("_EmissionColor"))
        {
            highlightMaterial.SetColor("_EmissionColor", originalColor * 0.5f);
        }
    }
}
