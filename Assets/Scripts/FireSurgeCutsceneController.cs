using UnityEngine;
using UnityEngine.XR;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages a fire surge cutscene with lightning effects, light flickering, and NPC dialogue.
/// Freezes time and player movement during the cutscene to prevent premature gameplay.
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
    
    [Header("UI References")]
    public GameObject dialogueUI;
    public TMPro.TextMeshProUGUI dialogueText;
    
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
    
    void Start()
    {
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
    
    public void StartCutscene()
    {
        if (cutsceneActive) return;
        
        cutsceneActive = true;
        FreezeGameState();
        StartCoroutine(PlayCutsceneSequence());
    }
    
    private IEnumerator PlayCutsceneSequence()
    {
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
        
        // Phase 4: NPC Dialogue
        yield return StartCoroutine(PlayDialogue());
        
        // Phase 5: End Cutscene and Start Level
        EndCutscene();
    }
    
    private void PlayLightningSound()
    {
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
        // Show dialogue UI
        if (dialogueUI != null)
            dialogueUI.SetActive(true);
        
        // Play through all dialogue lines
        foreach (string line in dialogueLines)
        {
            // Switch to talking animation
            PlayAnimation(talkingAnimationName);
            
            // Display the dialogue
            if (dialogueText != null)
                dialogueText.text = line;
            
            // Wait for the line duration (using realtime since timescale is 0)
            float lineDuration = CalculateLineDuration(line);
            yield return new WaitForSecondsRealtime(lineDuration);
            
            // Brief pause between lines with idle animation
            PlayAnimation(idleAnimationName);
            yield return new WaitForSecondsRealtime(timeBetweenLines);
        }
        
        // Hide dialogue UI
        if (dialogueUI != null)
            dialogueUI.SetActive(false);
        
        Debug.Log("Dialogue complete!");
    }
    
    private void PlayAnimation(string animationName)
    {
        if (npcAnimator != null && !string.IsNullOrEmpty(animationName))
        {
            npcAnimator.Play(animationName);
        }
    }
    
    private float CalculateLineDuration(string line)
    {
        // Rough estimate: 0.05 seconds per character, minimum 2 seconds
        float duration = Mathf.Max(2f, line.Length * 0.05f);
        return duration;
    }
    
    private void FreezeGameState()
    {
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
}