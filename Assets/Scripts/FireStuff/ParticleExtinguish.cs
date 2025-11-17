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

        private ParticleSystem part;
        private List<ParticleCollisionEvent> collisionEvents;
        private Dictionary<GameObject, float> lastErrorTime = new Dictionary<GameObject, float>();

        void Start()
        {
            part = GetComponent<ParticleSystem>();
            collisionEvents = new List<ParticleCollisionEvent>();
            Debug.Log($"ParticleExtinguish initialized as {extinguisherType} type");
        }

        void OnParticleCollision(GameObject other)
        {
            int numCollisionEvents = part.GetCollisionEvents(other, collisionEvents);

            Ignis.FlammableObject flamObj = other.GetComponentInParent<Ignis.FlammableObject>();
            if (flamObj)
            {
                // Get the fire class classifier
                FlammableMaterialClassifier classifier = flamObj.GetComponent<FlammableMaterialClassifier>();
                
                if (classifier == null)
                {
                    // No classifier found, add default ABC class
                    classifier = flamObj.gameObject.AddComponent<FlammableMaterialClassifier>();
                    classifier.fireClass = FireClass.ABC;
                    Debug.LogWarning($"{flamObj.gameObject.name} has no FlammableMaterialClassifier, defaulting to ABC class");
                }
                
                // Check if this extinguisher can put out this fire
                if (classifier.CanBeExtinguishedBy(extinguisherType))
                {
                    // Correct extinguisher type - extinguish the fire
                    int i = 0;
                    while (i < numCollisionEvents)
                    {
                        Vector3 pos = collisionEvents[i].intersection;
                        flamObj.IncrementalExtinguish(pos, particleExtinquishRadius, incrementalPower);
                        i++;
                    }
                }
                else
                {
                    // Wrong extinguisher type - block extinguishing and show error
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
    }
}
#endif