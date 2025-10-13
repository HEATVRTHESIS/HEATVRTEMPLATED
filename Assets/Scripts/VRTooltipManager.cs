using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VRTooltipManager : MonoBehaviour
{
    [Header("System Control")]
    [Tooltip("When disabled, no tooltips will be created or shown")]
    public bool tooltipsEnabled = true;
    
    [Header("Tooltip Prefab")]
    public GameObject tooltipPrefab;
    
    [Header("Prefab Component References (Optional - for precise control)")]
    [Tooltip("If set, will use this specific Image component from the prefab instead of auto-detecting")]
    public string buttonImageObjectName = "ButtonIcon";
    [Tooltip("If set, will use this specific Text component from the prefab instead of auto-detecting")]  
    public string textObjectName = "DescriptionText";
    
    [Header("Camera Settings")]
    public Transform cameraTransform;
    public float distanceFromObject = 0.5f;
    public float heightOffset = 0.3f;
    
    [Header("Follow Settings")]
    public float followSpeed = 5f;
    public bool smoothFollow = true;
    
    [Header("Stability Settings")]
    public float creationCooldown = 0.2f;
    
    private GameObject activeTooltip;
    private Transform targetObject;
    private Image tooltipImage;
    private TextMeshProUGUI tooltipText;
    private float lastCreationTime;
    
    private void Start()
    {
        // Auto-find camera if not assigned
        if (cameraTransform == null)
        {
            // Try Camera.main first
            cameraTransform = Camera.main?.transform;
            
            // If not found, look for any active camera
            if (cameraTransform == null)
            {
                Camera[] cameras = FindObjectsOfType<Camera>();
                foreach (Camera cam in cameras)
                {
                    if (cam.enabled && cam.gameObject.activeInHierarchy)
                    {
                        cameraTransform = cam.transform;
                        break;
                    }
                }
            }
            
            // Final fallback: look for MainCamera tag
            if (cameraTransform == null)
            {
                GameObject mainCam = GameObject.FindWithTag("MainCamera");
                if (mainCam != null)
                {
                    cameraTransform = mainCam.transform;
                }
            }
        }
    }
    
    private void Update()
    {
        // Destroy tooltip if system is disabled
        if (!tooltipsEnabled && activeTooltip != null)
        {
            DestroyTooltip();
            return;
        }
        
        if (activeTooltip != null && targetObject != null && cameraTransform != null)
        {
            UpdateTooltipPosition();
        }
    }
    
    public void CreateTooltip(Transform target, Sprite controlImage, string description)
    {
        // Don't create tooltips if system is disabled
        if (!tooltipsEnabled)
        {
            return;
        }
        
        // Prevent rapid creation/destruction
        if (Time.time - lastCreationTime < creationCooldown)
        {
            return;
        }
        
        // Don't create if already showing tooltip for same object
        if (activeTooltip != null && targetObject == target)
        {
            return;
        }
        
        lastCreationTime = Time.time;
        
        // Destroy existing tooltip first
        DestroyTooltip();
        
        if (tooltipPrefab == null)
        {
            Debug.LogError("Tooltip prefab is not assigned!");
            return;
        }
        
        // Instantiate tooltip
        activeTooltip = Instantiate(tooltipPrefab);
        targetObject = target;
        
        // Make the tooltip ignore raycasts to prevent hover interference
        Canvas tooltipCanvas = activeTooltip.GetComponent<Canvas>();
        if (tooltipCanvas != null)
        {
            tooltipCanvas.sortingOrder = 1000; // Ensure it renders on top
        }
        
        // Disable raycast on all UI elements in the tooltip
        Graphic[] graphics = activeTooltip.GetComponentsInChildren<Graphic>();
        foreach (Graphic graphic in graphics)
        {
            graphic.raycastTarget = false;
        }
        
        // Try to find components by specific names first (most reliable)
        if (!string.IsNullOrEmpty(buttonImageObjectName))
        {
            Transform buttonImageTransform = activeTooltip.transform.Find(buttonImageObjectName);
            if (buttonImageTransform == null)
            {
                // Try recursive search
                buttonImageTransform = FindChildByName(activeTooltip.transform, buttonImageObjectName);
            }
            
            if (buttonImageTransform != null)
            {
                tooltipImage = buttonImageTransform.GetComponent<Image>();
                Debug.Log($"Found button image by name: {buttonImageObjectName}");
            }
        }
        
        if (!string.IsNullOrEmpty(textObjectName))
        {
            Transform textTransform = activeTooltip.transform.Find(textObjectName);
            if (textTransform == null)
            {
                textTransform = FindChildByName(activeTooltip.transform, textObjectName);
            }
            
            if (textTransform != null)
            {
                tooltipText = textTransform.GetComponent<TextMeshProUGUI>();
                Debug.Log($"Found text by name: {textObjectName}");
            }
        }
        
        // Fallback to auto-detection if not found by name
        if (tooltipImage == null || tooltipText == null)
        {
            Image[] images = activeTooltip.GetComponentsInChildren<Image>();
            TextMeshProUGUI[] texts = activeTooltip.GetComponentsInChildren<TextMeshProUGUI>();
            
            // Find the button image by excluding the root/panel images
            if (tooltipImage == null)
            {
                foreach (Image img in images)
                {
                    // Skip images that are likely backgrounds
                    string objName = img.gameObject.name.ToLower();
                    bool isProbablyBackground = objName.Contains("panel") || 
                                              objName.Contains("background") || 
                                              objName.Contains("bg") ||
                                              img.transform == activeTooltip.transform || 
                                              img.transform.parent == activeTooltip.transform; 
                    
                    if (!isProbablyBackground)
                    {
                        tooltipImage = img;
                        Debug.Log($"Auto-found button image on: {img.gameObject.name}");
                        break;
                    }
                }
            }
            
            // Get the text component
            if (tooltipText == null && texts.Length > 0)
            {
                tooltipText = texts[0];
            }
        }
        
        // Set content
        if (tooltipImage != null && controlImage != null)
        {
            tooltipImage.sprite = controlImage;
        }
        
        if (tooltipText != null)
        {
            tooltipText.text = description;
        }
        
        // Initial position
        UpdateTooltipPosition();
        
        Debug.Log($"Tooltip created for {target.name}: {description}");
    }
    
    public void DestroyTooltip()
    {
        if (activeTooltip != null)
        {
            Destroy(activeTooltip);
            activeTooltip = null;
            targetObject = null;
            tooltipImage = null;
            tooltipText = null;
        }
    }
    
    /// <summary>
    /// Enable or disable the tooltip system at runtime
    /// </summary>
    public void SetTooltipsEnabled(bool enabled)
    {
        tooltipsEnabled = enabled;
        
        // Immediately destroy active tooltip if disabling
        if (!enabled && activeTooltip != null)
        {
            DestroyTooltip();
        }
    }
    
    private void UpdateTooltipPosition()
    {
        if (activeTooltip == null || targetObject == null || cameraTransform == null)
            return;
        
        // Calculate position above the target object
        Vector3 basePosition = targetObject.position + Vector3.up * heightOffset;
        
        // Calculate direction from camera to target
        Vector3 cameraToTarget = (basePosition - cameraTransform.position).normalized;
        
        // Position tooltip at a distance from the object towards the camera
        Vector3 tooltipPosition = basePosition - cameraToTarget * distanceFromObject;
        
        // Apply position (with optional smoothing)
        if (smoothFollow)
        {
            activeTooltip.transform.position = Vector3.Lerp(
                activeTooltip.transform.position, 
                tooltipPosition, 
                followSpeed * Time.deltaTime
            );
        }
        else
        {
            activeTooltip.transform.position = tooltipPosition;
        }
        
        // Make tooltip face the camera
        Vector3 lookDirection = cameraTransform.position - activeTooltip.transform.position;
        lookDirection.y = 0; // Keep it upright, only rotate on Y-axis
        
        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            
            if (smoothFollow)
            {
                activeTooltip.transform.rotation = Quaternion.Slerp(
                    activeTooltip.transform.rotation, 
                    targetRotation, 
                    followSpeed * Time.deltaTime
                );
            }
            else
            {
                activeTooltip.transform.rotation = targetRotation;
            }
        }
    }
    
    public bool IsTooltipActive()
    {
        return activeTooltip != null;
    }
    
    public void UpdateTooltipContent(Sprite newImage, string newText)
    {
        if (activeTooltip == null) return;
        
        if (tooltipImage != null && newImage != null)
        {
            tooltipImage.sprite = newImage;
        }
        
        if (tooltipText != null)
        {
            tooltipText.text = newText;
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