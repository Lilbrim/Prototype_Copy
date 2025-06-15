using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    [Header("Transition Settings")]
    [Tooltip("Fade Duration")]
    [Range(0.1f, 5f)]
    public float fadeDuration = 1.0f;
    public CanvasGroup fadeCanvasGroup;
    public bool dontDestroyOnLoad = true;
    public Camera vrCamera;
    
    private static SceneTransitionManager _instance;
    public static SceneTransitionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.LogError("SceneTransitionManager is not available");
            }
            return _instance;
        }
    }
    
    private bool _isTransitioning = false;
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        
        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }
        
        if (fadeCanvasGroup == null)
        {
            CreateFadeCanvas();
        }
        
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
    }
    
    private void Start()
    {
        if (vrCamera == null)
        {
            vrCamera = Camera.main;
            if (vrCamera == null)
            {
                Debug.LogWarning("No VR camera assigned and couldn't find main camera. VR fade transitions may not work correctly.");
            }
            else
            {
                UpdateCanvasForVR();
            }
        }
    }
    
    private void CreateFadeCanvas()
    {
        GameObject canvasObj = new GameObject("FadeCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = vrCamera; 
        canvas.planeDistance = 0.5f; 
        
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        
        canvas.sortingOrder = 999;
        
        GameObject panelObj = new GameObject("BlackFadePanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = Color.black;
        
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;
        
        fadeCanvasGroup = panelObj.AddComponent<CanvasGroup>();
        
        canvasObj.transform.SetParent(transform);
    }
    
    private void UpdateCanvasForVR()
    {
        if (fadeCanvasGroup == null || vrCamera == null) return;
        
        Canvas canvas = fadeCanvasGroup.transform.parent.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = vrCamera;
            canvas.planeDistance = 0.5f;
        }
    }
    
    
    
    
    
    public void LoadSceneWithTransition(string sceneName)
    {
        if (!_isTransitioning)
        {
            StartCoroutine(Transition(sceneName));
        }
        else
        {
            Debug.LogWarning("Scene transition already in progress!");
        }
    }
    
    
    
    
    
    public void LoadSceneWithTransition(int sceneIndex)
    {
        if (!_isTransitioning)
        {
            StartCoroutine(Transition(sceneIndex));
        }
        else
        {
            Debug.LogWarning("Scene transition already in progress!");
        }
    }
    
    private IEnumerator Transition(string sceneName)
    {
        _isTransitioning = true;
        
        yield return StartCoroutine(FadeToBlack());
        
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        
        yield return null; 
        vrCamera = Camera.main;
        UpdateCanvasForVR();
        
        yield return new WaitForSeconds(0.1f);
        
        yield return StartCoroutine(FadeFromBlack());
        
        _isTransitioning = false;
    }

    private IEnumerator Transition(int sceneIndex)
    {
        _isTransitioning = true;

        yield return StartCoroutine(FadeToBlack());
        
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneIndex);
        
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        
        yield return null; 
        vrCamera = Camera.main;
        UpdateCanvasForVR();
        
        yield return new WaitForSeconds(0.1f);
        
        yield return StartCoroutine(FadeFromBlack());
        
        _isTransitioning = false;
    }
    
    private IEnumerator FadeToBlack()
    {
        fadeCanvasGroup.blocksRaycasts = true;
        
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            yield return null;
        }
        
        fadeCanvasGroup.alpha = 1f;
    }
    
    private IEnumerator FadeFromBlack()
    {
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsedTime / fadeDuration);
            yield return null;
        }
        
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
    }
}