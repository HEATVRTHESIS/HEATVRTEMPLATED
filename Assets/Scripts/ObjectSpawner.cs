using UnityEngine;
using System.Collections.Generic; // Required for List

public class ObjectSpawner : MonoBehaviour
{
    // A list of the trash prefabs you want to spawn
    public List<GameObject> spawnPrefabs;

    // The total number of objects to spawn
    public int spawnCount = 10;

    // The area where objects will be spawned, represented by a Box Collider
    public BoxCollider spawnArea;

    // Whether to spawn objects automatically when the scene starts
    public bool spawnOnStart = true;

    // Called when the script starts
    void Start()
    {
        if (spawnOnStart)
        {
            SpawnObjects();
        }
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

            // Instantiate the prefab at the random position with no rotation
            Instantiate(prefabToSpawn, randomPos, Quaternion.identity);
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
