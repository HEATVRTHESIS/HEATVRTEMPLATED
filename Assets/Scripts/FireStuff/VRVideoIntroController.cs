using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Plays a video full-screen in VR before starting level scripts.
/// Uses a Canvas overlay to display video in VR (both eyes).
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
    
    // Private variables
    private VideoPlayer videoPlayer;
    private RenderTexture renderTexture;
    private bool videoComplete = false;
    
    // UI Elements
    private GameObject videoCanvas;
    private RawImage videoImage;
    private Image fadeImage;
    
    // State management
    private List<MonoBehaviour> disabledScripts = new List<MonoBehaviour>();
    private Dictionary<MonoBehaviour, bool> scriptOriginalStates = new Dictionary<MonoBehaviour, bool>();
    private float originalTimeScale;
    
    void Awake()
    {
        // Find camera if not assigned
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
        
        // Freeze game state before other scripts run
        FreezeGameState();
    }
    
    void Start()
    {
        // Check if video is assigned
        if (introVideo == null)
        {
            Debug.LogWarning("VRVideoIntroController: No video assigned. Starting level.");
            UnfreezeGameState();
            return;
        }
        
        // Play video
        StartCoroutine(PlayVideoIntro());
    }
    
    private void FreezeGameState()
    {
        Debug.Log("VRVideoIntroController: Freezing game state...");
        
        originalTimeScale = Time.timeScale;
        
        if (freezeTime)
        {
            Time.timeScale = 0f;
        }
        
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
        
        foreach (MonoBehaviour script in disabledScripts)
        {
            if (script != null && scriptOriginalStates.ContainsKey(script))
            {
                script.enabled = scriptOriginalStates[script];
            }
        }
        
        Debug.Log("Level started!");
    }
    
    private IEnumerator PlayVideoIntro()
    {
        // Create UI canvas
        CreateVideoCanvas();
        
        // Setup video player
        SetupVideoPlayer();
        
        // Start with black screen
        fadeImage.color = Color.black;
        
        // Wait for video to prepare
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared)
        {
            yield return null;
        }
        
        Debug.Log($"Playing video: {introVideo.name} ({videoPlayer.length}s)");
        
        // Play video
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
        
        // Cleanup
        CleanupVideo();
        
        // Unfreeze game
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
        
        // Destroy canvas
        if (videoCanvas != null)
        {
            Destroy(videoCanvas);
        }
        
        Debug.Log("Video intro complete!");
    }
    
    private void CreateVideoCanvas()
    {
        // Create canvas - CRITICAL: Use ScreenSpaceCamera for VR to render in both eyes
        videoCanvas = new GameObject("VideoIntroCanvas");
        Canvas canvas = videoCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = playerCamera; // Assign the VR camera
        canvas.planeDistance = 1f; // Distance from camera (adjust if needed)
        canvas.sortingOrder = 999;
        
        CanvasScaler scaler = videoCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        videoCanvas.AddComponent<GraphicRaycaster>();
        
        // Create video display
        GameObject videoObj = new GameObject("VideoDisplay");
        videoObj.transform.SetParent(videoCanvas.transform, false);
        
        videoImage = videoObj.AddComponent<RawImage>();
        videoImage.color = Color.white;
        
        RectTransform videoRect = videoObj.GetComponent<RectTransform>();
        videoRect.anchorMin = Vector2.zero;
        videoRect.anchorMax = Vector2.one;
        videoRect.sizeDelta = Vector2.zero;
        videoRect.anchoredPosition = Vector2.zero;
        
        // Create fade overlay
        GameObject fadeObj = new GameObject("FadeOverlay");
        fadeObj.transform.SetParent(videoCanvas.transform, false);
        
        fadeImage = fadeObj.AddComponent<Image>();
        fadeImage.color = Color.black;
        
        RectTransform fadeRect = fadeObj.GetComponent<RectTransform>();
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.sizeDelta = Vector2.zero;
        fadeRect.anchoredPosition = Vector2.zero;
        
        // Put fade on top
        fadeObj.transform.SetAsLastSibling();
    }
    
    private void SetupVideoPlayer()
    {
        // Create video player
        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.clip = introVideo;
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        videoPlayer.SetDirectAudioVolume(0, videoVolume);
        
        // Create render texture
        renderTexture = new RenderTexture(
            (int)introVideo.width,
            (int)introVideo.height,
            0
        );
        videoPlayer.targetTexture = renderTexture;
        
        // Assign to UI
        videoImage.texture = renderTexture;
        
        // Subscribe to events
        videoPlayer.loopPointReached += OnVideoComplete;
    }
    
    private void OnVideoComplete(VideoPlayer vp)
    {
        videoComplete = true;
        Debug.Log("Video reached end");
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
        
        if (videoCanvas != null)
        {
            Destroy(videoCanvas);
        }
    }
}