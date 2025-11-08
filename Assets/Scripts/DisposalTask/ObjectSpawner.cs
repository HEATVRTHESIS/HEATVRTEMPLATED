using UnityEngine;
using System.Collections.Generic; // Required for List

/// <summary>
/// This script handles the spawning of a list of prefabs within a defined area.
/// Now includes automatic tutorial configuration for spawned objects!
/// </summary>
public class ObjectSpawner : MonoBehaviour
{
    // A list of the trash prefabs you want to spawn
    public List<GameObject> spawnPrefabs;

    // The total number of objects to spawn
    public int spawnCount = 10;

    // The area where objects will be spawned, represented by a Box Collider
    public BoxCollider spawnArea;
    
    [Header("Tutorial Settings")]
    [Tooltip("If spawned prefabs have TutorialTriggerGrabbable, set this as their indicator target (e.g., trash can)")]
    public Transform tutorialIndicatorTarget;
    
    [Tooltip("Override the indicator text for all spawned objects (leave empty to use prefab's default)")]
    public string overrideIndicatorText = "";
    
    [Tooltip("Override the audio message for all spawned objects (leave empty to use prefab's default)")]
    [TextArea(3, 5)]
    public string overrideAudioMessage = "";
    
    // A reference to the transform of the TaskController, which is the parent of this spawner.
    private Transform parentTaskTransform;

    void Awake()
    {
        // Get a reference to the parent TaskController's transform
        // This script is a child of the TaskController.
        parentTaskTransform = this.transform.parent;
    }

    // Public method to manually trigger the spawning process
    public void SpawnObjects()
    {
        // Check if the spawn area and prefabs are set
        if (spawnArea == null)
        {
            Debug.LogError("Spawn Area is not assigned! Please assign a Box Collider to the Spawn Area field.");
            return;
        }

        if (spawnPrefabs == null || spawnPrefabs.Count == 0)
        {
            Debug.LogError("No prefabs assigned to spawn! Please add some prefabs to the Spawn Prefabs list.");
            return;
        }

        // Loop to create the specified number of objects
        for (int i = 0; i < spawnCount; i++)
        {
            // Get a random position within the spawn area's bounds
            Vector3 randomPos = GetRandomPositionInBounds(spawnArea.bounds);

            // Select a random prefab from the list
            GameObject prefabToSpawn = spawnPrefabs[Random.Range(0, spawnPrefabs.Count)];

            // Instantiate the prefab and set the TaskController as its parent.
            GameObject spawnedObject = Instantiate(prefabToSpawn, randomPos, Quaternion.identity, parentTaskTransform);
            
            // Configure tutorial settings if the spawned object has TutorialTriggerGrabbable
            ConfigureTutorialTrigger(spawnedObject);
        }
    }
    
    /// <summary>
    /// Configures the TutorialTriggerGrabbable component on spawned objects
    /// Searches in children in case the component is not on the root object
    /// </summary>
    private void ConfigureTutorialTrigger(GameObject spawnedObject)
    {
        // Try to find TutorialTriggerGrabbable component (including children)
        TutorialTriggerGrabbable tutorialTrigger = spawnedObject.GetComponentInChildren<TutorialTriggerGrabbable>();
        
        if (tutorialTrigger == null)
        {
            // Object doesn't have tutorial trigger, skip
            return;
        }
        
        // Set the indicator target if specified
        if (tutorialIndicatorTarget != null)
        {
            tutorialTrigger.SetIndicatorTarget(tutorialIndicatorTarget);
            Debug.Log($"[ObjectSpawner] Set tutorial indicator target for {spawnedObject.name} to {tutorialIndicatorTarget.name}");
        }
        
        // Override indicator text if specified
        if (!string.IsNullOrEmpty(overrideIndicatorText))
        {
            tutorialTrigger.SetIndicatorText(overrideIndicatorText);
            Debug.Log($"[ObjectSpawner] Set tutorial indicator text for {spawnedObject.name}: {overrideIndicatorText}");
        }
        
        // Override audio message if specified
        if (!string.IsNullOrEmpty(overrideAudioMessage))
        {
            tutorialTrigger.audioMessageTTS = overrideAudioMessage;
            Debug.Log($"[ObjectSpawner] Set tutorial audio message for {spawnedObject.name}");
        }
    }

    // Helper function to get a random position inside a BoxCollider's bounds
    private Vector3 GetRandomPositionInBounds(Bounds bounds)
    {
        float x = Random.Range(bounds.min.x, bounds.max.x);
        float y = Random.Range(bounds.min.y, bounds.max.y);
        float z = Random.Range(bounds.min.z, bounds.max.z);

        return new Vector3(x, y, z);
    }
}