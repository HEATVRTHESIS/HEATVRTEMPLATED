using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class VRPauseMenu : MonoBehaviour
{
    [Header("Menu References")]
    [SerializeField] private Canvas pauseMenuCanvas;
    [SerializeField] private Slider audioSlider;
    [SerializeField] private Toggle muteTTSToggle;
    
    [Header("Audio Sources to Control")]
    [Tooltip("Any AudioSources you want to control (background music, sound effects, etc.)")]
    [SerializeField] private AudioSource[] additionalAudioSources;
    
    [Header("TTS Reference")]
    [Tooltip("Reference to the dialogue system for TTS control")]
    [SerializeField] private VRDialogueSystem dialogueSystem;
    
    // Input action for the pause button (menu button on left controller)
    private InputAction pauseAction;
    
    private bool isPaused = false;
    private float currentVolume = 1f;
    private bool isTTSMuted = false;
    private float timeScaleBeforePause = 1f; // Store time scale when pause menu opens

    private void Awake()
    {
        // Create an input action for the left controller menu button
        pauseAction = new InputAction(
            name: "Pause",
            binding: "<XRController>{LeftHand}/menuButton"
        );
    }

    private void OnEnable()
    {
        pauseAction.Enable();
        pauseAction.performed += OnPauseButtonPressed;
    }

    private void OnDisable()
    {
        pauseAction.performed -= OnPauseButtonPressed;
        pauseAction.Disable();
    }

    void Start()
    {
        // Initialize menu as hidden
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.enabled = false;
        }
        
        // Auto-find the dialogue system if not assigned
        if (dialogueSystem == null)
        {
            dialogueSystem = FindObjectOfType<VRDialogueSystem>();
            if (dialogueSystem != null)
            {
                Debug.Log("Found VRDialogueSystem");
            }
        }
        
        // Get initial volume from AudioListener
        currentVolume = AudioListener.volume;
        
        // Setup audio slider
        if (audioSlider != null)
        {
            audioSlider.minValue = 0f;
            audioSlider.maxValue = 1f;
            audioSlider.value = currentVolume;
            audioSlider.onValueChanged.AddListener(OnAudioSliderChanged);
        }
        
        // Setup mute TTS toggle
        if (muteTTSToggle != null)
        {
            // Sync with the current mute state from VRDialogueSystem
            isTTSMuted = VRDialogueSystem.IsTTSMuted;
            muteTTSToggle.isOn = isTTSMuted;
            muteTTSToggle.onValueChanged.AddListener(OnMuteTTSToggled);
        }
    }

    void Update()
    {
        // Continuously apply volume while paused to ensure it sticks
        if (isPaused)
        {
            ApplyVolume();
        }
    }

    // This function is called every time the pause button is pressed
    private void OnPauseButtonPressed(InputAction.CallbackContext context)
    {
        TogglePauseMenu();
    }

    void TogglePauseMenu()
    {
        isPaused = !isPaused;
        
        if (isPaused)
        {
            OpenPauseMenu();
        }
        else
        {
            ClosePauseMenu();
        }
    }

    void OpenPauseMenu()
    {
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.enabled = true;
        }
        
        // Save the current time scale before pausing
        // (This could be 0 if dialogue is active, or 1 if not)
        timeScaleBeforePause = Time.timeScale;
        
        // Pause time (ensure it's 0 for the pause menu)
        Time.timeScale = 0f;
        
        // Update slider to current volume
        if (audioSlider != null)
        {
            audioSlider.value = currentVolume;
        }
        
        // Update mute toggle
        if (muteTTSToggle != null)
        {
            muteTTSToggle.isOn = isTTSMuted;
        }
    }

    void ClosePauseMenu()
    {
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.enabled = false;
        }
        
        // Restore time to whatever it was before the pause menu opened
        // (If dialogue was active, this will restore it back to 0)
        Time.timeScale = timeScaleBeforePause;
        
        Debug.Log($"VRPauseMenu: Restored time scale to {timeScaleBeforePause}");
    }

    void OnAudioSliderChanged(float value)
    {
        currentVolume = value;
        ApplyVolume();
    }

    void OnMuteTTSToggled(bool isMuted)
    {
        isTTSMuted = isMuted;
        
        // Set the mute state in VRDialogueSystem
        VRDialogueSystem.IsTTSMuted = isMuted;
        
        if (isTTSMuted)
        {
            // Stop any currently playing Meta Voice TTS
            if (dialogueSystem != null && dialogueSystem.ttsSpeaker != null)
            {
                dialogueSystem.ttsSpeaker.Stop();
            }
            Debug.Log("TTS Muted");
        }
        else
        {
            Debug.Log("TTS Unmuted");
        }
    }

    void ApplyVolume()
    {
        // Apply to master volume
        AudioListener.volume = currentVolume;
        
        // Apply to any additional AudioSources (background music, sound effects, etc.)
        if (additionalAudioSources != null)
        {
            foreach (AudioSource audioSource in additionalAudioSources)
            {
                if (audioSource != null)
                {
                    audioSource.volume = currentVolume;
                }
            }
        }
        
        // Note: Meta Voice SDK's TTSSpeaker handles its own audio internally
        // Volume is controlled via AudioListener.volume or the TTSSpeaker's AudioSource
    }

    // Public methods for UI buttons if needed
    public void ResumeButton()
    {
        isPaused = false;
        ClosePauseMenu();
    }

    public void QuitButton()
    {
        // Resume time before quitting
        Time.timeScale = 1f;
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    void OnDestroy()
    {
        // Clean up listeners
        if (audioSlider != null)
        {
            audioSlider.onValueChanged.RemoveListener(OnAudioSliderChanged);
        }
        
        if (muteTTSToggle != null)
        {
            muteTTSToggle.onValueChanged.RemoveListener(OnMuteTTSToggled);
        }
        
        // Ensure time is restored
        Time.timeScale = 1f;
    }
}