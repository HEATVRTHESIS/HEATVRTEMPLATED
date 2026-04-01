#if OAVA_IGNIS
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ignis
{
    /// <summary>
    /// Modified ParticleExtinguish with fire class validation
    /// </summary>
    public class ParticleExtinguish : MonoBehaviour
    {
        [Header("Extinguisher Type")]
        [Tooltip("What type of extinguisher is this?")]
        public FireClass extinguisherType = FireClass.ABC;

        [Header("Extinguishing Settings")]
        [Tooltip("How large area can one particle extinguish")]
        public float particleExtinquishRadius = 0.1f;

        [Tooltip("How much the area is incremented if new area is not hit. (simulates water puddling/sliding on ground)")]
        public float incrementalPower = 0.0005f;
        
        [Header("Wrong Extinguisher Feedback")]
        [Tooltip("Popup manager for showing error messages")]
        public PopupManager popupManager;
        
        [Tooltip("Cooldown between wrong extinguisher error messages (seconds)")]
        public float errorMessageCooldown = 3f;

        [Header("Collision Blobs")]
        [Tooltip("Blob prefab to spawn on every particle collision.")]
        public GameObject collisionBlobPrefab;

        [Tooltip("Blob lifetime in seconds.")]
        public float blobLifetime = 2.5f;

        [Tooltip("Blob fade duration.")]
        public float blobFadeDuration = 2.5f;

        [Tooltip("Maximum active blobs at once.")]
        public int maxActiveBlobs = 120;

        [Tooltip("Spawn blob only every Nth collision to reduce spam.")]
        public int spawnEveryNthCollision = 4;

        [Tooltip("Minimum distance between blob spawns on same object.")]
        public float minSpawnDistance = 0.16f;

        [Tooltip("Maximum blobs spawned per OnParticleCollision call.")]
        public int maxSpawnsPerCollisionCall = 3;

        [Tooltip("Side-surface random width/length scale multiplier range.")]
        public Vector2 sideSurfaceScaleRandomRange = new Vector2(0.7f, 1.1f);

        private ParticleSystem part;
        private List<ParticleCollisionEvent> collisionEvents;
        private Dictionary<GameObject, float> lastErrorTime = new Dictionary<GameObject, float>();
        private Queue<GameObject> activeBlobs = new Queue<GameObject>();
        private Dictionary<GameObject, Vector3> lastBlobPosition = new Dictionary<GameObject, Vector3>();
        private int collisionCounter = 0;

        void Start()
        {
            part = GetComponent<ParticleSystem>();
            collisionEvents = new List<ParticleCollisionEvent>();
            Debug.Log($"ParticleExtinguish initialized as {extinguisherType} type");
        }

        void OnParticleCollision(GameObject other)
        {
            int numCollisionEvents = part.GetCollisionEvents(other, collisionEvents);

            int spawnedThisCall = 0;
            for (int i = 0; i < numCollisionEvents; i++)
            {
                if (spawnedThisCall >= Mathf.Max(1, maxSpawnsPerCollisionCall))
                {
                    break;
                }

                if (SpawnBlobAtCollision(other, collisionEvents[i]))
                {
                    spawnedThisCall++;
                }
            }

            // Handle fire extinguishing if this is a flammable object
            Ignis.FlammableObject flamObj = other.GetComponentInParent<Ignis.FlammableObject>();
            if (flamObj)
            {
                FlammableMaterialClassifier classifier = flamObj.GetComponent<FlammableMaterialClassifier>();
                
                if (classifier == null)
                {
                    classifier = flamObj.gameObject.AddComponent<FlammableMaterialClassifier>();
                    classifier.fireClass = FireClass.ABC;
                    Debug.LogWarning($"{flamObj.gameObject.name} has no FlammableMaterialClassifier, defaulting to ABC class");
                }
                
                if (classifier.CanBeExtinguishedBy(extinguisherType))
                {
                    // Correct extinguisher - extinguish the fire
                    for (int i = 0; i < numCollisionEvents; i++)
                    {
                        Vector3 pos = collisionEvents[i].intersection;
                        flamObj.IncrementalExtinguish(pos, particleExtinquishRadius, incrementalPower);
                    }
                }
                else
                {
                    // Wrong extinguisher - show error
                    OnWrongExtinguisherUsed(other, classifier.GetFireClass());
                }
            }
        }
        
        /// <summary>
        /// Called when the wrong extinguisher type is used on a fire
        /// </summary>
        private void OnWrongExtinguisherUsed(GameObject target, FireClass fireClass)
        {
            // Check cooldown to avoid spamming errors
            if (lastErrorTime.ContainsKey(target))
            {
                if (Time.time - lastErrorTime[target] < errorMessageCooldown)
                {
                    return; // Still in cooldown
                }
            }
            
            lastErrorTime[target] = Time.time;
            
            // Track the error in FireScoreTracker
            if (FireScoreTracker.Instance != null)
            {
                FireScoreTracker.Instance.OnWrongExtinguisherType();
                Debug.Log($"Wrong extinguisher error tracked! {extinguisherType} cannot extinguish {fireClass} fire");
            }
            
            // Show feedback message
            string errorMessage = GetErrorMessage(fireClass);
            
            if (popupManager != null)
            {
                popupManager.ShowMessage(errorMessage);
            }
            
            Debug.LogWarning($"Wrong extinguisher used! {extinguisherType} extinguisher cannot put out {fireClass} fires.");
        }
        
        /// <summary>
        /// Get appropriate error message based on fire class
        /// </summary>
        private string GetErrorMessage(FireClass fireClass)
        {
            switch (fireClass)
            {
                case FireClass.K:
                    return "Wrong extinguisher! Use a Class K extinguisher for oil/grease fires!";
                case FireClass.ABC:
                    return "Wrong extinguisher! Use a Class ABC extinguisher for this fire!";
                default:
                    return "Wrong extinguisher type!";
            }
        }

        private bool SpawnBlobAtCollision(GameObject target, ParticleCollisionEvent collisionEvent)
        {
            // Throttle: Only spawn every Nth collision
            collisionCounter++;
            if (collisionCounter % spawnEveryNthCollision != 0)
            {
                return false;
            }

            if (collisionBlobPrefab == null)
            {
                Debug.LogError("collisionBlobPrefab is not assigned!");
                return false;
            }

            Vector3 hitPos = collisionEvent.intersection;
            Vector3 hitNormal = collisionEvent.normal;
            
            // Use up vector if normal is invalid
            if (hitNormal.sqrMagnitude < 0.0001f)
            {
                hitNormal = Vector3.up;
            }

            // Distance throttle: Don't spawn too close to last blob on this surface
            if (lastBlobPosition.TryGetValue(target, out Vector3 lastPos))
            {
                if (Vector3.Distance(hitPos, lastPos) < minSpawnDistance)
                {
                    return false;
                }
            }
            lastBlobPosition[target] = hitPos;

            // Orient blob to surface
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, hitNormal);

            // Spawn at collision point in world space
            GameObject blob = Instantiate(collisionBlobPrefab, hitPos, rot);

            // Detect if this is a vertical/side surface (normal mostly horizontal)
            float verticalComponent = Mathf.Abs(hitNormal.y);
            bool isVerticalSurface = verticalComponent < 0.3f; // Surface is more vertical than horizontal

            // Add lifetime component to scale down and fade
            FoamBlobLifetime life = blob.GetComponent<FoamBlobLifetime>();
            if (life == null)
            {
                life = blob.AddComponent<FoamBlobLifetime>();
            }
            
            // Base scale
            Vector3 blobScale = new Vector3(0.60214f, 0.15289f, 0.7239528f);
            
            // Scale down on vertical surfaces for drip effect
            if (isVerticalSurface)
            {
                float minMul = Mathf.Min(sideSurfaceScaleRandomRange.x, sideSurfaceScaleRandomRange.y);
                float maxMul = Mathf.Max(sideSurfaceScaleRandomRange.x, sideSurfaceScaleRandomRange.y);
                float xMul = Random.Range(minMul, maxMul);
                float zMul = Random.Range(minMul, maxMul);

                // Keep height deterministic, vary only side dimensions to avoid uniform blobs.
                blobScale = new Vector3(blobScale.x * xMul * 0.6f, blobScale.y, blobScale.z * zMul * 0.6f);
            }
            
            Vector3 endScale = blobScale * 0.1f;
            life.Initialize(blobLifetime, blobFadeDuration, blobScale, endScale, isVerticalSurface);

            // Track blob for queue management
            activeBlobs.Enqueue(blob);
            while (activeBlobs.Count > Mathf.Max(1, maxActiveBlobs))
            {
                GameObject oldest = activeBlobs.Dequeue();
                if (oldest != null)
                {
                    Destroy(oldest);
                }
            }

            return true;
        }
    }
}
#endif