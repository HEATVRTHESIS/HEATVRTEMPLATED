using UnityEngine;
using System.Collections;

public class SceneFadeManager : MonoBehaviour
{
    [Header("Fade Settings")]
    public float fadeInTime = 1f;
    public bool fadeInOnStart = false; // CHANGED: Default to false to prevent double fade
    
    private CanvasGroup fadeCanvas;
    
    void Start()
    {
        // Find or create fade canvas
        SetupFadeCanvas();
        
        if (fadeInOnStart)
        {
            StartCoroutine(FadeInOnSceneStart());
        }
    }
    
    void SetupFadeCanvas()
    {
        // Look for existing fade canvas first
        GameObject fadeCanvasGO = GameObject.Find("FadeCanvas");
        
        if (fadeCanvasGO != null)
        {
            fadeCanvas = fadeCanvasGO.GetComponent<CanvasGroup>();
            Debug.Log("Found existing fade canvas");
        }
        else
        {
            // Create new fade canvas if none exists
            CreateFadeCanvas();
            Debug.Log("Created new fade canvas for scene");
        }
    }
    
    void CreateFadeCanvas()
    {
        GameObject fadeGO = new GameObject("FadeCanvas");
        Canvas canvas = fadeGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        
        fadeCanvas = fadeGO.AddComponent<CanvasGroup>();
        fadeCanvas.alpha = 1f; // Start black
        fadeCanvas.blocksRaycasts = false;
        
        // Create black panel
        GameObject panel = new GameObject("FadePanel");
        panel.transform.SetParent(fadeGO.transform);
        
        UnityEngine.UI.Image image = panel.AddComponent<UnityEngine.UI.Image>();
        image.color = Color.black;
        
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        
        DontDestroyOnLoad(fadeGO);
    }
    
    IEnumerator FadeInOnSceneStart()
    {
        // Check if we just teleported (to prevent double fade)
        if (PlayerPrefs.GetInt("JustTeleported", 0) == 1)
        {
            PlayerPrefs.DeleteKey("JustTeleported");
            Debug.Log("Skipping scene fade-in because we just teleported");
            
            // Just ensure canvas is transparent
            if (fadeCanvas != null)
            {
                fadeCanvas.alpha = 0f;
            }
            yield break;
        }
        
        // Wait a brief moment for everything to initialize
        yield return new WaitForSeconds(0.1f);
        
        if (fadeCanvas == null)
        {
            Debug.LogWarning("No fade canvas found for scene fade in!");
            yield break;
        }
        
        Debug.Log("Starting scene fade in");
        
        float elapsedTime = 0f;
        float startAlpha = fadeCanvas.alpha;
        
        while (elapsedTime < fadeInTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeInTime);
            fadeCanvas.alpha = alpha;
            yield return null;
        }
        
        fadeCanvas.alpha = 0f;
        Debug.Log("Scene fade in complete");
    }
    
    // Public method to manually trigger fade out (useful for testing)
    public void FadeOut(float duration = 1f)
    {
        if (fadeCanvas != null)
        {
            StartCoroutine(FadeScreen(fadeCanvas.alpha, 1f, duration));
        }
    }
    
    // Public method to manually trigger fade in
    public void FadeIn(float duration = 1f)
    {
        if (fadeCanvas != null)
        {
            StartCoroutine(FadeScreen(fadeCanvas.alpha, 0f, duration));
        }
    }
    
    IEnumerator FadeScreen(float startAlpha, float endAlpha, float duration)
    {
        if (fadeCanvas == null) yield break;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime; // USE UNSCALED TIME
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            fadeCanvas.alpha = alpha;
            yield return null;
        }
        
        fadeCanvas.alpha = endAlpha;
    }
}