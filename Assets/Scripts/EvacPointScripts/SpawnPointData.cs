using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[System.Serializable]
public class SpawnPointData
{
    public string spawnPointName = "Spawn Point";
    public TeleportationType teleportType = TeleportationType.SameScene;
    public string targetSceneName = "";
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

public class VRSpawnPoint : MonoBehaviour
{
    [Header("Spawn Point Configuration")]
    public SpawnPointData spawnData;
    
    [Header("VR Settings")]
    public LayerMask playerLayer = -1;
    public float triggerRadius = 2f;
    
    [Header("Visual Effects")]
    public GameObject visualEffectPrefab;
    public ParticleSystem teleportParticles;
    
    private bool isPlayerInside = false;
    private bool isTeleporting = false;
    private GameObject player;
    private CharacterController playerController;
    private Camera vrCamera;
    private CanvasGroup fadeCanvas;
    
    // Events for other systems to hook into
    public System.Action<string> OnTeleportStart;
    public System.Action<string> OnTeleportComplete;
    
    void Start()
    {
        SetupSpawnPoint();
        FindVRComponents();
        CreateFadeCanvas();
    }
    
    void SetupSpawnPoint()
    {
        // Create trigger collider
        SphereCollider trigger = GetComponent<SphereCollider>();
        if (trigger == null)
        {
            trigger = gameObject.AddComponent<SphereCollider>();
        }
        
        trigger.isTrigger = true;
        trigger.radius = triggerRadius;
        
        // Visual setup
        if (spawnData.showVisualEffect && visualEffectPrefab == null)
        {
            CreateDefaultVisual();
        }
    }
    
    void FindVRComponents()
    {
        // Find VR player components - works with any VR setup
        player = GameObject.FindWithTag("Player");
        
        // Try finding by common GameObject names if Player tag not found
        if (player == null)
        {
            player = GameObject.Find("XR Rig") ?? 
                     GameObject.Find("XRRig") ?? 
                     GameObject.Find("XR Origin") ??
                     GameObject.Find("XROrigin") ??
                     GameObject.Find("Player") ??
                     GameObject.Find("[CameraRig]") ?? // SteamVR
                     GameObject.Find("OVRCameraRig") ?? // Oculus
                     GameObject.Find("VRPlayer") ??
                     GameObject.Find("Camera Rig");
        }
        
        // Try finding by component types (works with most VR systems)
        if (player == null)
        {
            // Look for CharacterController first
            CharacterController cc = FindObjectOfType<CharacterController>();
            if (cc != null)
                player = cc.gameObject;
        }
        
        // Last resort: find object with Camera component that's likely VR
        if (player == null)
        {
            Camera[] cameras = FindObjectsOfType<Camera>();
            foreach (Camera cam in cameras)
            {
                // Look for cameras that are likely VR (not UI cameras, etc.)
                if (cam.gameObject.name.ToLower().Contains("vr") || 
                    cam.gameObject.name.ToLower().Contains("xr") ||
                    cam.gameObject.name.ToLower().Contains("rig") ||
                    cam.gameObject.name.ToLower().Contains("player"))
                {
                    player = cam.transform.root.gameObject; // Get root object
                    break;
                }
            }
        }
        
        if (player != null)
        {
            playerController = player.GetComponent<CharacterController>();
            
            // Find VR camera
            vrCamera = Camera.main;
            if (vrCamera == null)
            {
                // Look for camera in player hierarchy
                vrCamera = player.GetComponentInChildren<Camera>();
            }
            if (vrCamera == null)
            {
                vrCamera = FindObjectOfType<Camera>();
            }
            
            Debug.Log($"Found VR Player: {player.name}");
        }
        else
        {
            Debug.LogWarning("VR Player not found! Please assign the 'Player' tag to your VR rig, or manually set the player reference.");
        }
    }
    
    void CreateFadeCanvas()
    {
        // Create fade to black canvas for smooth transitions
        GameObject fadeGO = new GameObject("FadeCanvas");
        Canvas canvas = fadeGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        
        fadeCanvas = fadeGO.AddComponent<CanvasGroup>();
        fadeCanvas.alpha = 0f;
        fadeCanvas.blocksRaycasts = false;
        
        // Create black panel
        GameObject panel = new GameObject("FadePanel");
        panel.transform.SetParent(fadeGO.transform);
        
        UnityEngine.UI.Image image = panel.AddComponent<UnityEngine.UI.Image>();
        image.color = Color.black;
        
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        
        DontDestroyOnLoad(fadeGO);
    }
    
    void CreateDefaultVisual()
    {
        // Create a simple visual indicator
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visual.transform.SetParent(transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = new Vector3(triggerRadius * 2, 0.1f, triggerRadius * 2);
        
        Renderer renderer = visual.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = spawnData.triggerColor;
        mat.SetFloat("_Mode", 3); // Transparent mode
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        
        Color color = spawnData.triggerColor;
        color.a = 0.3f;
        mat.color = color;
        renderer.material = mat;
        
        // Remove collider from visual
        Destroy(visual.GetComponent<Collider>());
        
        visualEffectPrefab = visual;
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (IsPlayerObject(other.gameObject) && !isTeleporting)
        {
            isPlayerInside = true;
            OnPlayerEnterTrigger();
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (IsPlayerObject(other.gameObject))
        {
            isPlayerInside = false;
            OnPlayerExitTrigger();
        }
    }
    
    bool IsPlayerObject(GameObject obj)
    {
        // Check if object is on player layer
        return ((1 << obj.layer) & playerLayer) != 0 || obj.CompareTag("Player");
    }
    
    void OnPlayerEnterTrigger()
    {
        Debug.Log($"Player entered spawn point: {spawnData.spawnPointName}");
        
        // Start teleport particles if available
        if (teleportParticles != null)
        {
            teleportParticles.Play();
        }
        
        // Trigger haptic feedback for VR controllers
        TriggerHapticFeedback();
        
        // Start teleportation after brief delay
        StartCoroutine(InitiateTeleport());
    }
    
    void OnPlayerExitTrigger()
    {
        Debug.Log($"Player exited spawn point: {spawnData.spawnPointName}");
        
        if (teleportParticles != null)
        {
            teleportParticles.Stop();
        }
    }
    
    void TriggerHapticFeedback()
    {
        // Add haptic feedback for VR controllers
        // This depends on your VR SDK (XR Toolkit, SteamVR, etc.)
        try
        {
            var leftController = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
            var rightController = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
            
            if (leftController.isValid)
                leftController.SendHapticImpulse(0, 0.3f, 0.2f);
            if (rightController.isValid)
                rightController.SendHapticImpulse(0, 0.3f, 0.2f);
        }
        catch (System.Exception e)
        {
            Debug.Log("Haptic feedback not available: " + e.Message);
        }
    }
    
    IEnumerator InitiateTeleport()
    {
        if (isTeleporting) yield break;
        
        isTeleporting = true;
        OnTeleportStart?.Invoke(spawnData.spawnPointName);
        
        // Fade out
        yield return StartCoroutine(FadeScreen(0f, 1f, spawnData.fadeTime * 0.5f));
        
        // Perform teleportation
        switch (spawnData.teleportType)
        {
            case TeleportationType.SameScene:
                TeleportInSameScene();
                break;
            case TeleportationType.NewScene:
                yield return StartCoroutine(TeleportToNewScene());
                break;
        }
        
        // Wait a frame for everything to settle
        yield return new WaitForEndOfFrame();
        
        // Fade in
        yield return StartCoroutine(FadeScreen(1f, 0f, spawnData.fadeTime * 0.5f));
        
        isTeleporting = false;
        OnTeleportComplete?.Invoke(spawnData.spawnPointName);
    }
    
    void TeleportInSameScene()
    {
        if (player == null) return;
        
        Vector3 targetPos;
        Vector3 targetRot;
        
        if (spawnData.targetTransform != null)
        {
            targetPos = spawnData.targetTransform.position;
            targetRot = spawnData.targetTransform.eulerAngles;
        }
        else
        {
            targetPos = spawnData.targetPosition;
            targetRot = spawnData.targetRotation;
        }
        
        // Disable character controller if present
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        // Move player
        player.transform.position = targetPos;
        player.transform.rotation = Quaternion.Euler(targetRot);
        
        // Re-enable character controller
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        
        Debug.Log($"Teleported to position: {targetPos}");
    }
    
    IEnumerator TeleportToNewScene()
    {
        if (string.IsNullOrEmpty(spawnData.targetSceneName))
        {
            Debug.LogWarning("Target scene name is empty!");
            yield break;
        }
        
        // Load new scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(spawnData.targetSceneName);
        
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        
        Debug.Log($"Loaded scene: {spawnData.targetSceneName}");
        
        // Re-find components in new scene
        yield return new WaitForEndOfFrame();
        FindVRComponents();
        
        // Position player in new scene if target position is specified
        if (spawnData.targetPosition != Vector3.zero && player != null)
        {
            TeleportInSameScene();
        }
    }
    
    IEnumerator FadeScreen(float startAlpha, float endAlpha, float duration)
    {
        if (fadeCanvas == null) yield break;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            fadeCanvas.alpha = alpha;
            yield return null;
        }
        
        fadeCanvas.alpha = endAlpha;
    }
    
    void OnDrawGizmos()
    {
        // Draw trigger area in editor
        Gizmos.color = spawnData.triggerColor;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
        
        // Draw target position if in same scene
        if (spawnData.teleportType == TeleportationType.SameScene)
        {
            Vector3 targetPos = spawnData.targetTransform != null ? 
                spawnData.targetTransform.position : spawnData.targetPosition;
            
            if (targetPos != Vector3.zero)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(targetPos, Vector3.one);
                Gizmos.DrawLine(transform.position, targetPos);
            }
        }
    }
}