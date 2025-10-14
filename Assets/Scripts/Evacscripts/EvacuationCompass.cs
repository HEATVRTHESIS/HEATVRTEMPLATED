using UnityEngine;
using UnityEngine.UI;

public class EvacuationCompass : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the EvacuationLevelManager in the scene")]
    public EvacuationLevelManager levelManager;
    
    [Tooltip("Reference to the XR Camera transform (Main Camera in XR Rig)")]
    public Transform xrCamera;
    
    [Header("XR Canvas Compass")]
    [Tooltip("Image component for XR canvas compass arrow")]
    public Image compassImage;
    
    [Tooltip("The world-space canvas containing the compass")]
    public Canvas compassCanvas;
    
    [Header("Canvas Positioning (World Space)")]
    [Tooltip("Keep canvas at fixed distance in front of camera")]
    public bool followCamera = true;
    
    [Tooltip("Distance from camera")]
    public float distanceFromCamera = 1.5f;
    
    [Tooltip("Offset from center (right/left, up/down, forward/back)")]
    public Vector3 canvasOffset = new Vector3(0.3f, -0.2f, 0);
    
    [Tooltip("Keep canvas upright (locked to world Y-axis)")]
    public bool lockCanvasUpright = true;
    
    [Header("Compass Settings")]
    [Tooltip("Update frequency in seconds")]
    public float updateInterval = 0.1f;
    
    [Tooltip("Smooth rotation speed for compass arrow")]
    public float rotationSmoothSpeed = 10f;
    
    [Header("Optional: Distance Display")]
    [Tooltip("Text component to show distance (optional)")]
    public Text distanceText;
    
    [Tooltip("Text component to show door name (optional)")]
    public Text doorNameText;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    public bool showDebugLine = true;
    
    private Transform closestDoorTransform;
    private float updateTimer;
    private string closestDoorName = "";
    private float distanceToClosestDoor = 0f;
    private float targetCompassAngle = 0f;
    private float currentCompassAngle = 0f;
    
    void Start()
    {
        // Auto-find references if not assigned
        if (levelManager == null)
        {
            levelManager = FindObjectOfType<EvacuationLevelManager>();
            if (levelManager == null)
            {
                Debug.LogError("EvacuationCompass: Could not find EvacuationLevelManager in scene!");
                enabled = false;
                return;
            }
        }
        
        if (xrCamera == null)
        {
            // Try to find main camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                xrCamera = mainCam.transform;
            }
            else
            {
                Debug.LogWarning("EvacuationCompass: XR Camera not assigned! Please assign your XR Camera.");
            }
        }
        
        // Setup canvas for world space if not already configured
        if (compassCanvas != null)
        {
            compassCanvas.renderMode = RenderMode.WorldSpace;
            
            // Set reasonable default size for XR
            RectTransform canvasRect = compassCanvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                canvasRect.sizeDelta = new Vector2(300, 300);
                compassCanvas.transform.localScale = Vector3.one * 0.001f; // Scale down for XR
            }
        }
        
        // Initial update
        FindClosestEnabledDoor();
        
        if (compassCanvas != null && xrCamera != null)
        {
            PositionCanvas();
        }
    }
    
    void Update()
    {
        if (xrCamera == null) return;
        
        updateTimer += Time.deltaTime;
        
        // Update closest door periodically
        if (updateTimer >= updateInterval)
        {
            FindClosestEnabledDoor();
            updateTimer = 0f;
        }
        
        // Position canvas to follow camera if enabled
        if (followCamera && compassCanvas != null)
        {
            PositionCanvas();
        }
        
        // Update compass direction every frame for smooth rotation
        if (closestDoorTransform != null && compassImage != null)
        {
            UpdateCompassDirection();
        }
        
        // Update UI text if available
        UpdateUIText();
    }
    
    void PositionCanvas()
    {
        // Position canvas in front of camera with offset
        Vector3 targetPosition = xrCamera.position + 
                                (xrCamera.forward * distanceFromCamera) +
                                (xrCamera.right * canvasOffset.x) +
                                (xrCamera.up * canvasOffset.y) +
                                (xrCamera.forward * canvasOffset.z);
        
        compassCanvas.transform.position = targetPosition;
        
        // Rotate canvas to face camera
        if (lockCanvasUpright)
        {
            // Face camera but stay upright (only rotate on Y axis)
            Vector3 directionToCamera = xrCamera.position - compassCanvas.transform.position;
            directionToCamera.y = 0; // Keep level
            
            if (directionToCamera != Vector3.zero)
            {
                compassCanvas.transform.rotation = Quaternion.LookRotation(-directionToCamera);
            }
        }
        else
        {
            // Face camera completely
            compassCanvas.transform.LookAt(xrCamera);
            compassCanvas.transform.Rotate(0, 180, 0); // Flip to face camera
        }
    }
    
    void FindClosestEnabledDoor()
    {
        if (levelManager == null || xrCamera == null)
            return;
        
        float closestDistance = float.MaxValue;
        Transform newClosestDoor = null;
        string newClosestDoorName = "";
        
        // Check all evacuation doors
        foreach (EvacuationDoor door in levelManager.allEvacuationDoors)
        {
            // Only consider enabled doors
            if (door.doorCollider != null && door.doorCollider.enabled)
            {
                float distance = Vector3.Distance(
                    xrCamera.position, 
                    door.doorCollider.transform.position
                );
                
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    newClosestDoor = door.doorCollider.transform;
                    newClosestDoorName = door.doorName;
                }
            }
        }
        
        closestDoorTransform = newClosestDoor;
        closestDoorName = newClosestDoorName;
        distanceToClosestDoor = closestDistance;
        
        if (showDebugInfo)
        {
            if (closestDoorTransform != null)
            {
                Debug.Log($"Closest door: {closestDoorName} at {distanceToClosestDoor:F1}m");
            }
            else
            {
                Debug.Log("No enabled evacuation doors found!");
            }
        }
    }
    
    void UpdateCompassDirection()
    {
        // Calculate direction to target (ignore height for compass)
        Vector3 directionToTarget = closestDoorTransform.position - xrCamera.position;
        directionToTarget.y = 0; // Flatten to horizontal plane
        
        if (directionToTarget.magnitude < 0.1f)
            return;
        
        // Get camera's forward direction (also flattened)
        Vector3 cameraForward = xrCamera.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();
        
        // Calculate angle between camera forward and target direction
        targetCompassAngle = Vector3.SignedAngle(cameraForward, directionToTarget, Vector3.up);
        
        // Smooth the rotation
        currentCompassAngle = Mathf.LerpAngle(currentCompassAngle, targetCompassAngle, 
                                              Time.deltaTime * rotationSmoothSpeed);
        
        // Apply rotation to compass image (negative because UI rotates clockwise)
        compassImage.rectTransform.localRotation = Quaternion.Euler(0, 0, -currentCompassAngle);
    }
    
    void UpdateUIText()
    {
        if (distanceText != null)
        {
            if (closestDoorTransform != null)
            {
                distanceText.text = $"{distanceToClosestDoor:F1}m";
            }
            else
            {
                distanceText.text = "---";
            }
        }
        
        if (doorNameText != null)
        {
            if (closestDoorTransform != null)
            {
                doorNameText.text = closestDoorName;
            }
            else
            {
                doorNameText.text = "No Exit Found";
            }
        }
    }
    
    // Public methods for other scripts to use
    
    /// <summary>
    /// Get the name of the closest enabled evacuation door
    /// </summary>
    public string GetClosestDoorName()
    {
        return closestDoorName;
    }
    
    /// <summary>
    /// Get the distance to the closest enabled evacuation door
    /// </summary>
    public float GetDistanceToClosestDoor()
    {
        return distanceToClosestDoor;
    }
    
    /// <summary>
    /// Get the transform of the closest enabled evacuation door
    /// </summary>
    public Transform GetClosestDoorTransform()
    {
        return closestDoorTransform;
    }
    
    /// <summary>
    /// Force an immediate update of the closest door
    /// </summary>
    public void ForceUpdate()
    {
        FindClosestEnabledDoor();
    }
    
    /// <summary>
    /// Toggle canvas visibility
    /// </summary>
    public void SetCompassVisible(bool visible)
    {
        if (compassCanvas != null)
        {
            compassCanvas.gameObject.SetActive(visible);
        }
    }
    
    // Debug visualization
    void OnDrawGizmos()
    {
        if (!showDebugLine || closestDoorTransform == null || xrCamera == null)
            return;
        
        // Draw line from camera to closest door
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(xrCamera.position, closestDoorTransform.position);
        
        // Draw sphere at closest door
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(closestDoorTransform.position, 1f);
        
        // Draw canvas position
        if (compassCanvas != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(compassCanvas.transform.position, 0.1f);
        }
    }
}