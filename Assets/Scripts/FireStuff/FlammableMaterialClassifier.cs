using UnityEngine;

/// <summary>
/// Defines the different classes of fires and extinguisher types
/// </summary>
public enum FireClass
{
    ABC,  // Class A (ordinary combustibles), B (flammable liquids), C (electrical) - Dry Powder
    K     // Class K (cooking oils/fats) - Wet Chemical
}

/// <summary>
/// Tag a flammable object with its fire class.
/// Attach this to any GameObject that has a FlammableObject component.
/// </summary>
public class FlammableMaterialClassifier : MonoBehaviour
{
    [Header("Fire Classification")]
    [Tooltip("What type of fire does this material create?")]
    public FireClass fireClass = FireClass.ABC;
    
    [Header("Visual Feedback (Optional)")]
    [Tooltip("Color to tint the fire particles (optional, for visual distinction)")]
    public Color fireColor = Color.red;
    
    [Tooltip("Apply fire color tint to particle systems?")]
    public bool applyColorTint = false;

    void Start()
    {
        // Apply visual tint if enabled
        if (applyColorTint)
        {
            ApplyFireColorTint();
        }
        
        Debug.Log($"{gameObject.name} classified as {fireClass} fire");
    }
    
    /// <summary>
    /// Apply color tint to fire particle systems for visual distinction
    /// </summary>
    private void ApplyFireColorTint()
    {
        ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in particleSystems)
        {
            var main = ps.main;
            main.startColor = fireColor;
        }
    }
    
    /// <summary>
    /// Get the fire class of this material
    /// </summary>
    public FireClass GetFireClass()
    {
        return fireClass;
    }
    
    /// <summary>
    /// Check if a specific extinguisher type can put out this fire
    /// </summary>
    public bool CanBeExtinguishedBy(FireClass extinguisherType)
    {
        // ABC extinguishers can only extinguish ABC fires
        // K extinguishers can only extinguish K fires
        return fireClass == extinguisherType;
    }
}
