using UnityEngine;
using TMPro; // Use TextMeshPro for better text rendering

public class PopupManager : MonoBehaviour
{
    // Reference to the UI Text element to display messages
    public TextMeshProUGUI textElement;

    // Duration the message will be shown
    public float displayDuration = 2f;

    // This method will be called by the Bin script to show a message
    public void ShowMessage(string message)
    {
        // Set the text and make it visible
        textElement.text = message;
        textElement.gameObject.SetActive(true);

        // Start a coroutine to hide the message after a delay
        StartCoroutine(HideMessageAfterDelay(displayDuration));
    }

    // Coroutine to wait and then hide the message
    private System.Collections.IEnumerator HideMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        textElement.gameObject.SetActive(false);
    }
}

