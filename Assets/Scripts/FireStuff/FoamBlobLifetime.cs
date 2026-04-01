using UnityEngine;

namespace Ignis
{
    /// <summary>
    /// Scales and fades a foam blob over time, then destroys it.
    /// </summary>
    public class FoamBlobLifetime : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private float lifeTime = 6f;
        private float fadeDuration = 3f;
        private Vector3 startScale = Vector3.one;
        private Vector3 endScale = Vector3.one * 0.25f;
        private bool shouldDrip = false;

        private float spawnedAt;
        private Vector3 spawnPosition;
        private Renderer cachedRenderer;
        private MaterialPropertyBlock propertyBlock;
        private bool supportsColorFade;
        private int colorPropertyId = -1;
        private Color baseColor = Color.white;

        public void Initialize(float life, float fade, Vector3 start, Vector3 end, bool drip = false)
        {
            lifeTime = Mathf.Max(0.1f, life);
            fadeDuration = Mathf.Clamp(fade, 0f, lifeTime);
            startScale = start;
            endScale = end;
            shouldDrip = drip;
            spawnedAt = Time.time;
            spawnPosition = transform.position;
            transform.localScale = startScale;

            if (cachedRenderer == null)
            {
                cachedRenderer = GetComponentInChildren<Renderer>();
                propertyBlock = new MaterialPropertyBlock();
                CacheColorProperty();
            }

            SetAlpha(1f);
        }

        private void Update()
        {
            float age = Time.time - spawnedAt;
            float t = Mathf.Clamp01(age / lifeTime);
            
            // Scale down over time
            transform.localScale = Vector3.Lerp(startScale, endScale, t);

            // Handle dripping on vertical surfaces
            if (shouldDrip)
            {
                // Drip down for first 60% of lifetime
                float dripDuration = lifeTime * 0.6f;
                if (age < dripDuration)
                {
                    float dripT = age / dripDuration;
                    float dripAmount = Mathf.Lerp(0f, 0.15f, dripT); // Drip down up to 0.15 units
                    transform.position = spawnPosition + Vector3.down * dripAmount;
                }
            }

            // Fade out - make it strong and visible
            if (fadeDuration > 0f && supportsColorFade)
            {
                float fadeStart = lifeTime - fadeDuration;
                if (age >= fadeStart)
                {
                    float fadeT = 1f - Mathf.Clamp01((age - fadeStart) / fadeDuration);
                    SetAlpha(fadeT);
                }
            }

            if (age >= lifeTime)
            {
                Destroy(gameObject);
            }
        }

        private void CacheColorProperty()
        {
            if (cachedRenderer == null || cachedRenderer.sharedMaterial == null)
            {
                return;
            }

            Material mat = cachedRenderer.sharedMaterial;
            if (mat.HasProperty(BaseColorId))
            {
                supportsColorFade = true;
                colorPropertyId = BaseColorId;
                baseColor = mat.GetColor(BaseColorId);
            }
            else if (mat.HasProperty(ColorId))
            {
                supportsColorFade = true;
                colorPropertyId = ColorId;
                baseColor = mat.GetColor(ColorId);
            }
        }

        private void SetAlpha(float alpha01)
        {
            if (!supportsColorFade || cachedRenderer == null)
            {
                return;
            }

            cachedRenderer.GetPropertyBlock(propertyBlock);
            Color c = baseColor;
            c.a = Mathf.Clamp01(alpha01); // Direct alpha, not multiplied
            propertyBlock.SetColor(colorPropertyId, c);
            cachedRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
