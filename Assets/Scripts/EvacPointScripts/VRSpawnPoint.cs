using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Linq;

public class VRSpawnPoint : MonoBehaviour
{
    [Header("Spawn Point Configuration")]
    public SpawnPointData spawnData;
    
    [Header("VR Settings")]
    public LayerMask playerLayer = -1;
    public float triggerRadius = 2f;
    public float startupDelay = 2f; // NEW: Prevent immediate teleporting
    
    [Header("Visual Effects")]
    public GameObject visualEffectPrefab;
    public ParticleSystem teleportParticles;
    
    private bool isPlayerInside = false;
    private bool isTeleporting = false;
    private bool isReady = false; // NEW: Track if spawn point is ready
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
        
        // Start the ready timer
        StartCoroutine(StartupDelayTimer());
    }
    
    IEnumerator StartupDelayTimer()
    {
        isReady = false;
        Debug.Log($"Spawn point '{spawnData.spawnPointName}' will be ready in {startupDelay} seconds...");
        
        yield return new WaitForSeconds(startupDelay);
        
        isReady = true;
        Debug.Log($"Spawn point '{spawnData.spawnPointName}' is now ready!");
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
        // Check if fade canvas already exists (from previous scene)
        GameObject existingFade = GameObject.Find("FadeCanvas");
        if (existingFade != null)
        {
            fadeCanvas = existingFade.GetComponent<CanvasGroup>();
            if (fadeCanvas != null)
            {
                Debug.Log("Using existing fade canvas");
                return;
            }
        }
        
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
        Debug.Log("Created new fade canvas");
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
        color.a = 0.1f; // Much more transparent (was 0.3f)
        mat.color = color;
        renderer.material = mat;
        
        // Remove collider from visual
        Destroy(visual.GetComponent<Collider>());
        
        visualEffectPrefab = visual;
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (IsPlayerObject(other.gameObject) && !isTeleporting && isReady)
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
        #if UNITY_EDITOR
        // Skip haptics in editor to avoid warnings
        Debug.Log("Haptic feedback triggered (disabled in editor)");
        #else
        // Enhanced haptic feedback for XR Interaction Toolkit
        try
        {
            // Method 1: XR Input Devices (Universal)
            var leftController = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
            var rightController = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
            
            if (leftController.isValid)
                leftController.SendHapticImpulse(0, 0.3f, 0.2f);
            if (rightController.isValid)
                rightController.SendHapticImpulse(0, 0.3f, 0.2f);
            
            // Method 2: Try XR Interaction Toolkit controllers if available
            var controllers = FindObjectsOfType<MonoBehaviour>().Where(mb => 
                mb.GetType().Name.Contains("XRController") || 
                mb.GetType().Name.Contains("ActionBasedController"));
                
            foreach (var controller in controllers)
            {
                // Try to trigger haptics via reflection (safe fallback)
                var hapticMethod = controller.GetType().GetMethod("SendHapticImpulse");
                if (hapticMethod != null)
                {
                    hapticMethod.Invoke(controller, new object[] { 0.3f, 0.2f });
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.Log("Haptic feedback not available: " + e.Message);
        }
        #endif
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
        
        // Set entry point if specified
        if (!string.IsNullOrEmpty(spawnData.targetEntryPointName))
        {
            SceneEntryManager.SetRequestedEntryPoint(spawnData.targetEntryPointName);
        }
        
        // Set flag to prevent double fade
        PlayerPrefs.SetInt("JustTeleported", 1);
        
        // Store target position before scene load
        Vector3 storedTargetPosition = spawnData.targetPosition;
        Vector3 storedTargetRotation = spawnData.targetRotation;
        bool hasTargetPosition = storedTargetPosition != Vector3.zero;
        
        // Load new scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(spawnData.targetSceneName);
        
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        
        Debug.Log($"Loaded scene: {spawnData.targetSceneName}");
        
        // IMPORTANT: Re-find the fade canvas in the new scene
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame(); // Extra frame for stability
        
        // Re-establish fade canvas reference
        GameObject fadeCanvasGO = GameObject.Find("FadeCanvas");
        if (fadeCanvasGO != null)
        {
            fadeCanvas = fadeCanvasGO.GetComponent<CanvasGroup>();
            Debug.Log("Re-found fade canvas in new scene");
        }
        else
        {
            Debug.LogWarning("Fade canvas not found in new scene! Creating emergency fade out.");
            // Create emergency fade canvas and immediately fade out
            CreateFadeCanvas();
            if (fadeCanvas != null)
            {
                fadeCanvas.alpha = 1f; // Start black
            }
        }
        
        // Re-find VR components
        FindVRComponents();
        
        // Position player in new scene if target position is specified AND no entry point name
        if (hasTargetPosition && player != null && string.IsNullOrEmpty(spawnData.targetEntryPointName))
        {
            Debug.Log($"Positioning player at target position: {storedTargetPosition}");
            
            // Disable character controller if present
            if (playerController != null)
            {
                playerController.enabled = false;
            }
            
            // Move player to target position
            player.transform.position = storedTargetPosition;
            player.transform.rotation = Quaternion.Euler(storedTargetRotation);
            
            Debug.Log($"Player positioned at: {player.transform.position}");
            
            // Re-enable character controller
            if (playerController != null)
            {
                playerController.enabled = true;
            }
        }
        else
        {
            Debug.Log($"Using default spawn position. HasTargetPos: {hasTargetPosition}, HasPlayer: {player != null}, EntryPoint: {spawnData.targetEntryPointName}");
        }
    }
    
    IEnumerator FadeScreen(float startAlpha, float endAlpha, float duration)
    {
        if (fadeCanvas == null) yield break;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime; // USE UNSCALED TIME
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