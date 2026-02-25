using UnityEngine;
using System.Collections;

public class SceneFadeManager : MonoBehaviour
{
    [Header("Fade Settings")]
    public float fadeInTime = 1f;

    [Header("VR Settings")]
    public float distanceFromCamera = 0.5f;
    public float planeSize = 4f;

    private CanvasGroup fadeCanvas;
    private GameObject fadeGO;
    private Camera vrCamera;

    void Start()
    {
        vrCamera = Camera.main;
        SetupFadeCanvas();
        StartCoroutine(FadeIn());
    }

    void SetupFadeCanvas()
    {
        fadeGO = new GameObject("FadeCanvas");

        Canvas canvas = fadeGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 9999;

        fadeCanvas = fadeGO.AddComponent<CanvasGroup>();
        fadeCanvas.alpha = 1f;
        fadeCanvas.blocksRaycasts = false;
        fadeCanvas.interactable = false;

        RectTransform canvasRect = fadeGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1f, 1f);

        GameObject panel = new GameObject("FadePanel");
        panel.transform.SetParent(fadeGO.transform, false);

        UnityEngine.UI.Image image = panel.AddComponent<UnityEngine.UI.Image>();
        image.color = Color.black;
        image.raycastTarget = false;

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;

        PositionInFrontOfCamera();
    }

    void PositionInFrontOfCamera()
    {
        if (vrCamera == null) return;

        fadeGO.transform.position = vrCamera.transform.position + vrCamera.transform.forward * distanceFromCamera;
        fadeGO.transform.rotation = vrCamera.transform.rotation;
        fadeGO.transform.localScale = new Vector3(planeSize, planeSize, 1f);
        fadeGO.transform.SetParent(vrCamera.transform, true);
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
        Destroy(fadeGO);
    }
}