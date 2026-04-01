using Ignis;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a world-space fire health indicator above a FlammableObject.
/// The indicator scales down as the fire is extinguished.
/// </summary>
public class FlammableFireHealthIndicator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Flammable object to track. If empty, uses this GameObject.")]
    public FlammableObject flammableObject;

    [Tooltip("Prefab containing a UI Image (world-space UI).")]
    public GameObject indicatorPrefab;

    [Tooltip("Optional camera transform. If empty, this script auto-finds one.")]
    public Transform cameraTransform;

    [Header("Placement")]
    [Tooltip("Vertical offset from the object's bounds center.")]
    public float heightOffset = 0.5f;

    [Tooltip("Smooth follow speed for indicator movement and rotation.")]
    public float followSpeed = 8f;

    [Tooltip("Smoothly move and rotate the indicator.")]
    public bool smoothFollow = true;

    [Tooltip("Keep the indicator upright by rotating only on Y axis.")]
    public bool keepUpright = true;

    [Header("Visibility")]
    [Tooltip("Only show indicator while the object is burning.")]
    public bool showOnlyWhenOnFire = true;

    [Tooltip("Destroy indicator after the object is fully extinguished.")]
    public bool hideWhenExtinguished = false;

    [Header("Scale Behavior")]
    [Tooltip("Optional target transform to scale. If empty, scales the whole indicator instance.")]
    public Transform scaleTarget;

    [Tooltip("Reduce fill in visible chunks instead of fully continuous motion.")]
    public bool useChunkedFill = true;

    [Tooltip("How many chunks the indicator has from full to empty.")]
    [Range(2, 20)]
    public int chunkCount = 10;

    [Tooltip("How much of the bar is consumed before burnout starts. Remaining portion drains during burnout until task completion.")]
    [Range(0.5f, 0.95f)]
    public float preBurnoutWeight = 0.75f;

    [Tooltip("Smoothly animate icon scale toward the target value.")]
    public bool smoothScale = true;

    [Tooltip("How fast the icon shrinks when Smooth Scale is enabled.")]
    public float scaleLerpSpeed = 4f;

    [Tooltip("Minimum visible scale while object is still burning. Prevents instant disappearance.")]
    [Range(0f, 0.25f)]
    public float minVisibleScaleWhileBurning = 0.1f;

    private GameObject activeIndicator;
    private Image fillImage;
    private float currentDisplayedScale = 1f;
    private Transform resolvedScaleTarget;
    private Vector3 initialScale = Vector3.one;

    private void Start()
    {
        if (flammableObject == null)
        {
            flammableObject = GetComponent<FlammableObject>();
        }

        if (flammableObject == null)
        {
            Debug.LogError($"{nameof(FlammableFireHealthIndicator)} requires a FlammableObject reference.", this);
            enabled = false;
            return;
        }

        if (indicatorPrefab == null)
        {
            Debug.LogError($"{nameof(FlammableFireHealthIndicator)}: Indicator Prefab is not assigned.", this);
            enabled = false;
            return;
        }

        ResolveCamera();
    }

    private void Update()
    {
        if (flammableObject == null)
        {
            return;
        }

        if (showOnlyWhenOnFire && !flammableObject.onFire)
        {
            DestroyIndicator();
            return;
        }

        if (hideWhenExtinguished && flammableObject.IsExtinguished() && !flammableObject.onFire)
        {
            DestroyIndicator();
            return;
        }

        if (activeIndicator == null)
        {
            CreateIndicator();
        }

        if (activeIndicator == null)
        {
            return;
        }

        ResolveCamera();
        UpdateIndicatorPosition();
        UpdateIndicatorFill();
    }

    private void OnDisable()
    {
        DestroyIndicator();
    }

    private void OnDestroy()
    {
        DestroyIndicator();
    }

    private void ResolveCamera()
    {
        if (cameraTransform != null)
        {
            return;
        }

        cameraTransform = Camera.main != null ? Camera.main.transform : null;
        if (cameraTransform != null)
        {
            return;
        }

        Camera[] cameras = FindObjectsOfType<Camera>();
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i].enabled && cameras[i].gameObject.activeInHierarchy)
            {
                cameraTransform = cameras[i].transform;
                return;
            }
        }
    }

    private void CreateIndicator()
    {
        activeIndicator = Instantiate(indicatorPrefab);
        fillImage = activeIndicator.GetComponentInChildren<Image>();
        currentDisplayedScale = 1f;
        resolvedScaleTarget = scaleTarget != null ? scaleTarget : activeIndicator.transform;
        initialScale = resolvedScaleTarget.localScale;

        if (fillImage == null)
        {
            Debug.LogWarning($"{nameof(FlammableFireHealthIndicator)}: No Image found on indicator prefab. Fill will not update.", this);
        }
        else
        {
            fillImage.fillAmount = 1f;
        }

        ApplyScale(1f);

        UpdateIndicatorPosition();
        UpdateIndicatorFill();
    }

    private void DestroyIndicator()
    {
        if (activeIndicator != null)
        {
            Destroy(activeIndicator);
            activeIndicator = null;
            fillImage = null;
            resolvedScaleTarget = null;
        }
    }

    private void ApplyScale(float normalizedScale)
    {
        if (resolvedScaleTarget == null)
        {
            return;
        }

        resolvedScaleTarget.localScale = initialScale * Mathf.Clamp01(normalizedScale);
    }

    private void UpdateIndicatorPosition()
    {
        if (activeIndicator == null)
        {
            return;
        }

        Vector3 targetPosition = GetAnchorPosition();

        if (smoothFollow)
        {
            activeIndicator.transform.position = Vector3.Lerp(
                activeIndicator.transform.position,
                targetPosition,
                followSpeed * Time.deltaTime
            );
        }
        else
        {
            activeIndicator.transform.position = targetPosition;
        }

        if (cameraTransform == null)
        {
            return;
        }

        Vector3 lookDirection = cameraTransform.position - activeIndicator.transform.position;
        if (keepUpright)
        {
            lookDirection.y = 0f;
        }

        if (lookDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
        if (smoothFollow)
        {
            activeIndicator.transform.rotation = Quaternion.Slerp(
                activeIndicator.transform.rotation,
                targetRotation,
                followSpeed * Time.deltaTime
            );
        }
        else
        {
            activeIndicator.transform.rotation = targetRotation;
        }
    }

    private Vector3 GetAnchorPosition()
    {
        Renderer[] renderers = flammableObject.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return bounds.center + Vector3.up * (bounds.extents.y + heightOffset);
        }

        return flammableObject.transform.position + Vector3.up * heightOffset;
    }

    private void UpdateIndicatorFill()
    {
        if (fillImage == null || flammableObject == null)
        {
            return;
        }

        if (!flammableObject.onFire)
        {
            currentDisplayedScale = 0f;
            ApplyScale(0f);
            fillImage.fillAmount = 0f;
            return;
        }

        float threshold = flammableObject.GetObjectApproxSize() * flammableObject.fullExtinguishToughness * 0.5f;
        if (threshold <= 0.0001f)
        {
            currentDisplayedScale = 1f;
            ApplyScale(1f);
            return;
        }

        float extinguishProgress = Mathf.Clamp01(flammableObject.GetPutOutRadius() / threshold);

        // Match task completion behavior: success monitor completes only when onFire becomes false.
        // So we reserve part of the bar for the burnout phase (onFireTimer from burnOutStart_s to burnOutStart_s + burnOutLength_s).
        float burnOutProgress = 0f;
        if (flammableObject.onFireTimer > flammableObject.burnOutStart_s)
        {
            float burnOutDuration = Mathf.Max(0.01f, flammableObject.burnOutLength_s);
            burnOutProgress = Mathf.Clamp01((flammableObject.onFireTimer - flammableObject.burnOutStart_s) / burnOutDuration);
        }

        float weightedProgress = (extinguishProgress * preBurnoutWeight) + (burnOutProgress * (1f - preBurnoutWeight));
        float healthRemaining = 1f - Mathf.Clamp01(weightedProgress);

        if (useChunkedFill && chunkCount > 1)
        {
            float stepSize = 1f / chunkCount;
            healthRemaining = Mathf.Ceil(healthRemaining / stepSize) * stepSize;
            healthRemaining = Mathf.Clamp01(healthRemaining);
        }

        healthRemaining = Mathf.Max(healthRemaining, minVisibleScaleWhileBurning);

        if (smoothScale)
        {
            currentDisplayedScale = Mathf.MoveTowards(currentDisplayedScale, healthRemaining, scaleLerpSpeed * Time.deltaTime);
            ApplyScale(currentDisplayedScale);
        }
        else
        {
            currentDisplayedScale = healthRemaining;
            ApplyScale(currentDisplayedScale);
        }

        // Keep the sprite fully visible; progression is communicated by world-space scale.
        fillImage.fillAmount = 1f;
    }
}
