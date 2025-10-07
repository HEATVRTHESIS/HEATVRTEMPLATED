using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PathPlane
{
    public string pathName; // e.g., "Path 1", "Path 2", etc.
    public int pathNumber; // 1, 2, 3, 4, 5, 6, 7
    public GameObject planeMesh; // Your path plane mesh
    public Bounds pathBounds; // Will be calculated automatically
    
    [Tooltip("How many obstacles to attempt spawning on this path")]
    public int maxObstaclesOnPath = 5;
}

[System.Serializable]
public class EvacuationDoor
{
    public string doorName; // e.g., "Exit A", "Emergency Exit 1"
    public BoxCollider doorCollider; // The box collider for this door
}

[System.Serializable]
public class SpawnPointConfig
{
    public string departmentName; // e.g., "ER", "MedTech", "Dietary"
    public Transform spawnPoint;
    public List<int> assignedPathNumbers = new List<int>(); // e.g., {1, 2, 3, 4}
    public List<Transform> roadblockPoints = new List<Transform>();
    
    [Header("Evacuation Doors")]
    [Tooltip("Which evacuation doors should be enabled for this department")]
    public List<string> enabledDoorNames = new List<string>(); // e.g., {"Exit A", "Exit C"}
    
    [Range(1, 50)]
    public int totalObstaclesToSpawn = 8;
}

public class EvacuationLevelManager : MonoBehaviour
{
    [Header("Path Setup")]
    [Tooltip("Define all your paths (1-7) here")]
    public List<PathPlane> allPaths = new List<PathPlane>();
    
    [Header("Evacuation Doors")]
    [Tooltip("Define all evacuation doors in your scene")]
    public List<EvacuationDoor> allEvacuationDoors = new List<EvacuationDoor>();
    
    [Header("Spawn Point Configurations")]
    [Tooltip("Define which paths each spawn point uses")]
    public List<SpawnPointConfig> spawnConfigs = new List<SpawnPointConfig>();
    
    [Header("Prefabs")]
    public List<GameObject> obstaclePrefabs = new List<GameObject>();
    public GameObject roadblockPrefab;
    
    [Header("Player Settings")]
    [Tooltip("Reference to the player GameObject already in the scene")]
    public GameObject playerObject;
    
    [Header("Department Selection")]
    [Tooltip("Set this to determine which spawn point to use (e.g., 'ER', 'MedTech', 'Dietary')")]
    public string selectedDepartment = "";
    
    [Header("Generation Settings")]
    public int randomSeed = -1;
    public float minObstacleSpacing = 2f; // Minimum distance between obstacles
    public float minPathClearance = 2.5f; // Must leave this much space for player
    public int maxSpawnAttempts = 50; // Attempts per obstacle
    public LayerMask pathLayerMask = -1; // What layer are your paths on?
    public float obstacleHeightOffset = 0.1f; // Spawn slightly above ground
    public bool debugMode = false; // Shows raycast attempts
    
    private System.Random rng;
    private List<GameObject> spawnedObjects = new List<GameObject>();
    private Dictionary<int, PathPlane> pathLookup = new Dictionary<int, PathPlane>();
    private Dictionary<string, SpawnPointConfig> departmentLookup = new Dictionary<string, SpawnPointConfig>();
    private Dictionary<string, EvacuationDoor> doorLookup = new Dictionary<string, EvacuationDoor>();
    private SpawnPointConfig currentConfig;
    
    void Start()
    {
        CalculatePathBounds();
        BuildDepartmentLookup();
        BuildDoorLookup();
        GenerateLevel();
    }
    
    void CalculatePathBounds()
    {
        foreach (PathPlane path in allPaths)
        {
            if (path.planeMesh != null)
            {
                MeshRenderer renderer = path.planeMesh.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    path.pathBounds = renderer.bounds;
                }
                else
                {
                    // Try getting collider bounds
                    Collider col = path.planeMesh.GetComponent<Collider>();
                    if (col != null)
                        path.pathBounds = col.bounds;
                }
            }
        }
    }
    
    void BuildPathLookup()
    {
        pathLookup.Clear();
        foreach (PathPlane path in allPaths)
        {
            pathLookup[path.pathNumber] = path;
        }
    }
    
    void BuildDepartmentLookup()
    {
        departmentLookup.Clear();
        foreach (SpawnPointConfig config in spawnConfigs)
        {
            if (!string.IsNullOrEmpty(config.departmentName))
            {
                departmentLookup[config.departmentName] = config;
            }
        }
    }
    
    void BuildDoorLookup()
    {
        doorLookup.Clear();
        foreach (EvacuationDoor door in allEvacuationDoors)
        {
            if (!string.IsNullOrEmpty(door.doorName))
            {
                doorLookup[door.doorName] = door;
            }
        }
    }
    
    void ConfigureEvacuationDoors(SpawnPointConfig config)
    {
        // First, disable all door colliders (meshes stay visible)
        foreach (EvacuationDoor door in allEvacuationDoors)
        {
            if (door.doorCollider != null)
            {
                door.doorCollider.enabled = false;
            }
        }
        
        // Then enable only the colliders specified for this department
        int doorsEnabled = 0;
        foreach (string doorName in config.enabledDoorNames)
        {
            if (doorLookup.ContainsKey(doorName))
            {
                EvacuationDoor door = doorLookup[doorName];
                if (door.doorCollider != null)
                {
                    door.doorCollider.enabled = true;
                    doorsEnabled++;
                }
                else
                {
                    Debug.LogWarning($"Door '{doorName}' has no collider assigned!");
                }
            }
            else
            {
                Debug.LogWarning($"Door '{doorName}' not found in evacuation doors list!");
            }
        }
        
        Debug.Log($"Enabled {doorsEnabled} evacuation door colliders for {config.departmentName}");
    }
    
    void PositionPlayer(SpawnPointConfig config)
    {
        if (playerObject == null)
        {
            Debug.LogWarning("Player object not assigned!");
            return;
        }
        
        playerObject.transform.position = config.spawnPoint.position;
        playerObject.transform.rotation = config.spawnPoint.rotation;
        Debug.Log($"Player positioned at {config.departmentName}");
    }
    
    public Transform GetCurrentSpawnPoint()
    {
        return currentConfig?.spawnPoint;
    }
    
    /// <summary>
    /// Sets which department configuration to use for the evacuation level.
    /// Call this before GenerateLevel() if you want to change departments.
    /// </summary>
    public void SetDepartment(string departmentName)
    {
        selectedDepartment = departmentName;
    }
    
    public void GenerateLevel()
    {
        ClearLevel();
        BuildPathLookup();
        BuildDepartmentLookup();
        BuildDoorLookup();
        
        if (randomSeed == -1)
            rng = new System.Random();
        else
            rng = new System.Random(randomSeed);
        
        // Find the selected spawn point configuration
        SpawnPointConfig selectedConfig = null;
        
        if (!string.IsNullOrEmpty(selectedDepartment) && departmentLookup.ContainsKey(selectedDepartment))
        {
            selectedConfig = departmentLookup[selectedDepartment];
        }
        else if (spawnConfigs.Count > 0)
        {
            // Fallback to first config if no valid department selected
            selectedConfig = spawnConfigs[0];
            Debug.LogWarning($"Department '{selectedDepartment}' not found. Using default: {selectedConfig.departmentName}");
        }
        else
        {
            Debug.LogError("No spawn point configurations available!");
            return;
        }
        
        currentConfig = selectedConfig;
        Debug.Log($"Selected spawn point: {selectedConfig.departmentName}");
        Debug.Log($"Using paths: {string.Join(", ", selectedConfig.assignedPathNumbers)}");
        
        // Configure evacuation doors for this department
        ConfigureEvacuationDoors(selectedConfig);
        
        // Position player at selected spawn point
        if (playerObject != null)
        {
            PositionPlayer(selectedConfig);
        }
        
        // Generate obstacles
        GenerateObstaclesForConfig(selectedConfig);
        
        // Place roadblocks
        PlaceRoadblocks(selectedConfig);
    }
    
    void GenerateObstaclesForConfig(SpawnPointConfig config)
    {
        if (obstaclePrefabs.Count == 0)
        {
            Debug.LogWarning("No obstacle prefabs assigned!");
            return;
        }
        
        int obstaclesPlaced = 0;
        List<PathPlane> assignedPaths = new List<PathPlane>();
        
        // Get assigned paths
        foreach (int pathNum in config.assignedPathNumbers)
        {
            if (pathLookup.ContainsKey(pathNum))
            {
                assignedPaths.Add(pathLookup[pathNum]);
            }
        }
        
        if (assignedPaths.Count == 0)
        {
            Debug.LogWarning("No valid paths assigned!");
            return;
        }
        
        // Distribute obstacles across paths
        for (int i = 0; i < config.totalObstaclesToSpawn; i++)
        {
            // Pick a random path
            PathPlane randomPath = assignedPaths[rng.Next(assignedPaths.Count)];
            
            // Try to find a valid position
            Vector3? spawnPos = FindValidSpawnPosition(randomPath);
            
            if (spawnPos.HasValue)
            {
                // Pick random obstacle
                GameObject obstaclePrefab = obstaclePrefabs[rng.Next(obstaclePrefabs.Count)];
                
                // Spawn it
                GameObject obstacle = Instantiate(obstaclePrefab, spawnPos.Value, 
                    Quaternion.Euler(0, (float)(rng.NextDouble() * 360), 0));
                obstacle.transform.parent = this.transform;
                spawnedObjects.Add(obstacle);
                
                obstaclesPlaced++;
            }
        }
        
        Debug.Log($"Successfully placed {obstaclesPlaced}/{config.totalObstaclesToSpawn} obstacles");
    }
    
    Vector3? FindValidSpawnPosition(PathPlane path)
    {
        if (path.planeMesh == null)
        {
            if (debugMode) Debug.LogWarning($"Path {path.pathNumber} has no planeMesh assigned!");
            return null;
        }
        
        Bounds bounds = path.pathBounds;
        
        if (debugMode) Debug.Log($"Attempting to spawn on Path {path.pathNumber}, Bounds: {bounds}");
        
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            // Generate random point within path bounds
            float randomX = bounds.min.x + (float)(rng.NextDouble() * bounds.size.x);
            float randomZ = bounds.min.z + (float)(rng.NextDouble() * bounds.size.z);
            
            Vector3 testPoint = new Vector3(randomX, bounds.center.y + 10f, randomZ);
            
            // Raycast down to find surface
            RaycastHit hit;
            if (Physics.Raycast(testPoint, Vector3.down, out hit, 20f, pathLayerMask))
            {
                if (debugMode) Debug.DrawLine(testPoint, hit.point, Color.green, 5f);
                
                // Check if hit our path
                if (hit.collider.gameObject == path.planeMesh || 
                    hit.collider.transform.IsChildOf(path.planeMesh.transform) ||
                    hit.collider.transform.parent == path.planeMesh.transform)
                {
                    Vector3 spawnPos = hit.point + Vector3.up * obstacleHeightOffset;
                    
                    // Check spacing from other obstacles
                    if (IsPositionValid(spawnPos))
                    {
                        if (debugMode) Debug.Log($"Found valid position at {spawnPos}");
                        return spawnPos;
                    }
                }
                else
                {
                    if (debugMode) Debug.Log($"Hit wrong object: {hit.collider.gameObject.name}");
                }
            }
            else
            {
                if (debugMode) Debug.DrawLine(testPoint, testPoint + Vector3.down * 20f, Color.red, 5f);
            }
        }
        
        if (debugMode) Debug.LogWarning($"Could not find valid position on Path {path.pathNumber} after {maxSpawnAttempts} attempts");
        return null; // Couldn't find valid position
    }
    
    bool IsPositionValid(Vector3 position)
    {
        // Check distance from existing obstacles
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
            {
                float distance = Vector3.Distance(obj.transform.position, position);
                if (distance < minObstacleSpacing)
                    return false;
            }
        }
        
        return true;
    }
    
    void PlaceRoadblocks(SpawnPointConfig config)
    {
        if (roadblockPrefab == null)
            return;
        
        foreach (Transform roadblockPoint in config.roadblockPoints)
        {
            GameObject roadblock = Instantiate(roadblockPrefab, roadblockPoint.position, 
                roadblockPoint.rotation);
            roadblock.transform.parent = this.transform;
            spawnedObjects.Add(roadblock);
        }
        
        Debug.Log($"Placed {config.roadblockPoints.Count} roadblocks");
    }
    
    public void ClearLevel()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        spawnedObjects.Clear();
    }
    
    [ContextMenu("Recalculate Path Bounds")]
    public void RecalculatePathBounds()
    {
        CalculatePathBounds();
        Debug.Log("Path bounds recalculated!");
    }
    
    [ContextMenu("Regenerate Level")]
    public void RegenerateLevel()
    {
        GenerateLevel();
    }
    
    // Visualize path bounds in editor
    void OnDrawGizmosSelected()
    {
        foreach (PathPlane path in allPaths)
        {
            if (path.planeMesh != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(path.pathBounds.center, path.pathBounds.size);
            }
        }
        
        // Show spawned obstacle positions
        Gizmos.color = Color.red;
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
            {
                Gizmos.DrawWireSphere(obj.transform.position, minObstacleSpacing / 2);
            }
        }
        
        // Show evacuation door positions
        Gizmos.color = Color.green;
        foreach (EvacuationDoor door in allEvacuationDoors)
        {
            if (door.doorCollider != null)
            {
                Bounds doorBounds = door.doorCollider.bounds;
                Gizmos.color = door.doorCollider.enabled ? Color.green : Color.gray;
                Gizmos.DrawWireCube(doorBounds.center, doorBounds.size);
            }
        }
    }
}