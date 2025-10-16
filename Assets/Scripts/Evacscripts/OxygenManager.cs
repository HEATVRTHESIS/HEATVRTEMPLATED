using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System.Collections.Generic;

public class OxygenManager : MonoBehaviour
{
    [Header("Oxygen Settings")]
    [Tooltip("Maximum oxygen value")]
    public float maxOxygen = 100f;
    
    [Tooltip("How fast oxygen decreases per second without protection")]
    public float oxygenDecreaseRate = 5f;
    
    [Tooltip("How fast oxygen regenerates per second with wet cloth")]
    public float oxygenRegenerationRate = 15f;
    
    [Header("Current State")]
    [Tooltip("Current oxygen level (read-only in inspector)")]
    [SerializeField] private float currentOxygen = 100f;
    
    [SerializeField] private bool isProtected = false;
    
    [Header("UI References")]
    [Tooltip("UI Slider to show oxygen level")]
    public Slider oxygenSlider;
    
    [Tooltip("TextMeshPro text to show oxygen percentage")]
    public TextMeshProUGUI oxygenText;
    
    [Tooltip("Optional: Image that changes color based on oxygen")]
    public Image oxygenBarFill;
    
    [Header("Color Gradient")]
    public Color highOxygenColor = Color.green;
    public Color mediumOxygenColor = Color.yellow;
    public Color lowOxygenColor = Color.red;
    
    [Header("Warning Settings")]
    [Tooltip("Oxygen level at which warnings start")]
    public float warningThreshold = 30f;
    
    [Tooltip("Oxygen level at which player suffocates")]
    public float suffocationThreshold = 0f;
    
    [Header("Audio (Optional)")]
    public AudioSource breathingSound;
    public AudioClip normalBreathing;
    public AudioClip labordBreathing;
    
    [Header("Events")]
    public UnityEvent OnOxygenDepleted;
    public UnityEvent OnOxygenWarning;
    public UnityEvent OnOxygenSafe;
    
    private bool hasWarned = false;
    private bool hasSuffocated = false;
    
    // Track which cloths are providing protection
    private HashSet<WetCloth> protectingCloths = new HashSet<WetCloth>();
    
    void Start()
    {
        currentOxygen = maxOxygen;
        UpdateUI();
    }
    
    void Update()
    {
        // Check if ANY cloth is providing protection
        isProtected = protectingCloths.Count > 0;
        
        // Decrease or regenerate oxygen based on protection
        if (isProtected)
        {
            // Regenerate oxygen when protected with wet cloth
            currentOxygen += oxygenRegenerationRate * Time.deltaTime;
            currentOxygen = Mathf.Min(currentOxygen, maxOxygen);
            
            // Reset warning flags when oxygen is restored
            if (currentOxygen > warningThreshold)
            {
                if (hasWarned)
                {
                    hasWarned = false;
                    if (OnOxygenSafe != null)
                    {
                        OnOxygenSafe.Invoke();
                    }
                }
            }
        }
        else
        {
            // Decrease oxygen when not protected
            currentOxygen -= oxygenDecreaseRate * Time.deltaTime;
            currentOxygen = Mathf.Max(currentOxygen, 0f);
        }
        
        // Check for warning threshold
        if (currentOxygen <= warningThreshold && currentOxygen > suffocationThreshold && !hasWarned)
        {
            hasWarned = true;
            if (OnOxygenWarning != null)
            {
                OnOxygenWarning.Invoke();
            }
            Debug.LogWarning("OXYGEN LOW! Find wet cloth!");
        }
        
        // Check for suffocation
        if (currentOxygen <= suffocationThreshold && !hasSuffocated)
        {
            hasSuffocated = true;
            if (OnOxygenDepleted != null)
            {
                OnOxygenDepleted.Invoke();
            }
            Debug.LogError("SUFFOCATION! Player needs oxygen!");
            HandleSuffocation();
        }
        
        // Update breathing sounds based on oxygen level
        UpdateBreathingSound();
        
        // Update UI
        UpdateUI();
    }
    
    // NEW METHOD: Register/unregister individual cloths
    public void RegisterClothProtection(WetCloth cloth, bool isProtecting)
    {
        if (isProtecting)
        {
            if (protectingCloths.Add(cloth))
            {
                Debug.Log(cloth.gameObject.name + " is now protecting! Total protecting cloths: " + protectingCloths.Count);
            }
        }
        else
        {
            if (protectingCloths.Remove(cloth))
            {
                Debug.Log(cloth.gameObject.name + " stopped protecting. Total protecting cloths: " + protectingCloths.Count);
            }
        }
    }
    
    // DEPRECATED: Keep for backwards compatibility but not recommended
    public void SetClothProtection(bool protectionActive)
    {
        isProtected = protectionActive;
        
        if (protectionActive)
        {
            Debug.Log("Player is protected with wet cloth! (Legacy method)");
        }
    }
    
    void UpdateUI()
    {
        // Update slider
        if (oxygenSlider != null)
        {
            oxygenSlider.value = currentOxygen / maxOxygen;
        }
        
        // Update text
        if (oxygenText != null)
        {
            oxygenText.text = "Oxygen: " + Mathf.RoundToInt(currentOxygen) + "%";
        }
        
        // Update color
        if (oxygenBarFill != null)
        {
            float oxygenPercent = currentOxygen / maxOxygen;
            
            if (oxygenPercent > 0.6f)
            {
                oxygenBarFill.color = highOxygenColor;
            }
            else if (oxygenPercent > 0.3f)
            {
                oxygenBarFill.color = mediumOxygenColor;
            }
            else
            {
                oxygenBarFill.color = lowOxygenColor;
            }
        }
    }
    
    void UpdateBreathingSound()
    {
        if (breathingSound == null) return;
        
        float oxygenPercent = currentOxygen / maxOxygen;
        
        // Switch to labored breathing when oxygen is low
        if (oxygenPercent < 0.3f)
        {
            if (labordBreathing != null && breathingSound.clip != labordBreathing)
            {
                breathingSound.clip = labordBreathing;
                breathingSound.Play();
            }
        }
        else
        {
            if (normalBreathing != null && breathingSound.clip != normalBreathing)
            {
                breathingSound.clip = normalBreathing;
                breathingSound.Play();
            }
        }
    }
    
    void HandleSuffocation()
    {
        // Implement your suffocation logic here
        // Examples:
        // - Fade screen to black
        // - Show game over screen
        // - Reset level
        // - Apply damage to player
        
        Debug.Log("HANDLE SUFFOCATION - Implement your game over logic here!");
    }
    
    // Public methods for external access
    public float GetCurrentOxygen()
    {
        return currentOxygen;
    }
    
    public float GetOxygenPercentage()
    {
        return (currentOxygen / maxOxygen) * 100f;
    }
    
    public bool IsOxygenLow()
    {
        return currentOxygen <= warningThreshold;
    }
    
    public bool HasSuffocated()
    {
        return currentOxygen <= suffocationThreshold;
    }
    
    public int GetProtectingClothCount()
    {
        return protectingCloths.Count;
    }
    
    // Optional: Reset oxygen (for testing or respawn)
    [ContextMenu("Reset Oxygen")]
    public void ResetOxygen()
    {
        currentOxygen = maxOxygen;
        hasWarned = false;
        hasSuffocated = false;
        protectingCloths.Clear();
        UpdateUI();
    }
    
    // Optional: Instantly deplete oxygen (for testing)
    [ContextMenu("Deplete Oxygen")]
    public void DepleteOxygen()
    {
        currentOxygen = 0;
        UpdateUI();
    }
}