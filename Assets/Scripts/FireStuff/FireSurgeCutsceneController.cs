using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using Meta.WitAi.TTS.Utilities; // NEW: Meta Voice SDK namespace
using Meta.WitAi.TTS.Data; // NEW: For TTSClipData

// Removed: using System.IO;
// Removed: using Crosstales.RTVoice;
// Removed: using Crosstales.RTVoice.Model;

/// <summary>
/// Manages a fire surge cutscene with lightning effects, light flickering, and NPC dialogue.
/// Freezes time and player movement during the cutscene to prevent premature gameplay.
/// Includes Meta Voice TTS support and button-based dialogue progression.
/// </summary>
public class FireSurgeCutsceneController : MonoBehaviour
{
    [Header("Player References")]
    [Tooltip("Assign the XR Origin or OVRPlayerController")]
    public GameObject playerRig;
    [Tooltip("If using a specific movement script, assign it here")]
    public MonoBehaviour playerMovementScript;
    
    [Header("NPC Animation")]
    public Animator npcAnimator;
    [Tooltip("Name of the talking animation state in Animator")]
    public string talkingAnimationName = "Talking";
    [Tooltip("Name of the idle animation state in Animator")]
    public string idleAnimationName = "Idle";
    [Tooltip("Name of the alarmed/shocked animation state in Animator")]
    public string alarmedAnimationName = "Alarmed";
    
    [Header("Lightning Effect")]
    [Tooltip("Audio source for lightning sound effect")]
    public AudioSource lightningAudioSource;
    [Tooltip("Lightning sound clip")]
    public AudioClip lightningSound;
    [Tooltip("Volume for lightning sound (0-1)")]
    [Range(0f, 1f)]
    public float lightningVolume = 1f;
    
    [Header("Light Flickering")]
    [Tooltip("Lights that will flicker during the surge")]
    public List<Light> sceneLights = new List<Light>();
    [Tooltip("Number of times lights flicker")]
    public int flickerCount = 5;
    [Tooltip("Duration of each flicker on/off cycle")]
    public float flickerSpeed = 0.1f;
    [Tooltip("Should lights stay on after flickering?")]
    public bool lightsOnAfterFlicker = true;
    
    [Header("Spark Effects")]
    [Tooltip("Particle systems for sparks from electronic devices")]
    public List<ParticleSystem> sparkParticleSystems = new List<ParticleSystem>();
    [Tooltip("How long each individual spark burst lasts")]
    public float sparkBurstDuration = 0.5f;
    [Tooltip("Time between spark bursts")]
    public float sparkInterval = 1f;
    
    [Header("Dialogue System")]
    [TextArea(3, 10)]
    public List<string> dialogueLines = new List<string>();
    public float timeBetweenLines = 0.5f;
    [Tooltip("The speed at which characters are typed out. A smaller value is faster.")]
    public float typingSpeed = 0.05f;
    
    [Header("UI References")]
    public GameObject dialogueUI;
    public TMPro.TextMeshProUGUI dialogueText;
    
    [Header("Input Mapping")]
    [Tooltip("Input action to progress dialogue lines")]
    public InputActionProperty nextLineAction;
    
    // --- NEW: Meta Voice TTS Settings ---
    [Header("Meta Voice TTS Settings")]
    [Tooltip("Enable text-to-speech using Meta Voice SDK")]
    public bool enableTTS = true;
    [Tooltip("TTSSpeaker component for Meta Voice SDK speech output")]
    public TTSSpeaker ttsSpeaker;
    [Tooltip("Wait for speech to complete before allowing next line")]
    public bool waitForSpeech = true;
    [Tooltip("Show text immediately when speech starts (disable typewriter for speech)")]
    public bool showTextImmediatelyWithSpeech = false;
    // --- REMOVED RT-Voice Settings ---
    
    [Header("Game State Management")]
    [Tooltip("FireTimer component to pause during cutscene")]
    public FireTimer fireTimer;
    [Tooltip("Additional MonoBehaviours to disable during cutscene (e.g., flammable object scripts)")]
    public List<MonoBehaviour> scriptsToDisable = new List<MonoBehaviour>();
    
    [Header("Cutscene Settings")]
    public bool startCutsceneOnStart = true;
    public float cutsceneStartDelay = 0.5f;
    [Tooltip("Delay after lightning before lights flicker")]
    public float delayBeforeFlicker = 0.2f;
    [Tooltip("Delay after flicker before NPC speaks")]
    public float delayBeforeDialogue = 0.5f;
    
    private bool cutsceneActive = false;
    private bool sparksRunning = false;
    private Transform playerCamera;
    private CharacterController characterController;
    private bool characterControllerWasEnabled;
    private List<bool> lightsOriginalState = new List<bool>();
    private List<bool> scriptsOriginalState = new List<bool>();
    private bool fireTimerWasRunning = false;
    
    // TTS and dialogue progression variables
    private bool isTyping = false;
    private bool isSpeaking = false;
    private bool speechStartedForCurrentLine = false;
    private string currentLine;
    private Coroutine typingCoroutine;
    
    // NEW: Variable to track the currently speaking text (like NPCCutsceneController.cs)
    private string currentSpeechText;
    
    // Removed RT-Voice specific variables:
    // private Voice selectedVoice;
    // private string currentSpeechId;
    
    private Queue<string> dialogueQueue = new Queue<string>();
    private bool dialogueInProgress = false;
    
    void Awake()
    {
        // Removed RT-Voice path configuration.
        // Removed speechAudioSource setup (not used by TTSSpeaker)
        
        // Setup TTSSpeaker if not assigned
        if (ttsSpeaker == null && enableTTS)
        {
            ttsSpeaker = GetComponent<TTSSpeaker>();
            if (ttsSpeaker == null)
            {
                Debug.LogError($"FireSurgeCutsceneController on {gameObject.name}: TTSSpeaker component not found! Please add a TTSSpeaker component or assign it in the inspector.");
            }
        }
    }
    
    void Start()
    {
        // ... (rest of Start() is unchanged) ...
        
        // Find player camera
        playerCamera = Camera.main.transform;
        
        // Try to find CharacterController on player rig
        if (playerRig != null)
        {
            characterController = playerRig.GetComponent<CharacterController>();
        }
        
        // Store original state of lights
        foreach (Light light in sceneLights)
        {
            if (light != null)
            {
                lightsOriginalState.Add(light.enabled);
            }
        }
        
        // Setup particle systems to work with timeScale = 0
        foreach (ParticleSystem ps in sparkParticleSystems)
        {
            if (ps != null)
            {
                var main = ps.main;
                main.useUnscaledTime = true; // Critical: makes particles work when time is frozen
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
        
        // Hide dialogue UI initially
        if (dialogueUI != null)
            dialogueUI.SetActive(false);
        
        if (startCutsceneOnStart)
        {
            Invoke(nameof(StartCutscene), cutsceneStartDelay);
        }
    }
    
    void OnEnable()
    {
        // Subscribe to next line action
        if (nextLineAction.action != null)
        {
            nextLineAction.action.performed += OnNextLineButtonPressed;
            nextLineAction.action.Enable();
        }

        // NEW: Subscribe to Meta Voice TTS events
        if (enableTTS && ttsSpeaker != null)
        {
            ttsSpeaker.Events.OnPlaybackStart.AddListener(OnSpeechStart);
            ttsSpeaker.Events.OnPlaybackComplete.AddListener(OnSpeechComplete);
            ttsSpeaker.Events.OnPlaybackCancelled.AddListener(OnSpeechCancelled); // FIX: 3-argument listener
        }
        // Removed RT-Voice subscriptions (Speaker.Instance)
    }
    
    void OnDisable()
    {
        // Unsubscribe from next line action
        if (nextLineAction.action != null)
        {
            nextLineAction.action.performed -= OnNextLineButtonPressed;
            nextLineAction.action.Disable();
        }

        // NEW: Unsubscribe from Meta Voice TTS events
        if (enableTTS && ttsSpeaker != null)
        {
            ttsSpeaker.Events.OnPlaybackStart.RemoveListener(OnSpeechStart);
            ttsSpeaker.Events.OnPlaybackComplete.RemoveListener(OnSpeechComplete);
            ttsSpeaker.Events.OnPlaybackCancelled.RemoveListener(OnSpeechCancelled);
        }
        // Removed RT-Voice unsubscriptions (Speaker.Instance)
    }

    #region Meta Voice Event Handlers // NEW: Using Meta Voice Handlers

    // Removed OnVoicesReady()

    private void OnSpeechStart(TTSSpeaker speaker, TTSClipData clipData)
    {
        if (clipData.textToSpeak == currentSpeechText)
        {
            isSpeaking = true;
        }
    }

    private void OnSpeechComplete(TTSSpeaker speaker, TTSClipData clipData)
    {
        if (clipData.textToSpeak == currentSpeechText)
        {
            isSpeaking = false;
        }
    }

    // NEW: Handler for OnPlaybackCancelled (3-argument signature)
    private void OnSpeechCancelled(TTSSpeaker speaker, TTSClipData clipData, string reason)
    {
        if (clipData.textToSpeak == currentSpeechText)
        {
            isSpeaking = false;
        }
    }

    #endregion

    #region Input Handlers

    private void OnNextLineButtonPressed(InputAction.CallbackContext context)
    {
        if (!dialogueInProgress)
        {
            return;
        }

        // If a line is currently being typed, complete it immediately
        if (isTyping)
        {
            CompleteLine();
            return;
        }
        
        // If we are waiting for speech, and it's playing, skip the speech
        if (waitForSpeech && isSpeaking)
        {
            if (enableTTS && ttsSpeaker != null)
            {
                ttsSpeaker.Stop(); // Stop speech immediately
            }
            return;
        }

        // Move to next line if typing is done and speech is done (or we're not waiting)
        if (!waitForSpeech || !isSpeaking)
        {
            // Stop any ongoing speech before moving to next line (safety)
            if (enableTTS && ttsSpeaker != null)
            {
                ttsSpeaker.Stop();
            }
            DisplayNextDialogueLine();
        }
    }

    #endregion
    
    public void StartCutscene()
    {
        if (cutsceneActive) return;
        
        cutsceneActive = true;
        FreezeGameState();
        StartCoroutine(PlayCutsceneSequence());
    }
    
    private IEnumerator PlayCutsceneSequence()
    {
        // ... (Unchanged) ...
        
        // Phase 1: Lightning Strike
        PlayLightningSound();
        
        // Wait for lightning sound to start
        yield return new WaitForSecondsRealtime(delayBeforeFlicker);
        
        // Phase 2: Light Flickering AND Spark Particles (simultaneous)
        StartCoroutine(PlaySparkParticles());
        yield return StartCoroutine(FlickerLights());
        
        // Phase 3: NPC Reacts (optional alarmed animation)
        if (!string.IsNullOrEmpty(alarmedAnimationName))
        {
            PlayAnimation(alarmedAnimationName);
        }
        
        // Wait before dialogue starts
        yield return new WaitForSecondsRealtime(delayBeforeDialogue);
        
        // Phase 4: NPC Dialogue with button progression
        yield return StartCoroutine(PlayDialogue());
        
        // Phase 5: End Cutscene and Start Level
        EndCutscene();
    }
    
    private void PlayLightningSound()
    {
        // ... (Unchanged) ...
        if (lightningAudioSource != null && lightningSound != null)
        {
            lightningAudioSource.PlayOneShot(lightningSound, lightningVolume);
            Debug.Log("Lightning sound played!");
        }
        else
        {
            Debug.LogWarning("Lightning audio source or sound clip not assigned!");
        }
    }
    
    private IEnumerator FlickerLights()
    {
        // ... (Unchanged) ...
        for (int i = 0; i < flickerCount; i++)
        {
            // Turn lights off
            foreach (Light light in sceneLights)
            {
                if (light != null)
                {
                    light.enabled = false;
                }
            }
            
            yield return new WaitForSecondsRealtime(flickerSpeed);
            
            // Turn lights on
            foreach (Light light in sceneLights)
            {
                if (light != null)
                {
                    light.enabled = true;
                }
            }
            
            yield return new WaitForSecondsRealtime(flickerSpeed);
        }
        
        // Set final light state
        foreach (Light light in sceneLights)
        {
            if (light != null)
            {
                light.enabled = lightsOnAfterFlicker;
            }
        }
        
        Debug.Log("Light flickering complete!");
    }
    
    private IEnumerator PlaySparkParticles()
    {
        // ... (Unchanged) ...
        int burstCount = 0;
        sparksRunning = true;
        
        Debug.Log($"Starting continuous spark loop with {sparkInterval}s intervals");
        
        // Keep sparking forever until manually stopped
        while (sparksRunning)
        {
            burstCount++;
            
            // Play spark burst
            foreach (ParticleSystem ps in sparkParticleSystems)
            {
                if (ps != null)
                {
                    // Double-check unscaled time is set (safety check)
                    var main = ps.main;
                    main.useUnscaledTime = true;
                    
                    // Clear any existing particles and play fresh
                    ps.Clear();
                    ps.Play();
                    
                    Debug.Log($"Spark burst #{burstCount} on: {ps.gameObject.name}");
                }
            }
            
            // Wait for burst to finish
            yield return new WaitForSecondsRealtime(sparkBurstDuration);
            
            // Stop emitting (let existing particles fade out)
            foreach (ParticleSystem ps in sparkParticleSystems)
            {
                if (ps != null)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
            
            // Wait for interval before next burst
            yield return new WaitForSecondsRealtime(sparkInterval);
        }
        
        Debug.Log($"Spark loop stopped! Total bursts: {burstCount}");
    }
    
    private IEnumerator PlayDialogue()
    {
        // ... (Unchanged) ...
        // Show dialogue UI
        if (dialogueUI != null)
            dialogueUI.SetActive(true);

        // Populate dialogue queue
        dialogueQueue.Clear();
        foreach (string line in dialogueLines)
        {
            if (!string.IsNullOrEmpty(line.Trim()))
            {
                dialogueQueue.Enqueue(line);
            }
        }

        // Start dialogue progression
        dialogueInProgress = true;
        DisplayNextDialogueLine();

        // Wait for all dialogue to complete
        while (dialogueInProgress)
        {
            yield return null;
        }
        
        // Hide dialogue UI
        if (dialogueUI != null)
            dialogueUI.SetActive(false);
        
        Debug.Log("Dialogue complete!");
    }

    private void DisplayNextDialogueLine()
    {
        // Stop any ongoing typing
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        // Stop any ongoing speech
        if (enableTTS && ttsSpeaker != null && isSpeaking)
        {
            ttsSpeaker.Stop();
        }

        // Reset speech flag for new line
        speechStartedForCurrentLine = false;

        // Check if there are lines left to display
        if (dialogueQueue.Count > 0)
        {
            currentLine = dialogueQueue.Dequeue();

            // Switch to talking animation
            PlayAnimation(talkingAnimationName);

            if (enableTTS && showTextImmediatelyWithSpeech)
            {
                // Show text immediately and start speech
                if (dialogueText != null)
                    dialogueText.text = currentLine;
                StartSpeech(currentLine);
            }
            else
            {
                // Start typing animation
                typingCoroutine = StartCoroutine(TypeLine(currentLine));
            }
        }
        else
        {
            // All dialogue complete
            dialogueInProgress = false;
            PlayAnimation(idleAnimationName);
        }
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        if (dialogueText != null)
            dialogueText.text = "";

        // Start speech if enabled and not showing text immediately
        if (enableTTS && !showTextImmediatelyWithSpeech)
        {
            StartSpeech(line);
        }

        foreach (char character in line.ToCharArray())
        {
            if (dialogueText != null)
                dialogueText.text += character;
            // Use real time for cutscene/dialogue during frozen game state
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isTyping = false;

        // Start speech after typing if not already started (edge case)
        if (enableTTS && showTextImmediatelyWithSpeech && !isSpeaking)
        {
            StartSpeech(line);
        }
    }

    private void CompleteLine()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        isTyping = false;
        if (dialogueText != null)
            dialogueText.text = currentLine;

        // Only start speech if it hasn't been started yet for this line
        if (enableTTS && !speechStartedForCurrentLine && !isSpeaking)
        {
            StartSpeech(currentLine);
        }
    }

    private void StartSpeech(string text)
    {
        if (!enableTTS || ttsSpeaker == null || string.IsNullOrEmpty(text.Trim()))
            return;
        
        // Removed RT-Voice specific logic (voice selection, speech ID, native vs file)

        speechStartedForCurrentLine = true;
        currentSpeechText = text; // Set text to track completion/cancellation

        // Use TTSSpeaker's Speak() for immediate playback/synthesis
        ttsSpeaker.Speak(text);
    }
    
    private void PlayAnimation(string animationName)
    {
        // ... (Unchanged) ...
        if (npcAnimator != null && !string.IsNullOrEmpty(animationName))
        {
            npcAnimator.Play(animationName);
        }
    }
    
    private float CalculateLineDuration(string line)
    {
        // ... (Unchanged) ...
        // Rough estimate: 0.05 seconds per character, minimum 2 seconds
        float duration = Mathf.Max(2f, line.Length * 0.05f);
        return duration;
    }
    
    private void FreezeGameState()
    {
        // ... (Unchanged) ...
        // Freeze time (prevents physics and most Update loops)
        Time.timeScale = 0f;
        
        // Disable player movement script
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
        }
        
        // Disable CharacterController
        if (characterController != null)
        {
            characterControllerWasEnabled = characterController.enabled;
            characterController.enabled = false;
        }
        
        // Pause the fire timer
        if (fireTimer != null)
        {
            fireTimerWasRunning = fireTimer.IsRunning();
            if (fireTimerWasRunning)
            {
                fireTimer.PauseTimer();
            }
        }
        
        // Disable additional scripts (like flammable objects)
        scriptsOriginalState.Clear();
        foreach (MonoBehaviour script in scriptsToDisable)
        {
            if (script != null)
            {
                scriptsOriginalState.Add(script.enabled);
                script.enabled = false;
            }
        }
        
        Debug.Log("Game state frozen for cutscene");
    }
    
    private void UnfreezeGameState()
    {
        // ... (Unchanged) ...
        // Stop spark particles when unfreezing
        sparksRunning = false;
        
        // Unfreeze time
        Time.timeScale = 1f;
        
        // Re-enable player movement script
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = true;
        }
        
        // Re-enable CharacterController
        if (characterController != null)
        {
            characterController.enabled = characterControllerWasEnabled;
        }
        
        // Resume the fire timer
        if (fireTimer != null && fireTimerWasRunning)
        {
            fireTimer.ResumeTimer();
        }
        
        // Re-enable additional scripts
        for (int i = 0; i < scriptsToDisable.Count && i < scriptsOriginalState.Count; i++)
        {
            if (scriptsToDisable[i] != null)
            {
                scriptsToDisable[i].enabled = scriptsOriginalState[i];
            }
        }
        
        Debug.Log("Game state unfrozen - level started!");
    }
    
    private void EndCutscene()
    {
        // ... (Unchanged) ...
        // Return NPC to idle
        PlayAnimation(idleAnimationName);
        
        // Unfreeze everything and start the level
        UnfreezeGameState();
        
        cutsceneActive = false;
        
        Debug.Log("Fire surge cutscene complete! Level started.");
    }
    
    // Public method to manually trigger cutscene (e.g., from a trigger collider)
    public void TriggerCutscene()
    {
        StartCutscene();
    }
    
    // Public method to skip cutscene (optional)
    public void SkipCutscene()
    {
        if (cutsceneActive)
        {
            StopAllCoroutines();
            
            // Stop spark particles
            sparksRunning = false;
            
            // NEW: Stop Meta Voice speech
            if (enableTTS && ttsSpeaker != null)
            {
                ttsSpeaker.Stop();
            }
            // Removed RT-Voice Silence() call
            
            // Set lights to final state
            foreach (Light light in sceneLights)
            {
                if (light != null)
                {
                    light.enabled = lightsOnAfterFlicker;
                }
            }
            
            // Hide dialogue
            if (dialogueUI != null)
                dialogueUI.SetActive(false);
            
            EndCutscene();
        }
    }

    void OnDestroy()
    {
        // NEW: Stop Meta Voice speech
        if (enableTTS && ttsSpeaker != null)
        {
            ttsSpeaker.Stop();
        }
        // Removed RT-Voice Silence() call
    }
}