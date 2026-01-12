using UnityEngine;
using System.Collections;

public class SceneFadeManager : MonoBehaviour
{
    [Header("Fade Settings")]
    public float fadeInTime = 1f;
    
    private CanvasGroup fadeCanvas;
    
    void Start()
    {
        SetupFadeCanvas();
        StartCoroutine(FadeIn());
    }
    
    void SetupFadeCanvas()
    {
        GameObject fadeGO = new GameObject("FadeCanvas");
        Canvas canvas = fadeGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // Even higher to ensure it's on top
        
        fadeCanvas = fadeGO.AddComponent<CanvasGroup>();
        fadeCanvas.alpha = 1f; // Start black
        fadeCanvas.blocksRaycasts = false;
        fadeCanvas.interactable = false;
        
        GameObject panel = new GameObject("FadePanel");
        panel.transform.SetParent(fadeGO.transform);
        
        UnityEngine.UI.Image image = panel.AddComponent<UnityEngine.UI.Image>();
        image.color = Color.black;
        image.raycastTarget = false; // Don't block raycasts
        
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }
    
    IEnumerator FadeIn()
    {
        yield return new WaitForSecondsRealtime(0.1f);
        
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeInTime)
        {
            elapsedTime += Time.unscaledDeltaTime;
            fadeCanvas.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeInTime);
            yield return null;
        }
        
        fadeCanvas.alpha = 0f;
        Destroy(fadeCanvas.gameObject);
    }
}