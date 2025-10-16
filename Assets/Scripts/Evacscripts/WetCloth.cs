using UnityEngine;


[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class WetCloth : MonoBehaviour
{
    [Header("Cloth State")]
    [Tooltip("Is the cloth currently wet?")]
    public bool isWet = false;
    
    [Header("Wetting Settings")]
    [Tooltip("How many water particle hits needed to make cloth wet")]
    public int hitsNeededToWet = 10;
    
    [Tooltip("How long the cloth stays wet (in seconds)")]
    public float wetDuration = 60f;
    
    [Header("Face Detection")]
    [Tooltip("Reference to the player's head/camera transform")]
    public Transform playerHead;
    
    [Tooltip("Distance from face to be considered 'covering mouth'")]
    public float faceProximityDistance = 0.3f;
    
    [Header("Visual Feedback")]
    [Tooltip("Material when cloth is dry")]
    public Material dryMaterial;
    
    [Tooltip("Material when cloth is wet")]
    public Material wetMaterial;
    
    [Header("References")]
    [Tooltip("Reference to the OxygenManager")]
    public OxygenManager oxygenManager;
    
    private int currentParticleHits = 0;
    private float wetTimer = 0f;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private Renderer clothRenderer;
    private bool isNearFace = false;
    
    void Start()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        clothRenderer = GetComponent<Renderer>();
        
        // Find OxygenManager if not assigned
        if (oxygenManager == null)
        {
            oxygenManager = FindObjectOfType<OxygenManager>();
        }
        
        // Find player head if not assigned
        if (playerHead == null)
        {
            // Try to find the XR camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                playerHead = mainCam.transform;
            }
            else
            {
                Debug.LogWarning("Player head not assigned and couldn't find main camera!");
            }
        }
        
        UpdateClothAppearance();
    }
    
    void Update()
    {
        // Check if cloth is near face
        CheckFaceProximity();
        
        // Handle wet timer countdown
        if (isWet && wetTimer > 0)
        {
            wetTimer -= Time.deltaTime;
            
            if (wetTimer <= 0)
            {
                MakeClothDry();
            }
        }
        
        // Notify oxygen manager of THIS cloth's state
        if (oxygenManager != null)
        {
            bool shouldProtect = isNearFace && isWet;
            oxygenManager.RegisterClothProtection(this, shouldProtect);
        }
    }
    
    void CheckFaceProximity()
    {
        if (playerHead == null) return;
        
        // Calculate distance to player's head
        float distance = Vector3.Distance(transform.position, playerHead.position);
        
        // Check if cloth is close enough to face
        isNearFace = distance <= faceProximityDistance;
    }
    
    // This is called by Unity's Particle System when a particle collides with THIS object
    void OnParticleCollision(GameObject other)
    {
        // No tag check needed - this method only fires when particles hit THIS specific object
        ParticleSystem ps = other.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            currentParticleHits++;
            Debug.Log(gameObject.name + " hit by water particle! Total hits: " + currentParticleHits + "/" + hitsNeededToWet);
            
            if (currentParticleHits >= hitsNeededToWet && !isWet)
            {
                MakeClothWet();
            }
        }
    }
    
    void MakeClothWet()
    {
        isWet = true;
        wetTimer = wetDuration;
        currentParticleHits = 0;
        UpdateClothAppearance();
        Debug.Log(gameObject.name + " is now WET!");
    }
    
    void MakeClothDry()
    {
        isWet = false;
        wetTimer = 0f;
        currentParticleHits = 0;
        UpdateClothAppearance();
        Debug.Log(gameObject.name + " has dried out");
    }
    
    void UpdateClothAppearance()
    {
        if (clothRenderer != null)
        {
            if (isWet && wetMaterial != null)
            {
                clothRenderer.material = wetMaterial;
            }
            else if (!isWet && dryMaterial != null)
            {
                clothRenderer.material = dryMaterial;
            }
        }
    }
    
    public bool IsNearFace()
    {
        return isNearFace;
    }
    
    public bool IsWet()
    {
        return isWet;
    }
    
    public float GetWetTimeRemaining()
    {
        return wetTimer;
    }
    
    // Optional: Force wet the cloth (for testing)
    [ContextMenu("Force Wet Cloth")]
    public void ForceWet()
    {
        MakeClothWet();
    }
    
    // Optional: Force dry the cloth (for testing)
    [ContextMenu("Force Dry Cloth")]
    public void ForceDry()
    {
        MakeClothDry();
    }
    
    // Visualize face proximity in editor
    void OnDrawGizmos()
    {
        if (playerHead != null)
        {
            Gizmos.color = isNearFace ? Color.green : Color.red;
            Gizmos.DrawWireSphere(playerHead.position, faceProximityDistance);
            Gizmos.DrawLine(transform.position, playerHead.position);
        }
    }
}