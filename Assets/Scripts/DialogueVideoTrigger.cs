using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System;
using System.Reflection;

/// <summary>
/// Monitors the VRDialogueSystem and displays images or video clips when specific dialogue lines are shown.
/// Uses a single RawImage component for both images and videos.
/// Only active when dialogue is running to maintain performance.
/// </summary>
public class DialogueMediaTrigger : MonoBehaviour
{
    [Header("System References")]
    [Tooltip("Reference to the VRDialogueSystem to monitor")]
    [SerializeField] private VRDialogueSystem dialogueSystem;
    
    [Tooltip("Canvas that contains the media display - will be enabled/disabled")]
    [SerializeField] private GameObject mediaCanvas;
    
    [Tooltip("RawImage component that displays both images and videos")]
    [SerializeField] private RawImage mediaDisplay;
    
    [Tooltip("VideoPlayer component (should be on same GameObject as RawImage)")]
    [SerializeField] private VideoPlayer videoPlayer;
    
    [Tooltip("RenderTexture used by VideoPlayer (assign this to VideoPlayer's Target Texture)")]
    [SerializeField] private RenderTexture videoRenderTexture;
    
    [Header("Media Triggers")]
    [Tooltip("List of dialogue strings and their corresponding media (images or videos)")]
    [SerializeField] private MediaDialogueTrigger[] mediaTriggers;
    
    [Header("Settings")]
    [Tooltip("Should videos loop while the dialogue line is displayed?")]
    [SerializeField] private bool loopVideo = false;
    
    [Tooltip("Fade in/out duration for the canvas (0 = instant)")]
    [SerializeField] private float fadeDuration = 0.2f;
    
    [Tooltip("Preserve aspect ratio of images/videos")]
    [SerializeField] private bool preserveAspect = true;
    
    // Private fields
    private string _lastCheckedLine = "";
    private bool _wasDialogueActive = false;
    private bool _isMediaShowing = false;
    private CanvasGroup _canvasGroup;
    private FieldInfo _currentLineField;
    private Coroutine _fadeCoroutine;
    private Texture _originalTexture; // Store original texture to restore if needed
    
    void Start()
    {
        // Validate references
        if (dialogueSystem == null)
        {
            Debug.LogError("DialogueMediaTrigger: VRDialogueSystem reference is not set!");
            enabled = false;
            return;
        }
        
        if (mediaCanvas == null)
        {
            Debug.LogError("DialogueMediaTrigger: Media Canvas reference is not set!");
            enabled = false;
            return;
        }
        
        if (mediaDisplay == null)
        {
            Debug.LogError("DialogueMediaTrigger: Media Display (RawImage) reference is not set!");
            enabled = false;
            return;
        }
        
        // Setup canvas group for fading (add if doesn't exist)
        _canvasGroup = mediaCanvas.GetComponent<CanvasGroup>();
        if (_canvasGroup == null && fadeDuration > 0)
        {
            _canvasGroup = mediaCanvas.AddComponent<CanvasGroup>();
        }
        
        // Get the private _currentLine field using reflection for efficient access
        _currentLineField = typeof(VRDialogueSystem).GetField("_currentLine", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (_currentLineField == null)
        {
            Debug.LogError("DialogueMediaTrigger: Could not access _currentLine field from VRDialogueSystem!");
            enabled = false;
            return;
        }
        
        // Configure video player if available
        if (videoPlayer != null)
        {
            videoPlayer.isLooping = loopVideo;
            videoPlayer.playOnAwake = false;
            
            // Set render texture as target
            if (videoRenderTexture != null)
            {
                videoPlayer.targetTexture = videoRenderTexture;
                videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            }
            else
            {
                Debug.LogWarning("DialogueMediaTrigger: No RenderTexture assigned! Videos won't display properly.");
            }
        }
        
        // Store original texture
        _originalTexture = mediaDisplay.texture;
        
        // Set preserve aspect
        if (preserveAspect && mediaDisplay != null)
        {
            mediaDisplay.GetComponent<AspectRatioFitter>()?.enabled.Equals(true);
        }
        
        // Start with canvas hidden
        mediaCanvas.SetActive(false);
        _isMediaShowing = false;
    }
    
    void Update()
    {
        // Early exit: Only check when dialogue is active (performance optimization)
        bool isDialogueActive = dialogueSystem.IsDialogueActive();
        
        // If dialogue just ended, hide media
        if (_wasDialogueActive && !isDialogueActive)
        {
            HideMedia();
            _lastCheckedLine = "";
            _wasDialogueActive = false;
            return;
        }
        
        // Nothing to do if dialogue isn't active
        if (!isDialogueActive)
        {
            return;
        }
        
        _wasDialogueActive = true;
        
        // Get current dialogue line using reflection
        string currentLine = _currentLineField.GetValue(dialogueSystem) as string;
        
        // Early exit: Only process if the line has changed (performance optimization)
        if (currentLine == _lastCheckedLine)
        {
            return;
        }
        
        // Line has changed - update tracking
        _lastCheckedLine = currentLine;
        
        // Check if current line matches any media triggers
        bool foundMatch = false;
        
        if (!string.IsNullOrEmpty(currentLine))
        {
            foreach (MediaDialogueTrigger trigger in mediaTriggers)
            {
                if (trigger.IsMatch(currentLine))
                {
                    // Match found - show appropriate media
                    if (trigger.mediaType == MediaType.Image)
                    {
                        ShowImage(trigger.imageTexture);
                    }
                    else if (trigger.mediaType == MediaType.Video)
                    {
                        ShowVideo(trigger.videoClip);
                    }
                    foundMatch = true;
                    break; // Only show first match
                }
            }
        }
        
        // If no match found and media is showing, hide it
        if (!foundMatch && _isMediaShowing)
        {
            HideMedia();
        }
    }
    
    /// <summary>
    /// Shows an image texture on the RawImage
    /// </summary>
    private void ShowImage(Texture2D texture)
    {
        if (texture == null)
        {
            Debug.LogWarning("DialogueMediaTrigger: No image texture assigned!");
            HideMedia();
            return;
        }
        
        _isMediaShowing = true;
        
        // Stop video if playing
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }
        
        // Set the image texture directly to RawImage
        mediaDisplay.texture = texture;
        
        // Show canvas
        ShowCanvas();
    }
    
    /// <summary>
    /// Shows a video on the RawImage through the VideoPlayer's RenderTexture
    /// </summary>
    private void ShowVideo(VideoClip clip)
    {
        if (videoPlayer == null)
        {
            Debug.LogWarning("DialogueMediaTrigger: VideoPlayer not assigned!");
            HideMedia();
            return;
        }
        
        if (clip == null)
        {
            Debug.LogWarning("DialogueMediaTrigger: No video clip assigned!");
            HideMedia();
            return;
        }
        
        if (videoRenderTexture == null)
        {
            Debug.LogError("DialogueMediaTrigger: No RenderTexture assigned for video playback!");
            HideMedia();
            return;
        }
        
        _isMediaShowing = true;
        
        // Set the RenderTexture on the RawImage (video will render to this)
        mediaDisplay.texture = videoRenderTexture;
        
        // Set the video clip and play
        videoPlayer.clip = clip;
        
        // Show canvas
        ShowCanvas();
        
        // Play the video
        videoPlayer.Play();
    }
    
    /// <summary>
    /// Shows the canvas with optional fade
    /// </summary>
    private void ShowCanvas()
    {
        if (!mediaCanvas.activeSelf)
        {
            mediaCanvas.SetActive(true);
        }
        
        // Fade in if canvas group exists
        if (_canvasGroup != null && fadeDuration > 0)
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }
            _fadeCoroutine = StartCoroutine(FadeCanvasGroup(_canvasGroup, _canvasGroup.alpha, 1f, fadeDuration));
        }
        else if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
        }
    }
    
    /// <summary>
    /// Hides the media canvas and stops playback
    /// </summary>
    private void HideMedia()
    {
        if (!_isMediaShowing)
        {
            return; // Already hidden
        }
        
        _isMediaShowing = false;
        
        // Stop the video if playing
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
        
        // Clear the texture (restore original if it existed)
        if (mediaDisplay != null)
        {
            mediaDisplay.texture = _originalTexture;
        }
        
        // Fade out then hide, or hide immediately
        if (_canvasGroup != null && fadeDuration > 0)
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }
            _fadeCoroutine = StartCoroutine(FadeOutAndHide(_canvasGroup, fadeDuration));
        }
        else
        {
            mediaCanvas.SetActive(false);
        }
    }
    
    /// <summary>
    /// Coroutine to fade canvas group alpha
    /// </summary>
    private System.Collections.IEnumerator FadeCanvasGroup(CanvasGroup cg, float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaled time in case dialogue pauses time
            float t = Mathf.Clamp01(elapsed / duration);
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }
        
        cg.alpha = endAlpha;
    }
    
    /// <summary>
    /// Coroutine to fade out and then hide the canvas
    /// </summary>
    private System.Collections.IEnumerator FadeOutAndHide(CanvasGroup cg, float duration)
    {
        float elapsed = 0f;
        float startAlpha = cg.alpha;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            cg.alpha = Mathf.Lerp(startAlpha, 0f, t);
            yield return null;
        }
        
        cg.alpha = 0f;
        mediaCanvas.SetActive(false);
    }
    
    void OnDisable()
    {
        // Clean up when disabled
        if (_isMediaShowing)
        {
            HideMedia();
        }
    }
}

/// <summary>
/// Type of media to display
/// </summary>
public enum MediaType
{
    Image,
    Video
}

/// <summary>
/// Match mode for dialogue triggers
/// </summary>
public enum MatchMode
{
    [Tooltip("Match a single specific dialogue line")]
    SingleLine,
    [Tooltip("Match any line from a list of dialogue lines")]
    MultipleLines,
    [Tooltip("Match any line containing a keyword")]
    Contains
}

/// <summary>
/// Serializable class that pairs dialogue strings with media (image or video)
/// Supports multiple dialogue lines triggering the same media
/// </summary>
[Serializable]
public class MediaDialogueTrigger
{
    [Header("Dialogue Matching")]
    [Tooltip("Match mode: Single line, Multiple lines, or Contains keyword")]
    public MatchMode matchMode = MatchMode.SingleLine;
    
    [Tooltip("Single dialogue line that will trigger this media (for SingleLine mode)")]
    public string dialogueLine;
    
    [Tooltip("Multiple dialogue lines that will ALL trigger this media (for MultipleLines mode)")]
    [TextArea(3, 10)]
    public string[] dialogueLines;
    
    [Tooltip("Keyword to search for in any line (for Contains mode)")]
    public string containsKeyword;
    
    [Tooltip("Ignore case when matching")]
    public bool ignoreCase = false;
    
    [Header("Media Content")]
    [Tooltip("Type of media to display")]
    public MediaType mediaType = MediaType.Image;
    
    [Tooltip("Image texture to display (for Image type) - Use PNG, JPG, etc.")]
    public Texture2D imageTexture;
    
    [Tooltip("Video clip to play (for Video type)")]
    public VideoClip videoClip;
    
    /// <summary>
    /// Checks if the given line matches this trigger
    /// </summary>
    public bool IsMatch(string currentLine)
    {
        if (string.IsNullOrEmpty(currentLine))
        {
            return false;
        }
        
        StringComparison comparison = ignoreCase 
            ? StringComparison.OrdinalIgnoreCase 
            : StringComparison.Ordinal;
        
        switch (matchMode)
        {
            case MatchMode.SingleLine:
                if (string.IsNullOrEmpty(dialogueLine))
                    return false;
                return string.Equals(currentLine, dialogueLine, comparison);
                
            case MatchMode.MultipleLines:
                if (dialogueLines == null || dialogueLines.Length == 0)
                    return false;
                foreach (string line in dialogueLines)
                {
                    if (!string.IsNullOrEmpty(line) && string.Equals(currentLine, line, comparison))
                    {
                        return true;
                    }
                }
                return false;
                
            case MatchMode.Contains:
                if (string.IsNullOrEmpty(containsKeyword))
                    return false;
                return currentLine.IndexOf(containsKeyword, comparison) >= 0;
                
            default:
                return false;
        }
    }
}