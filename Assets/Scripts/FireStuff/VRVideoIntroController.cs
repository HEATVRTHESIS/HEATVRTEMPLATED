using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Plays a video full-screen in VR before starting level scripts.
/// Uses camera rendering directly - most reliable method.
/// </summary>
[DefaultExecutionOrder(-100)]
public class VRVideoIntroController : MonoBehaviour
{
    [Header("Video Settings")]
    [Tooltip("The video clip to play at the start of the level")]
    public VideoClip introVideo;
    
    [Header("Player Camera")]
    [Tooltip("The VR camera (usually Camera.main)")]
    public Camera playerCamera;
    
    [Header("Playback Settings")]
    [Tooltip("Volume of the video (0-1)")]
    [Range(0f, 1f)]
    public float videoVolume = 1f;
    
    [Header("Fade Settings")]
    [Tooltip("Fade in duration at start")]
    public float fadeInDuration = 0.5f;
    
    [Tooltip("Fade out duration at end")]
    public float fadeOutDuration = 0.5f;
    
    [Header("Game State")]
    [Tooltip("Freeze time during video")]
    public bool freezeTime = true;
    
    [Tooltip("Auto-disable all scripts during video")]
    public bool autoDisableAllScripts = true;
    
    [Tooltip("Script types to keep enabled (class names)")]
    public List<string> scriptExclusionList = new List<string> { "AudioListener", "Camera" };

    [Header("Initialization")]
    [Tooltip("Wait this many frames before starting video (allows scene to initialize)")]
    public int initializationFrameDelay = 3;
    
    // Private variables
    private VideoPlayer videoPlayer;
    private RenderTexture renderTexture;
    private bool videoComplete = false;
    
    // UI Canvas approach - back to basics but done RIGHT
    private GameObject canvas;
    private RawImage videoImage;
    private Image backgroundImage;
    private Image fadeImage;
    
    // State management
    private List<MonoBehaviour> disabledScripts = new List<MonoBehaviour>();
    private Dictionary<MonoBehaviour, bool> scriptOriginalStates = new Dictionary<MonoBehaviour, bool>();
    private float originalTimeScale;
    
    // Camera state
    private CameraClearFlags originalClearFlags;
    private Color originalBackgroundColor;
    private int originalCullingMask;
    
    void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                Debug.LogError("VRVideoIntroController: No camera found!");
                enabled = false;
                return;
            }
        }
    }
    
    void Start()
    {
        if (introVideo == null)
        {
            Debug.LogWarning("VRVideoIntroController: No video assigned. Starting level.");
            return;
        }
        
        StartCoroutine(DelayedVideoIntro());
    }

    private IEnumerator DelayedVideoIntro()
    {
        Debug.Log("VRVideoIntroController: Waiting for scene initialization...");
        
        for (int i = 0; i < initializationFrameDelay; i++)
        {
            yield return null;
        }
        
        FreezeGameState();
        yield return StartCoroutine(PlayVideoIntro());
    }
    
    private void FreezeGameState()
    {
        Debug.Log("VRVideoIntroController: Freezing game state...");
        
        originalTimeScale = Time.timeScale;
        
        if (freezeTime)
        {
            Time.timeScale = 0f;
        }
        
        // Save and modify camera settings to hide scene
        originalClearFlags = playerCamera.clearFlags;
        originalBackgroundColor = playerCamera.backgroundColor;
        originalCullingMask = playerCamera.cullingMask;
        
        // Make camera render nothing but UI
        playerCamera.clearFlags = CameraClearFlags.SolidColor;
        playerCamera.backgroundColor = Color.black;
        playerCamera.cullingMask = 1 << LayerMask.NameToLayer("UI"); // Only render UI layer
        
        if (autoDisableAllScripts)
        {
            MonoBehaviour[] allScripts = FindObjectsOfType<MonoBehaviour>(true);
            
            foreach (MonoBehaviour script in allScripts)
            {
                if (script == this) continue;
                if (!script.enabled) continue;
                
                string scriptType = script.GetType().Name;
                if (scriptExclusionList.Contains(scriptType)) continue;
                
                scriptOriginalStates[script] = script.enabled;
                script.enabled = false;
                disabledScripts.Add(script);
            }
            
            Debug.Log($"Disabled {disabledScripts.Count} scripts");
        }
    }
    
    private void UnfreezeGameState()
    {
        Debug.Log("VRVideoIntroController: Unfreezing game state...");
        
        if (freezeTime)
        {
            Time.timeScale = originalTimeScale;
        }
        
        // Restore camera settings
        playerCamera.clearFlags = originalClearFlags;
        playerCamera.backgroundColor = originalBackgroundColor;
        playerCamera.cullingMask = originalCullingMask;
        
        foreach (MonoBehaviour script in disabledScripts)
        {
            if (script != null && scriptOriginalStates.ContainsKey(script))
            {
                script.enabled = scriptOriginalStates[script];
            }
        }
        
        disabledScripts.Clear();
        scriptOriginalStates.Clear();
        
        Debug.Log("Level started!");
    }
    
    private IEnumerator PlayVideoIntro()
    {
        CreateCanvas();
        SetupVideoPlayer();
        
        // Start with black screen
        fadeImage.color = Color.black;
        
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared)
        {
            yield return null;
        }
        
        Debug.Log($"Playing video: {introVideo.name} ({videoPlayer.length}s)");
        
        videoPlayer.Play();
        
        // Fade in from black
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = 1f - (elapsed / fadeInDuration);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        fadeImage.color = new Color(0, 0, 0, 0);
        
        // Wait for video to complete
        while (videoPlayer.isPlaying && !videoComplete)
        {
            yield return null;
        }
        
        Debug.Log("Video finished playing");
        
        // Fade out to black
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = elapsed / fadeOutDuration;
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        fadeImage.color = Color.black;
        
        CleanupVideo();
        UnfreezeGameState();
        
        // Fade back in
        elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = 1f - (elapsed / fadeInDuration);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        
        if (canvas != null)
        {
            Destroy(canvas);
        }
        
        Debug.Log("Video intro complete!");
    }
    
    private void CreateCanvas()
    {
        canvas = new GameObject("VideoCanvas");
        canvas.layer = LayerMask.NameToLayer("UI");
        
        Canvas c = canvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay; // Simplest mode - always on top
        c.sortingOrder = 32767;
        
        CanvasScaler scaler = canvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvas.AddComponent<GraphicRaycaster>();
        
        // Black background (blocks everything)
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvas.transform, false);
        bgObj.layer = LayerMask.NameToLayer("UI");
        
        backgroundImage = bgObj.AddComponent<Image>();
        backgroundImage.color = Color.black;
        
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        
        // Video display
        GameObject vidObj = new GameObject("Video");
        vidObj.transform.SetParent(canvas.transform, false);
        vidObj.layer = LayerMask.NameToLayer("UI");
        
        videoImage = vidObj.AddComponent<RawImage>();
        videoImage.color = Color.white;
        
        RectTransform vidRect = vidObj.GetComponent<RectTransform>();
        vidRect.anchorMin = Vector2.zero;
        vidRect.anchorMax = Vector2.one;
        vidRect.sizeDelta = Vector2.zero;
        
        // Fade overlay
        GameObject fadeObj = new GameObject("Fade");
        fadeObj.transform.SetParent(canvas.transform, false);
        fadeObj.layer = LayerMask.NameToLayer("UI");
        
        fadeImage = fadeObj.AddComponent<Image>();
        fadeImage.color = Color.black;
        
        RectTransform fadeRect = fadeObj.GetComponent<RectTransform>();
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.sizeDelta = Vector2.zero;
        
        fadeObj.transform.SetAsLastSibling();
        
        Debug.Log("Canvas created with ScreenSpaceOverlay");
    }
    
    private void SetupVideoPlayer()
    {
        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.clip = introVideo;
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        videoPlayer.SetDirectAudioVolume(0, videoVolume);
        
        renderTexture = new RenderTexture(
            (int)introVideo.width,
            (int)introVideo.height,
            0
        );
        videoPlayer.targetTexture = renderTexture;
        videoImage.texture = renderTexture;
        
        videoPlayer.loopPointReached += OnVideoComplete;
    }
    
    private void OnVideoComplete(VideoPlayer vp)
    {
        videoComplete = true;
    }
    
    private void CleanupVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoComplete;
            videoPlayer.Stop();
            Destroy(videoPlayer);
        }
        
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }
    
    void OnDestroy()
    {
        CleanupVideo();
        if (canvas != null)
        {
            Destroy(canvas);
        }
    }
}