using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Meta.WitAi.TTS.Utilities; // Meta Voice SDK namespace
using Meta.WitAi.TTS.Data; // For TTSClipData

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
    
    [Header("Audio Settings - Meta Voice TTS")]
    [Tooltip("TTSSpeaker component for Meta Voice SDK speech output (required)")]
    public TTSSpeaker ttsSpeaker;
    [Tooltip("AudioSource for playing pre-recorded audio clips")]
    public AudioSource audioSource;
    
    // Active indicator tracking
    private GameObject activeIndicator;
    private Transform indicatorTarget;
    private VRTutorialIndicator activeIndicatorComponent;
    
    // Audio queue system
    private Queue<TutorialAudioMessage> audioQueue = new Queue<TutorialAudioMessage>();
    private bool isPlayingAudio = false;
    private string currentSpeechText; // Track the text of the currently speaking message
    
    // First-time tracking
    private HashSet<string> triggeredObjects = new HashSet<string>();
    
    private struct TutorialAudioMessage
    {
        public string messageText;
        public AudioClip audioClip;
        public bool useTextToSpeech;
    }
    
    private void Awake()
    {
        // Setup TTSSpeaker if not assigned (must be done before Start to subscribe events)
        if (ttsSpeaker == null)
        {
            ttsSpeaker = GetComponent<TTSSpeaker>();
            if (ttsSpeaker == null)
            {
                Debug.LogError($"VRTutorialManager on {gameObject.name}: TTSSpeaker component not found! Please add a TTSSpeaker component or assign it in the inspector.");
            }
        }
    }
    
    private void Start()
    {
        // Find player camera
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main?.transform;
            if (cameraTransform == null)
            {
                Camera cam = FindObjectOfType<Camera>();
                if (cam != null) cameraTransform = cam.transform;
            }
        }
        
        // Setup audio source (only for AudioClip playback)
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }
    
    private void OnEnable()
    {
        // Subscribe to Meta Voice TTS events
        if (ttsSpeaker != null)
        {
            ttsSpeaker.Events.OnPlaybackComplete.AddListener(OnSpeechComplete);
            ttsSpeaker.Events.OnPlaybackCancelled.AddListener(OnSpeechCancelled); // FIX: Use the 3-argument listener
        }
    }
    
    private void OnDisable()
    {
        // Unsubscribe from Meta Voice TTS events
        if (ttsSpeaker != null)
        {
            ttsSpeaker.Events.OnPlaybackComplete.RemoveListener(OnSpeechComplete);
            ttsSpeaker.Events.OnPlaybackCancelled.RemoveListener(OnSpeechCancelled); // FIX: Remove the 3-argument listener
        }
        
        // Stop any ongoing speech
        if (ttsSpeaker != null)
        {
            ttsSpeaker.Stop();
        }
    }
    
    private void OnDestroy()
    {
        // Ensure any ongoing indicator/speech is cleaned up
        DestroyIndicator();
        if (ttsSpeaker != null)
        {
            ttsSpeaker.Stop();
        }
    }
    
    #region Meta Voice Event Handlers
    
    private void OnSpeechComplete(TTSSpeaker speaker, TTSClipData clipData)
    {
        // Only progress the queue if the completed speech is the one we started
        if (clipData.textToSpeak == currentSpeechText)
        {
            isPlayingAudio = false;
            currentSpeechText = null;
            ProcessAudioQueue();
        }
    }

    // FIX: New handler to match the 3-argument signature of OnPlaybackCancelled
    private void OnSpeechCancelled(TTSSpeaker speaker, TTSClipData clipData, string reason)
    {
        // Treat cancellation the same way as completion for queue progression.
        // We call OnSpeechComplete which handles the queue progression logic.
        OnSpeechComplete(speaker, clipData);
    }
    
    #endregion

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
            // Use TTS only if the speaker is assigned AND a message is provided
            useTextToSpeech = ttsSpeaker != null && !string.IsNullOrEmpty(messageText)
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
            // Use Meta Voice TTS
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
    /// Plays text-to-speech using Meta Voice TTS SDK
    /// </summary>
    private void PlayTextToSpeech(string text)
    {
        if (ttsSpeaker == null)
        {
            Debug.LogError("[VRTutorialManager] TTSSpeaker not assigned! Cannot play TTS.");
            isPlayingAudio = false;
            ProcessAudioQueue();
            return;
        }
        
        // Track the text so we can match it in the OnSpeechComplete event
        currentSpeechText = text;

        // Use Speak() for immediate playback/synthesis
        ttsSpeaker.Speak(text);
        
        Debug.Log($"[VRTutorialManager] Playing TTS (Meta Voice): {text}");
        
        // Completion is handled by the OnSpeechComplete event, so no manual timer is needed.
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
        
        if (ttsSpeaker != null)
        {
            ttsSpeaker.Stop();
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
            
            if (ttsSpeaker != null)
            {
                ttsSpeaker.Stop();
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