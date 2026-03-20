using UnityEngine;

public class MainMenuHandler : MonoBehaviour
{
    public void ExitGame()
    {
        // This logs a message to the console so you know the button works
        Debug.Log("Exit Button Pressed. Closing Application...");

        // This closes the app on the Quest 2 hardware
        Application.Quit();

        // This stops the play mode if you are testing inside the Unity Editor
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}