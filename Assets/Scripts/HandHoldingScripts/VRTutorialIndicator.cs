using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Component for the tutorial indicator prefab.
/// Displays a downward arrow with text above it.
/// </summary>
public class VRTutorialIndicator : MonoBehaviour
{
    [Header("Component References")]
    [Tooltip("The TextMeshProUGUI component for the instruction text")]
    public TextMeshProUGUI instructionText;
    
    [Tooltip("The Image component for the arrow (optional - for customization)")]
    public Image arrowImage;
    
    [Header("Auto-Find Settings")]
    [Tooltip("If text is not assigned, will search for object with this name")]
    public string textObjectName = "InstructionText";
    
    [Tooltip("If arrow is not assigned, will search for object with this name")]
    public string arrowObjectName = "Arrow";
    
    [Header("Visual Settings")]
    [Tooltip("Default text color")]
    public Color textColor = Color.white;
    
    [Tooltip("Default arrow color")]
    public Color arrowColor = Color.yellow;
    
    [Header("Animation (Optional)")]
    [Tooltip("Enable bobbing animation")]
    public bool enableBobbing = true;
    
    [Tooltip("How much to bob up and down")]
    public float bobbingAmount = 0.1f;
    
    [Tooltip("How fast to bob")]
    public float bobbingSpeed = 2f;
    
    private Vector3 initialLocalPosition;
    private float bobbingTimer = 0f;
    
    private void Start()
    {
        // Auto-find components if not assigned
        if (instructionText == null && !string.IsNullOrEmpty(textObjectName))
        {
            Transform textTransform = transform.Find(textObjectName);
            if (textTransform == null)
            {
                textTransform = FindChildByName(transform, textObjectName);
            }
            
            if (textTransform != null)
            {
                instructionText = textTransform.GetComponent<TextMeshProUGUI>();
            }
        }
        
        if (arrowImage == null && !string.IsNullOrEmpty(arrowObjectName))
        {
            Transform arrowTransform = transform.Find(arrowObjectName);
            if (arrowTransform == null)
            {
                arrowTransform = FindChildByName(transform, arrowObjectName);
            }
            
            if (arrowTransform != null)
            {
                arrowImage = arrowTransform.GetComponent<Image>();
            }
        }
        
        // Fallback to GetComponentInChildren if still not found
        if (instructionText == null)
        {
            instructionText = GetComponentInChildren<TextMeshProUGUI>();
        }
        
        if (arrowImage == null)
        {
            // Find first Image that's not a background
            Image[] images = GetComponentsInChildren<Image>();
            foreach (Image img in images)
            {
                string objName = img.gameObject.name.ToLower();
                bool isProbablyBackground = objName.Contains("panel") || 
                                          objName.Contains("background") || 
                                          objName.Contains("bg");
                
                if (!isProbablyBackground)
                {
                    arrowImage = img;
                    break;
                }
            }
        }
        
        // Apply colors
        if (instructionText != null)
        {
            instructionText.color = textColor;
        }
        
        if (arrowImage != null)
        {
            arrowImage.color = arrowColor;
        }
        
        // Store initial position for bobbing
        initialLocalPosition = transform.localPosition;
    }
    
    private void Update()
    {
        if (enableBobbing)
        {
            bobbingTimer += Time.deltaTime * bobbingSpeed;
            float offset = Mathf.Sin(bobbingTimer) * bobbingAmount;
            transform.localPosition = initialLocalPosition + new Vector3(0, offset, 0);
        }
    }
    
    /// <summary>
    /// Set the instruction text
    /// </summary>
    public void SetText(string text)
    {
        if (instructionText != null)
        {
            instructionText.text = text;
        }
        else
        {
            Debug.LogWarning("[VRTutorialIndicator] No TextMeshProUGUI component found!");
        }
    }
    
    /// <summary>
    /// Set the text color
    /// </summary>
    public void SetTextColor(Color color)
    {
        textColor = color;
        if (instructionText != null)
        {
            instructionText.color = color;
        }
    }
    
    /// <summary>
    /// Set the arrow color
    /// </summary>
    public void SetArrowColor(Color color)
    {
        arrowColor = color;
        if (arrowImage != null)
        {
            arrowImage.color = color;
        }
    }
    
    /// <summary>
    /// Set the arrow sprite
    /// </summary>
    public void SetArrowSprite(Sprite sprite)
    {
        if (arrowImage != null)
        {
            arrowImage.sprite = sprite;
        }
    }
    
    /// <summary>
    /// Enable or disable bobbing animation
    /// </summary>
    public void SetBobbingEnabled(bool enabled)
    {
        enableBobbing = enabled;
        if (!enabled)
        {
            transform.localPosition = initialLocalPosition;
        }
    }
    
    private Transform FindChildByName(Transform parent, string name)
    {
        if (parent.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
        {
            return parent;
        }
        
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindChildByName(parent.GetChild(i), name);
            if (result != null)
            {
                return result;
            }
        }
        
        return null;
    }
}
