using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.InputSystem;

public class PauseMenuVR : MonoBehaviour
{
    [Header("Pause Menu")]
    public GameObject pauseMenuCanvas;
    public Slider volumeSlider;
    
    private bool isPaused = false;

    void Start()
    {
        if (pauseMenuCanvas != null)
            pauseMenuCanvas.SetActive(false);
            
        if (volumeSlider != null)
        {
            // Set slider to current volume
            volumeSlider.value = AudioListener.volume;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
    }

    void Update()
    {
        // "P" key for testing in editor (New Input System)
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            TogglePause();
            return;
        }
        
        // Check menu button on both controllers
        UnityEngine.XR.InputDevice leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        UnityEngine.XR.InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        
        bool menuPressed = false;
        
        if (leftHand.isValid)
            leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.menuButton, out menuPressed);
            
        if (!menuPressed && rightHand.isValid)
            rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.menuButton, out menuPressed);
        
        if (menuPressed)
            TogglePause();
    }

    public void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        
        if (pauseMenuCanvas != null)
            pauseMenuCanvas.SetActive(true);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        
        if (pauseMenuCanvas != null)
            pauseMenuCanvas.SetActive(false);
    }

    public void RestartScene()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
    }

    public void ExitGame()
    {
        Time.timeScale = 1f;
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}