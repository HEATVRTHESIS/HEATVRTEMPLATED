using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Ignis;

public class PlayerFireHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Maximum number of hits player can take")]
    public int maxHealth = 5;
    
    private int currentHealth;
    
    [Header("UI References")]
    [Tooltip("The filled heart image on the watch")]
    public Image healthImage;
    
    [Tooltip("Canvas to show when player dies")]
    public GameObject failureCanvas;
    
    [Header("Damage Settings")]
    [Tooltip("Cooldown between damage instances (prevents rapid damage)")]
    public float damageCooldown = 1f;
    
    private float lastDamageTime;
    
    [Header("Visual Feedback")]
    [Tooltip("Color to flash when taking damage")]
    public Color damageFlashColor = Color.red;
    
    [Tooltip("Duration of damage flash effect")]
    public float flashDuration = 0.2f;
    
    private Color originalImageColor;
    
    [Header("Optional: Haptic Feedback")]
    [Tooltip("Enable haptic feedback on damage")]
    public bool useHaptics = true;
    
    [Tooltip("Haptic intensity (0-1)")]
    [Range(0f, 1f)]
    public float hapticIntensity = 0.5f;
    
    [Tooltip("Haptic duration")]
    public float hapticDuration = 0.2f;

    void Start()
    {
        currentHealth = maxHealth;
        lastDamageTime = -damageCooldown;
        
        if (healthImage != null)
        {
            originalImageColor = healthImage.color;
            UpdateHealthDisplay();
        }
        else
        {
            Debug.LogError("PlayerFireHealth: Health Image not assigned!");
        }
        
        Debug.Log("PlayerFireHealth initialized. Max Health: " + maxHealth);
    }

    void OnTriggerEnter(Collider other)
    {
        CheckForFire(other);
    }

    void OnTriggerStay(Collider other)
    {
        CheckForFire(other);
    }

    void CheckForFire(Collider other)
    {
        if (Time.time - lastDamageTime < damageCooldown)
            return;

        // Check if we hit a FireTrigger
        FireTrigger fireTrigger = other.GetComponent<FireTrigger>();
        if (fireTrigger != null && fireTrigger.flameObj != null && fireTrigger.flameObj.onFire)
        {
            Debug.Log("Hit FireTrigger for: " + fireTrigger.flameObj.gameObject.name);
            TakeDamage(fireTrigger.flameObj);
            return;
        }

        // Check if we hit the FlammableObject directly
        FlammableObject flammable = other.GetComponent<FlammableObject>();
        if (flammable != null && flammable.onFire)
        {
            Debug.Log("Hit FlammableObject: " + flammable.gameObject.name + " OnFire: " + flammable.onFire);
            TakeDamage(flammable);
            return;
        }

        // Check parent for FlammableObject
        if (other.transform.parent != null)
        {
            FlammableObject parentFlammable = other.transform.parent.GetComponent<FlammableObject>();
            if (parentFlammable != null && parentFlammable.onFire)
            {
                Debug.Log("Hit child of FlammableObject: " + parentFlammable.gameObject.name);
                TakeDamage(parentFlammable);
                return;
            }
        }
    }

    void TakeDamage(FlammableObject sourceFlammable = null)
    {
        if (currentHealth <= 0)
            return;

        currentHealth--;
        lastDamageTime = Time.time;
        
        UpdateHealthDisplay();
        StartCoroutine(DamageFlash());
        TriggerHapticFeedback();
        
        string sourceName = sourceFlammable != null ? sourceFlammable.gameObject.name : "Unknown Fire";
        Debug.Log($"<color=red>PLAYER TOOK FIRE DAMAGE from {sourceName}! Health: {currentHealth}/{maxHealth}</color>");
        
        if (currentHealth <= 0)
        {
            OnPlayerDeath();
        }
    }

    void UpdateHealthDisplay()
    {
        if (healthImage == null)
            return;
        
        float fillAmount = (float)currentHealth / maxHealth;
        healthImage.fillAmount = fillAmount;
        
        if (currentHealth <= 1)
        {
            healthImage.color = Color.Lerp(Color.red, originalImageColor, 0.3f);
        }
        else if (currentHealth <= 2)
        {
            healthImage.color = Color.Lerp(Color.yellow, originalImageColor, 0.5f);
        }
        else
        {
            healthImage.color = originalImageColor;
        }
    }

    IEnumerator DamageFlash()
    {
        if (healthImage == null)
            yield break;
        
        Color currentColor = healthImage.color;
        healthImage.color = damageFlashColor;
        
        yield return new WaitForSeconds(flashDuration);
        
        healthImage.color = currentColor;
    }

    void TriggerHapticFeedback()
    {
        if (!useHaptics)
            return;
        
        // Uncomment for XR Interaction Toolkit:
        /*
        UnityEngine.XR.InputDevice leftDevice;
        UnityEngine.XR.InputDevice rightDevice;
        
        if (UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand, out leftDevice))
        {
            leftDevice.SendHapticImpulse(0, hapticIntensity, hapticDuration);
        }
        if (UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand, out rightDevice))
        {
            rightDevice.SendHapticImpulse(0, hapticIntensity, hapticDuration);
        }
        */
    }

    void OnPlayerDeath()
    {
        Debug.Log("<color=red>PLAYER DIED FROM FIRE! Opening failure canvas and stopping time.</color>");
        
        // Stop time
        Time.timeScale = 0f;
        
        // Show the assigned failure canvas
        if (failureCanvas != null)
        {
            failureCanvas.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Failure canvas not assigned to PlayerFireHealth!");
        }
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        UpdateHealthDisplay();
        healthImage.color = originalImageColor;
        Debug.Log("Player health reset!");
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateHealthDisplay();
        Debug.Log($"Player healed! Health: {currentHealth}/{maxHealth}");
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public bool IsAlive()
    {
        return currentHealth > 0;
    }
}