using UnityEngine;

[System.Serializable]
public class SpawnPointData
{
    public string spawnPointName = "Spawn Point";
    public TeleportationType teleportType = TeleportationType.SameScene;
    public string targetSceneName = "";
    public string targetEntryPointName = ""; // NEW: For specific entry points
    public Transform targetTransform;
    public Vector3 targetPosition;
    public Vector3 targetRotation;
    public float fadeTime = 1.0f;
    
    [Header("Visual Feedback")]
    public Color triggerColor = Color.cyan;
    public bool showVisualEffect = true;
}

public enum TeleportationType
{
    SameScene,      // Teleport within same scene
    NewScene        // Load new scene
}