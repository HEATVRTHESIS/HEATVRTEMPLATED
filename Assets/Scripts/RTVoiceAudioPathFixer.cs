using UnityEngine;
using System.IO;

/// <summary>
/// Add this to your scene temporarily to fix RT-Voice audio path issues
/// Run once, then you can remove it
/// </summary>
public class RTVoiceAudioPathFixer : MonoBehaviour
{
    void Start()
    {
        CleanupRTVoiceAudioPath();
    }

    void CleanupRTVoiceAudioPath()
    {
        // Get the audio path RT-Voice is using
        string audioPath = Path.Combine(Application.persistentDataPath, "RTVoiceAudio");
        
        Debug.Log($"Checking RT-Voice audio path: {audioPath}");
        
        // If the directory exists, clean it up
        if (Directory.Exists(audioPath))
        {
            try
            {
                // Get all items in the directory
                string[] files = Directory.GetFiles(audioPath);
                string[] directories = Directory.GetDirectories(audioPath);
                
                Debug.Log($"Found {files.Length} files and {directories.Length} directories");
                
                // Delete any directories that have .wav extension (these are the corrupt ones)
                foreach (string dir in directories)
                {
                    if (dir.EndsWith(".wav") || dir.EndsWith(".mp3") || dir.EndsWith(".ogg"))
                    {
                        Debug.LogWarning($"Found corrupt directory (should be a file): {dir}");
                        Directory.Delete(dir, true);
                        Debug.Log($"Deleted corrupt directory: {dir}");
                    }
                }
                
                // Optionally: Clear all old audio files
                Debug.Log("Cleaning up old RT-Voice audio files...");
                foreach (string file in files)
                {
                    File.Delete(file);
                }
                
                Debug.Log("RT-Voice audio path cleaned successfully!");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error cleaning RT-Voice audio path: {e.Message}");
            }
        }
        else
        {
            // Create the directory if it doesn't exist
            Directory.CreateDirectory(audioPath);
            Debug.Log($"Created RT-Voice audio directory: {audioPath}");
        }
        
        // Update RT-Voice config
        Crosstales.RTVoice.Util.Config.AUDIOFILE_PATH = audioPath;
        Debug.Log($"Set RT-Voice audio path to: {audioPath}");
    }
}