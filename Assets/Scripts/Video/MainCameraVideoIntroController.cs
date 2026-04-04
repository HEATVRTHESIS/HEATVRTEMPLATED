using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Plays a full-screen video on the main camera and raises events when playback starts or ends.
/// Use this as a reusable intro layer before enabling other cutscene controllers.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class MainCameraVideoIntroController : MonoBehaviour
{
    [Header("Video")]
    [Tooltip("Video clip to play.")]
    public VideoClip videoClip;

    [Tooltip("Camera used to display the video. Defaults to Camera.main.")]
    public Camera targetCamera;

    [Tooltip("Volume for direct video audio output.")]
    [Range(0f, 1f)]
    public float videoVolume = 1f;

    [Tooltip("Automatically start playback when the scene starts.")]
    public bool playOnStart = true;

    [Tooltip("Loop the video instead of ending automatically.")]
    public bool loopVideo = false;

    [Header("Fade Settings")]
    [Tooltip("Fade in duration at start.")]
    public float fadeInDuration = 0.5f;

    [Tooltip("Fade out duration at end.")]
    public float fadeOutDuration = 0.5f;

    [Header("Game State")]
    [Tooltip("Freeze time during video.")]
    public bool freezeTime = true;

    [Tooltip("Auto-disable all scripts during video.")]
    public bool autoDisableAllScripts = true;

    [Tooltip("Script types to keep enabled (class names).")]
    public List<string> scriptExclusionList = new List<string> { "AudioListener", "Camera" };

    [Header("Overlay")]
    [Tooltip("Sorting order used for the video canvas.")]
    public int canvasSortingOrder = 999;

    [Tooltip("Distance from the camera for the screen-space canvas.")]
    public float canvasPlaneDistance = 1f;

    [Header("Events")]
    [Tooltip("Invoked right after the video starts playing.")]
    public UnityEvent onVideoStarted = new UnityEvent();

    [Tooltip("Invoked after the video finishes playing.")]
    public UnityEvent onVideoFinished = new UnityEvent();

    [Tooltip("Invoked if playback is skipped manually.")]
    public UnityEvent onVideoSkipped = new UnityEvent();

    private VideoPlayer videoPlayer;
    private RenderTexture renderTexture;
    private GameObject videoCanvas;
    private RawImage videoImage;
    private Image fadeImage;
    private bool videoComplete;
    private bool videoIsPlaying;
    private bool gameStateFrozen;

    private readonly List<MonoBehaviour> disabledScripts = new List<MonoBehaviour>();
    private readonly Dictionary<MonoBehaviour, bool> scriptOriginalStates = new Dictionary<MonoBehaviour, bool>();
    private float originalTimeScale = 1f;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                Debug.LogError($"{nameof(MainCameraVideoIntroController)}: No camera found!");
                enabled = false;
                return;
            }
        }

        if (playOnStart)
        {
            FreezeGameState();
        }
    }

    private void Start()
    {
        if (videoClip == null)
        {
            Debug.LogWarning($"{nameof(MainCameraVideoIntroController)}: No video assigned. Starting level.");
            UnfreezeGameState();
            return;
        }

        if (playOnStart)
        {
            StartCoroutine(PlayVideoIntro());
        }
    }

    public void PlayVideo()
    {
        if (videoIsPlaying)
        {
            return;
        }

        if (videoClip == null)
        {
            Debug.LogWarning($"{nameof(MainCameraVideoIntroController)}: No video assigned.");
            return;
        }

        if (!gameStateFrozen)
        {
            FreezeGameState();
        }

        StartCoroutine(PlayVideoIntro());
    }

    public void SkipVideo()
    {
        if (!videoIsPlaying)
        {
            return;
        }

        onVideoSkipped?.Invoke();
        StopAllCoroutines();
        CleanupVideo();
        UnfreezeGameState();
    }

    public void StopVideo()
    {
        StopAllCoroutines();
        CleanupVideo();
        UnfreezeGameState();
    }

    private IEnumerator PlayVideoIntro()
    {
        videoIsPlaying = true;

        CreateVideoCanvas();
        SetupVideoPlayer();

        if (fadeImage != null)
        {
            fadeImage.color = Color.black;
        }

        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared)
        {
            yield return null;
        }

        Debug.Log($"Playing video: {videoClip.name} ({videoPlayer.length}s)");

        videoPlayer.Play();
        onVideoStarted?.Invoke();

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = 1f - (elapsed / fadeInDuration);
            if (fadeImage != null)
            {
                fadeImage.color = new Color(0, 0, 0, alpha);
            }
            yield return null;
        }
        if (fadeImage != null)
        {
            fadeImage.color = new Color(0, 0, 0, 0);
        }

        while (videoPlayer.isPlaying && !videoComplete)
        {
            yield return null;
        }

        if (loopVideo)
        {
            yield return null;
        }

        Debug.Log("Video finished playing");

        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = elapsed / fadeOutDuration;
            if (fadeImage != null)
            {
                fadeImage.color = new Color(0, 0, 0, alpha);
            }
            yield return null;
        }
        if (fadeImage != null)
        {
            fadeImage.color = Color.black;
        }

        onVideoFinished?.Invoke();

        CleanupVideo();
        UnfreezeGameState();

        elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = 1f - (elapsed / fadeInDuration);
            if (fadeImage != null)
            {
                fadeImage.color = new Color(0, 0, 0, alpha);
            }
            yield return null;
        }

        if (videoCanvas != null)
        {
            Destroy(videoCanvas);
        }

        videoIsPlaying = false;
    }

    private void FreezeGameState()
    {
        if (gameStateFrozen)
        {
            return;
        }

        gameStateFrozen = true;
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
                if (script == null || script == this || !script.enabled)
                {
                    continue;
                }

                string scriptType = script.GetType().Name;
                if (scriptExclusionList.Contains(scriptType))
                {
                    continue;
                }

                scriptOriginalStates[script] = script.enabled;
                script.enabled = false;
                disabledScripts.Add(script);
            }

            Debug.Log($"Disabled {disabledScripts.Count} scripts");
        }
    }

    private void UnfreezeGameState()
    {
        if (!gameStateFrozen)
        {
            return;
        }

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

        disabledScripts.Clear();
        scriptOriginalStates.Clear();
        gameStateFrozen = false;
    }

    private void CreateVideoCanvas()
    {
        videoCanvas = new GameObject("VideoIntroCanvas");
        Canvas canvas = videoCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = targetCamera;
        canvas.planeDistance = canvasPlaneDistance;
        canvas.sortingOrder = canvasSortingOrder;

        CanvasScaler scaler = videoCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        videoCanvas.AddComponent<GraphicRaycaster>();

        GameObject videoObj = new GameObject("VideoDisplay");
        videoObj.transform.SetParent(videoCanvas.transform, false);

        videoImage = videoObj.AddComponent<RawImage>();
        videoImage.color = Color.white;

        RectTransform videoRect = videoObj.GetComponent<RectTransform>();
        videoRect.anchorMin = Vector2.zero;
        videoRect.anchorMax = Vector2.one;
        videoRect.sizeDelta = Vector2.zero;
        videoRect.anchoredPosition = Vector2.zero;

        GameObject fadeObj = new GameObject("FadeOverlay");
        fadeObj.transform.SetParent(videoCanvas.transform, false);

        fadeImage = fadeObj.AddComponent<Image>();
        fadeImage.color = Color.black;

        RectTransform fadeRect = fadeObj.GetComponent<RectTransform>();
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.sizeDelta = Vector2.zero;
        fadeRect.anchoredPosition = Vector2.zero;

        fadeObj.transform.SetAsLastSibling();
    }

    private void SetupVideoPlayer()
    {
        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.clip = videoClip;
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        videoPlayer.SetDirectAudioVolume(0, videoVolume);

        renderTexture = new RenderTexture((int)videoClip.width, (int)videoClip.height, 0);
        videoPlayer.targetTexture = renderTexture;
        videoImage.texture = renderTexture;

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
            videoPlayer = null;
        }

        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
            renderTexture = null;
        }

        if (videoImage != null)
        {
            videoImage.texture = null;
        }

        videoIsPlaying = false;
    }

    private void OnDisable()
    {
        if (videoIsPlaying)
        {
            CleanupVideo();
            UnfreezeGameState();
        }
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoComplete;
        }

        CleanupVideo();
        UnfreezeGameState();
    }
}