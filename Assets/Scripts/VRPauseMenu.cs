using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public class VRPauseMenu : MonoBehaviour
{
    [Header("Menu References")]
    [SerializeField] private Canvas pauseMenuCanvas;
    [SerializeField] private Slider audioSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Toggle muteTTSToggle;
    
    [Header("Audio Sources to Control")]
    [Tooltip("Background music audio source")]
    [SerializeField] private AudioSource bgmAudioSource;
    
    [Tooltip("Any other AudioSources you want to control (sound effects, etc.)")]
    [SerializeField] private AudioSource[] additionalAudioSources;
    
    [Header("TTS Reference")]
    [Tooltip("Reference to the dialogue system for TTS control")]
    [SerializeField] private VRDialogueSystem dialogueSystem;
    
    private InputAction pauseAction;
    
    private bool isPaused = false;
    private float currentVolume = 1f;
    private float currentBGMVolume = 1f;
    private bool isTTSMuted = false;
    private float timeScaleBeforePause = 1f;

    private void Awake()
{
    pauseAction = new InputAction(
        name: "Pause",
        type: InputActionType.Button
    );
    
    // Meta Quest 2 / Oculus specific bindings
    pauseAction.AddBinding("<XRController>{LeftHand}/menuButton");

    
    // Oculus Touch Controller specific
    pauseAction.AddBinding("<OculusTouchController>{LeftHand}/menu");
 
    
    // Generic XR bindings
    pauseAction.AddBinding("<XRController>/menuButton");
    pauseAction.AddBinding("<XRController>/start");
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
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.enabled = false;
        }
        
        if (dialogueSystem == null)
        {
            dialogueSystem = FindObjectOfType<VRDialogueSystem>();
            if (dialogueSystem != null)
            {
                Debug.Log("Found VRDialogueSystem");
            }
        }
        
        currentVolume = AudioListener.volume;
        
        if (bgmAudioSource != null)
        {
            currentBGMVolume = bgmAudioSource.volume;
        }
        
        if (audioSlider != null)
        {
            audioSlider.minValue = 0f;
            audioSlider.maxValue = 1f;
            audioSlider.value = currentVolume;
            audioSlider.onValueChanged.AddListener(OnAudioSliderChanged);
        }
        
        if (bgmSlider != null)
        {
            bgmSlider.minValue = 0f;
            bgmSlider.maxValue = 1f;
            bgmSlider.value = currentBGMVolume;
            bgmSlider.onValueChanged.AddListener(OnBGMSliderChanged);
        }
        
        if (muteTTSToggle != null)
        {
            isTTSMuted = VRDialogueSystem.IsTTSMuted;
            muteTTSToggle.isOn = isTTSMuted;
            muteTTSToggle.onValueChanged.AddListener(OnMuteTTSToggled);
        }
    }

    void Update()
    {
        if (isPaused)
        {
            ApplyVolume();
            ApplyBGMVolume();
        }
    }

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
        
        timeScaleBeforePause = Time.timeScale;
        Time.timeScale = 0f;
        
        if (audioSlider != null)
        {
            audioSlider.value = currentVolume;
        }
        
        if (bgmSlider != null)
        {
            bgmSlider.value = currentBGMVolume;
        }
        
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
        
        Time.timeScale = timeScaleBeforePause;
        Debug.Log($"VRPauseMenu: Restored time scale to {timeScaleBeforePause}");
    }

    void OnAudioSliderChanged(float value)
    {
        currentVolume = value;
        ApplyVolume();
    }

    void OnBGMSliderChanged(float value)
    {
        currentBGMVolume = value;
        ApplyBGMVolume();
    }

    void OnMuteTTSToggled(bool isMuted)
    {
        isTTSMuted = isMuted;
        VRDialogueSystem.IsTTSMuted = isMuted;
        
        if (isTTSMuted)
        {
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
        AudioListener.volume = currentVolume;
        
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
    }

    void ApplyBGMVolume()
    {
        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = currentBGMVolume;
        }
    }

    public void ResumeButton()
    {
        isPaused = false;
        ClosePauseMenu();
    }

    // NEW: Method to call before restarting scene
    public void RestartSceneButton()
    {
        // Restore time scale BEFORE restarting
        Time.timeScale = 1f;
        isPaused = false;
        
        // Now call your RestartCurrentScene script
        RestartCurrentScene restarter = FindObjectOfType<RestartCurrentScene>();
        if (restarter != null)
        {
            restarter.RestartScene();
        }
        else
        {
            Debug.LogError("RestartCurrentScene script not found!");
        }
    }

    // NEW: Method to call before loading a new scene
    public void LoadSceneButton()
    {
        // Restore time scale BEFORE loading new scene
        Time.timeScale = 1f;
        isPaused = false;
        
        // Now call your SceneLoader script
        SceneLoader loader = FindObjectOfType<SceneLoader>();
        if (loader != null)
        {
            loader.LoadNewLevel();
        }
        else
        {
            Debug.LogError("SceneLoader script not found!");
        }
    }

    public void QuitButton()
    {
        Time.timeScale = 1f;
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    void OnDestroy()
    {
        if (audioSlider != null)
        {
            audioSlider.onValueChanged.RemoveListener(OnAudioSliderChanged);
        }
        
        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveListener(OnBGMSliderChanged);
        }
        
        if (muteTTSToggle != null)
        {
            muteTTSToggle.onValueChanged.RemoveListener(OnMuteTTSToggled);
        }
        
        Time.timeScale = 1f;
    }
}
