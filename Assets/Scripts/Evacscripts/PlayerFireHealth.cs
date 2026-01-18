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
    
    [Header("Error Dialogue")]
    [Tooltip("The dialogue lines to display on first fire obstacle hit.")]
    public string[] fireObstacleDialogue;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip errorSound;
    
    [Header("Damage Settings")]
    [Tooltip("Cooldown between damage instances (prevents rapid damage)")]
    public float damageCooldown = 1f;
    
    private float lastDamageTime;
    private bool hasPlayedFireObstacleDialogue = false;
    
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

        FireTrigger fireTrigger = other.GetComponent<FireTrigger>();
        if (fireTrigger != null && fireTrigger.flameObj != null && fireTrigger.flameObj.onFire)
        {
            TakeDamage(fireTrigger.flameObj);
            return;
        }

        FlammableObject flammable = other.GetComponent<FlammableObject>();
        if (flammable != null && flammable.onFire)
        {
            TakeDamage(flammable);
            return;
        }

        if (other.transform.parent != null)
        {
            FlammableObject parentFlammable = other.transform.parent.GetComponent<FlammableObject>();
            if (parentFlammable != null && parentFlammable.onFire)
            {
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
        
        if (ErrorTracker.Instance != null)
            ErrorTracker.Instance.RecordEvacuationFireObstacleError();

        if (audioSource != null && errorSound != null)
            audioSource.PlayOneShot(errorSound);

        if (!hasPlayedFireObstacleDialogue)
        {
            hasPlayedFireObstacleDialogue = true;
            VRDialogueSystem dialogueSystem = FindObjectOfType<VRDialogueSystem>();
            if (dialogueSystem != null && fireObstacleDialogue != null && fireObstacleDialogue.Length > 0)
            {
                dialogueSystem.StartDialog(fireObstacleDialogue);
            }
        }
        
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
    }

    void OnPlayerDeath()
    {
        Debug.Log("<color=red>PLAYER DIED FROM FIRE!</color>");
        
        Time.timeScale = 0f;
        
        if (failureCanvas != null)
        {
            failureCanvas.SetActive(true);
        }
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        UpdateHealthDisplay();
        healthImage.color = originalImageColor;
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateHealthDisplay();
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