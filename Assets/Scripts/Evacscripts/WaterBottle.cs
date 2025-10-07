using UnityEngine;


[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class WaterBottle : MonoBehaviour
{
    [Header("Water Settings")]
    [Tooltip("The particle system that emits water")]
    public ParticleSystem waterParticles;
    
    [Tooltip("Angle (in degrees) the bottle needs to be tilted to pour water")]
    [Range(0, 180)]
    public float pourAngleThreshold = 45f;
    
    [Header("Water Amount")]
    [Tooltip("Total amount of water in the bottle")]
    public float maxWaterAmount = 100f;
    
    [Tooltip("How much water depletes per second while pouring")]
    public float waterDrainRate = 10f;
    
    private float currentWaterAmount;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private bool isPouring = false;
    private ParticleSystem.EmissionModule emission;
    
    void Start()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        currentWaterAmount = maxWaterAmount;
        
        if (waterParticles != null)
        {
            emission = waterParticles.emission;
            emission.enabled = false;
            waterParticles.Stop();
        }
        else
        {
            Debug.LogError("Water Particles not assigned on WaterBottle!");
        }
    }
    
    void Update()
    {
        // Only check pour angle if bottle is being held
        if (grabInteractable.isSelected && currentWaterAmount > 0)
        {
            CheckPourAngle();
        }
        else
        {
            StopPouring();
        }
        
        // Drain water while pouring
        if (isPouring && currentWaterAmount > 0)
        {
            currentWaterAmount -= waterDrainRate * Time.deltaTime;
            currentWaterAmount = Mathf.Max(0, currentWaterAmount);
            
            // Stop pouring if water runs out
            if (currentWaterAmount <= 0)
            {
                StopPouring();
            }
        }
    }
    
    void CheckPourAngle()
    {
        // Get the bottle's up direction
        Vector3 bottleUp = transform.up;
        
        // Calculate angle from vertical (world up)
        float angle = Vector3.Angle(bottleUp, Vector3.up);
        
        // If tilted beyond threshold, start pouring
        if (angle > pourAngleThreshold)
        {
            if (!isPouring)
            {
                StartPouring();
            }
        }
        else
        {
            if (isPouring)
            {
                StopPouring();
            }
        }
    }
    
    void StartPouring()
    {
        if (waterParticles != null && currentWaterAmount > 0)
        {
            isPouring = true;
            emission.enabled = true;
            waterParticles.Play();
            Debug.Log("Started pouring water");
        }
    }
    
    void StopPouring()
    {
        if (waterParticles != null && isPouring)
        {
            isPouring = false;
            emission.enabled = false;
            waterParticles.Stop();
            Debug.Log("Stopped pouring water");
        }
    }
    
    public float GetWaterPercentage()
    {
        return (currentWaterAmount / maxWaterAmount) * 100f;
    }
    
    public bool HasWater()
    {
        return currentWaterAmount > 0;
    }
    
    // Optional: Refill method
    public void RefillWater()
    {
        currentWaterAmount = maxWaterAmount;
    }
}