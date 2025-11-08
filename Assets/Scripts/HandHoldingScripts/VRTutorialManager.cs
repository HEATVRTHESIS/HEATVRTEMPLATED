using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Crosstales.RTVoice;
using Crosstales.RTVoice.Model;

/// <summary>
/// Manages tutorial indicators and one-time audio instructions for VR interactions.
/// Tracks which objects have been grabbed before to ensure audio only plays once.
/// </summary>
public class VRTutorialManager : MonoBehaviour
{
    [Header("System Control")]
    [Tooltip("Master switch for the entire tutorial system")]
    public bool tutorialEnabled = true;
    
    [Header("Indicator Prefab")]
    [Tooltip("Prefab containing a downward arrow and text component")]
    public GameObject indicatorPrefab;
    
    [Header("Camera Settings")]
    public Transform cameraTransform;
    public float followSpeed = 5f;
    public bool smoothFollow = true;
    
    [Header("Indicator Positioning")]
    [Tooltip("Height above the target object")]
    public float heightOffset = 0.5f;
    [Tooltip("Distance from object towards camera (like tooltip system)")]
    public float distanceFromObject = 0.3f;
    
    [Header("Audio Settings - Choose One Method")]
    [Tooltip("Use RT-Voice for text-to-speech (like your dialogue system)")]
    public bool useRTVoice = true;
    [Tooltip("AudioSource for playing pre-recorded audio clips (not needed for RT-Voice native TTS)")]
    public AudioSource audioSource;
    
    [Header("RT-Voice Settings (if enabled)")]
    [Tooltip("Voice to use for speech (leave empty for default)")]
    public string voiceName = "";
    [Tooltip("Speech rate (0.1 to 3.0, 1.0 is normal speed)")]
    [Range(0.1f, 3.0f)]
    public float speechRate = 1.0f;
    [Tooltip("Speech volume (0.0 to 1.0)")]
    [Range(0.0f, 1.0f)]
    public float speechVolume = 1.0f;
    
    // Active indicator tracking
    private GameObject activeIndicator;
    private Transform indicatorTarget;
    private VRTutorialIndicator activeIndicatorComponent;
    
    // Audio queue system
    private Queue<TutorialAudioMessage> audioQueue = new Queue<TutorialAudioMessage>();
    private bool isPlayingAudio = false;
    // private string currentSpeechId; // Not used with SpeakNative
    
    // First-time tracking
    private HashSet<string> triggeredObjects = new HashSet<string>();
    
    // RT-Voice
    private Voice selectedVoice;
    
    private struct TutorialAudioMessage
    {
        public string messageText;
        public AudioClip audioClip;
        public bool useTextToSpeech;
    }
    
    private void Start()
    {
        // Auto-find camera if not assigned
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main?.transform;
            if (cameraTransform == null)
            {
                Camera cam = FindObjectOfType<Camera>();
                if (cam != null) cameraTransform = cam.transform;
            }
        }
        
        // Setup audio source (only needed for AudioClip playback, NOT for native TTS)
        if (audioSource == null && !useRTVoice)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        // Setup RT-Voice if enabled
        if (useRTVoice)
        {
            // Note: SpeakNative doesn't fire OnSpeakComplete, so we don't subscribe to it
            Speaker.Instance.OnVoicesReady += OnVoicesReady;
        }
    }
    
    private void OnDestroy()
    {
        if (useRTVoice && Speaker.Instance != null)
        {
            Speaker.Instance.OnVoicesReady -= OnVoicesReady;
        }
    }
    
    private void OnVoicesReady()
    {
        if (!string.IsNullOrEmpty(voiceName))
        {
            selectedVoice = Speaker.Instance.VoiceForName(voiceName);
            if (selectedVoice == null)
            {
                Debug.LogWarning($"[VRTutorialManager] Voice '{voiceName}' not found. Using default.");
            }
        }
    }
    
    // NOTE: Not used with SpeakNative (which doesn't fire OnSpeakComplete)
    // Kept here in case switching to file-based Speak() method in the future
    /*
    private void OnSpeechComplete(Wrapper wrapper)
    {
        if (wrapper.Uid == currentSpeechId)
        {
            isPlayingAudio = false;
            ProcessAudioQueue();
        }
    }
    */
    
    private void Update()
    {
        // Destroy indicator if system disabled
        if (!tutorialEnabled && activeIndicator != null)
        {
            DestroyIndicator();
            return;
        }
        
        // Update indicator position
        if (activeIndicator != null && indicatorTarget != null && cameraTransform != null)
        {
            UpdateIndicatorPosition();
        }
    }
    
    /// <summary>
    /// Called when an object is grabbed. Handles both first-time audio and repeated indicators.
    /// </summary>
    /// <param name="triggerObjectName">Unique name/ID of the object being grabbed</param>
    /// <param name="indicatorTarget">Where to show the indicator (e.g., trash can location)</param>
    /// <param name="indicatorText">Text to show above the arrow</param>
    /// <param name="audioMessage">Audio message to play on first grab (optional)</param>
    /// <param name="audioClip">Pre-recorded audio clip (optional, used if not using TTS)</param>
    public void OnObjectGrabbed(string triggerObjectName, Transform indicatorTarget, string indicatorText, 
                               string audioMessage = null, AudioClip audioClip = null)
    {
        if (!tutorialEnabled) return;
        
        // Check if this is the first time this object was grabbed
        bool isFirstTime = !triggeredObjects.Contains(triggerObjectName);
        
        if (isFirstTime)
        {
            // Mark as triggered
            triggeredObjects.Add(triggerObjectName);
            
            // Queue audio if provided
            if (!string.IsNullOrEmpty(audioMessage) || audioClip != null)
            {
                QueueAudio(audioMessage, audioClip);
            }
        }
        
        // Always show the indicator (first time or not)
        ShowIndicator(indicatorTarget, indicatorText);
    }
    
    /// <summary>
    /// Shows the visual indicator at the target location
    /// </summary>
    public void ShowIndicator(Transform target, string text)
    {
        if (!tutorialEnabled || indicatorPrefab == null || target == null) 
        {
            if (!tutorialEnabled)
                Debug.LogWarning("[VRTutorialManager] Tutorial system is disabled");
            if (indicatorPrefab == null)
                Debug.LogError("[VRTutorialManager] Indicator prefab is not assigned!");
            if (target == null)
                Debug.LogError("[VRTutorialManager] Target is null!");
            return;
        }
        
        Debug.Log($"[VRTutorialManager] ShowIndicator called - Target: {target.name} at position {target.position}, Text: {text}");
        
        // Destroy existing indicator
        DestroyIndicator();
        
        // Create new indicator
        activeIndicator = Instantiate(indicatorPrefab);
        indicatorTarget = target;
        
        Debug.Log($"[VRTutorialManager] Indicator instantiated at initial position: {activeIndicator.transform.position}");
        Debug.Log($"[VRTutorialManager] Indicator target set to: {indicatorTarget.name} at {indicatorTarget.position}");
        
        // Get the indicator component
        activeIndicatorComponent = activeIndicator.GetComponent<VRTutorialIndicator>();
        if (activeIndicatorComponent != null)
        {
            activeIndicatorComponent.SetText(text);
        }
        else
        {
            Debug.LogError("[VRTutorialManager] Indicator prefab missing VRTutorialIndicator component!");
        }
        
        // Disable raycasts on indicator UI
        Canvas canvas = activeIndicator.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.sortingOrder = 1000;
        }
        
        UnityEngine.UI.Graphic[] graphics = activeIndicator.GetComponentsInChildren<UnityEngine.UI.Graphic>();
        foreach (var graphic in graphics)
        {
            graphic.raycastTarget = false;
        }
        
        // Set initial position
        UpdateIndicatorPosition();
        
        Debug.Log($"[VRTutorialManager] Indicator positioned at: {activeIndicator.transform.position}");
    }
    
    /// <summary>
    /// Destroys the current indicator
    /// </summary>
    public void DestroyIndicator()
    {
        if (activeIndicator != null)
        {
            Destroy(activeIndicator);
            activeIndicator = null;
            indicatorTarget = null;
            activeIndicatorComponent = null;
        }
    }
    
    /// <summary>
    /// Queues an audio message to be played. Messages play sequentially without overlap.
    /// </summary>
    private void QueueAudio(string messageText, AudioClip audioClip)
    {
        TutorialAudioMessage msg = new TutorialAudioMessage
        {
            messageText = messageText,
            audioClip = audioClip,
            useTextToSpeech = useRTVoice && !string.IsNullOrEmpty(messageText)
        };
        
        audioQueue.Enqueue(msg);
        
        // Start processing if not already playing
        if (!isPlayingAudio)
        {
            ProcessAudioQueue();
        }
    }
    
    /// <summary>
    /// Processes the audio queue, playing one message at a time
    /// </summary>
    private void ProcessAudioQueue()
    {
        if (audioQueue.Count == 0)
        {
            isPlayingAudio = false;
            return;
        }
        
        TutorialAudioMessage msg = audioQueue.Dequeue();
        isPlayingAudio = true;
        
        if (msg.useTextToSpeech)
        {
            // Use RT-Voice
            PlayTextToSpeech(msg.messageText);
        }
        else if (msg.audioClip != null)
        {
            // Use regular audio clip
            StartCoroutine(PlayAudioClipCoroutine(msg.audioClip));
        }
        else
        {
            // No valid audio, move to next
            isPlayingAudio = false;
            ProcessAudioQueue();
        }
    }
    
    /// <summary>
    /// Plays text-to-speech using RT-Voice
    /// </summary>
    private void PlayTextToSpeech(string text)
    {
        if (Speaker.Instance == null)
        {
            Debug.LogError("[VRTutorialManager] RT-Voice Speaker not available!");
            isPlayingAudio = false;
            ProcessAudioQueue();
            return;
        }
        
        // USE NATIVE SPEECH - NO FILE GENERATION
        // Note: SpeakNative doesn't return a UID, so we can't track completion
        // We'll use a simple timer based on text length
        Speaker.Instance.SpeakNative(
            text,
            selectedVoice,
            speechRate,
            1.0f,  // pitch
            speechVolume
        );
        
        Debug.Log($"[VRTutorialManager] Playing TTS (NATIVE): {text}");
        
        // Estimate speech duration (rough: 150 words per minute average)
        float estimatedDuration = EstimateSpeechDuration(text);
        StartCoroutine(WaitForNativeSpeechComplete(estimatedDuration));
    }
    
    /// <summary>
    /// Estimates speech duration based on text length
    /// </summary>
    private float EstimateSpeechDuration(string text)
    {
        // Average speaking rate: ~150 words per minute = 2.5 words per second
        // Average word length: ~5 characters
        // So roughly: characters / 12.5 = seconds
        int charCount = text.Length;
        float baseDuration = charCount / 12.5f / speechRate;
        
        // Add small buffer
        return baseDuration + 0.5f;
    }
    
    /// <summary>
    /// Waits for estimated native speech completion
    /// </summary>
    private System.Collections.IEnumerator WaitForNativeSpeechComplete(float duration)
    {
        yield return new WaitForSeconds(duration);
        
        isPlayingAudio = false;
        ProcessAudioQueue();
    }
    
    /// <summary>
    /// Plays a regular audio clip
    /// </summary>
    private IEnumerator PlayAudioClipCoroutine(AudioClip clip)
    {
        if (audioSource == null)
        {
            Debug.LogError("[VRTutorialManager] AudioSource not assigned! Cannot play audio clip.");
            isPlayingAudio = false;
            ProcessAudioQueue();
            yield break;
        }
        
        audioSource.clip = clip;
        audioSource.Play();
        
        Debug.Log($"[VRTutorialManager] Playing audio clip: {clip.name}");
        
        // Wait for clip to finish
        yield return new WaitForSeconds(clip.length);
        
        isPlayingAudio = false;
        ProcessAudioQueue();
    }
    
    /// <summary>
    /// Updates the indicator's position to follow the target and face the camera
    /// Uses same positioning logic as VRTooltipManager for consistency
    /// </summary>
    private void UpdateIndicatorPosition()
    {
        if (activeIndicator == null || indicatorTarget == null || cameraTransform == null)
            return;
        
        // Calculate position above the target object (same as tooltip system)
        Vector3 basePosition = indicatorTarget.position + Vector3.up * heightOffset;
        
        // Calculate direction from camera to target
        Vector3 cameraToTarget = (basePosition - cameraTransform.position).normalized;
        
        // Position indicator at a distance from the object towards the camera
        Vector3 indicatorPosition = basePosition - cameraToTarget * distanceFromObject;
        
        // Check if this is the first positioning (indicator is at origin)
        bool isFirstPosition = activeIndicator.transform.position == Vector3.zero;
        
        if (isFirstPosition)
        {
            Debug.Log($"[VRTutorialManager] UpdateIndicatorPosition - First position calculation:");
            Debug.Log($"  Target: {indicatorTarget.position}");
            Debug.Log($"  Base Position: {basePosition}");
            Debug.Log($"  Camera: {cameraTransform.position}");
            Debug.Log($"  Final Indicator Position: {indicatorPosition}");
        }
        
        // Apply position (force immediate on first frame, then optional smoothing)
        if (smoothFollow && !isFirstPosition)
        {
            activeIndicator.transform.position = Vector3.Lerp(
                activeIndicator.transform.position,
                indicatorPosition,
                followSpeed * Time.deltaTime
            );
        }
        else
        {
            // First frame or no smoothing - snap to position immediately
            activeIndicator.transform.position = indicatorPosition;
        }
        
        // Make indicator face the camera
        Vector3 lookDirection = cameraTransform.position - activeIndicator.transform.position;
        lookDirection.y = 0; // Keep it upright, only rotate on Y-axis
        
        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            
            if (smoothFollow && !isFirstPosition)
            {
                activeIndicator.transform.rotation = Quaternion.Slerp(
                    activeIndicator.transform.rotation,
                    targetRotation,
                    followSpeed * Time.deltaTime
                );
            }
            else
            {
                // First frame or no smoothing - snap rotation immediately
                activeIndicator.transform.rotation = targetRotation;
            }
        }
    }
    
    /// <summary>
    /// Manually queue an audio message (useful for scripted sequences)
    /// </summary>
    public void PlayAudioMessage(string message)
    {
        if (!tutorialEnabled) return;
        QueueAudio(message, null);
    }
    
    /// <summary>
    /// Manually queue an audio clip
    /// </summary>
    public void PlayAudioClip(AudioClip clip)
    {
        if (!tutorialEnabled) return;
        QueueAudio(null, clip);
    }
    
    /// <summary>
    /// Check if a specific object has been triggered before
    /// </summary>
    public bool HasBeenTriggered(string objectName)
    {
        return triggeredObjects.Contains(objectName);
    }
    
    /// <summary>
    /// Reset the tutorial system (clear all triggered objects)
    /// </summary>
    public void ResetTutorialProgress()
    {
        triggeredObjects.Clear();
        audioQueue.Clear();
        isPlayingAudio = false;
        DestroyIndicator();
        
        if (useRTVoice && Speaker.Instance != null)
        {
            Speaker.Instance.Silence();
        }
        
        Debug.Log("[VRTutorialManager] Tutorial progress reset");
    }
    
    /// <summary>
    /// Enable or disable the tutorial system
    /// </summary>
    public void SetTutorialEnabled(bool enabled)
    {
        tutorialEnabled = enabled;
        
        if (!enabled)
        {
            DestroyIndicator();
            audioQueue.Clear();
            isPlayingAudio = false;
            
            if (useRTVoice && Speaker.Instance != null)
            {
                Speaker.Instance.Silence();
            }
        }
    }
    
    /// <summary>
    /// Check if audio is currently playing
    /// </summary>
    public bool IsPlayingAudio()
    {
        return isPlayingAudio;
    }
    
    /// <summary>
    /// Get count of queued audio messages
    /// </summary>
    public int GetQueuedAudioCount()
    {
        return audioQueue.Count;
    }
}