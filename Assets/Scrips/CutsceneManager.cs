using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Video;
using UnityEngine.UI;
using TMPro;

public class CutsceneManager : MonoBehaviour
{
    [Header("Cutscene Settings")]
    [SerializeField] private GameObject cutsceneCanvas;
    [SerializeField] private RawImage cutsceneDisplay;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private VideoClip cutsceneVideo;
    [SerializeField] private RenderTexture renderTexture;
    
    [Header("Dome Theater Setup")]
    [SerializeField] private Transform theaterPosition;
    [SerializeField] private Transform playerRig;
    [SerializeField] private Transform originalPlayerPosition;
    
    [Header("Skip UI")]
    [SerializeField] private GameObject skipUI;
    [SerializeField] private TextMeshProUGUI skipText;
    [SerializeField] private float skipUIFadeTime = 0.5f;
    [SerializeField] private float skipUIDelay = 2f;
    
    [Header("Cutscene Control")]
    [SerializeField] private bool playOnStart = false;
    [SerializeField] private float cutsceneEndDelay = 1f;
    [SerializeField] private bool returnToOriginalPosition = true;
    
    [Header("Input Actions")]
    [SerializeField] private InputActionAsset inputActions;
    private InputAction acceptAction;
    
    private SceneTransitionManager transitionManager;
    private CanvasGroup skipUICanvasGroup;
    private bool cutsceneSkipped = false;
    private bool cutsceneStarted = false;
    private bool cutscenePlaying = false;
    private Vector3 storedPlayerPosition;
    private Quaternion storedPlayerRotation;
    
    public System.Action OnCutsceneStart;
    public System.Action OnCutsceneEnd;
    public System.Action OnCutsceneSkip;
    
    private void Awake()
    {
        
        if (skipUI != null)
        {
            skipUICanvasGroup = skipUI.GetComponent<CanvasGroup>();
            if (skipUICanvasGroup == null)
                skipUICanvasGroup = skipUI.AddComponent<CanvasGroup>();
        }
        
        SetupInputActions();
        
        InitializeUI();
        
        if (originalPlayerPosition == null && playerRig != null)
        {
            GameObject posMarker = new GameObject("OriginalPlayerPosition");
            posMarker.transform.position = playerRig.position;
            posMarker.transform.rotation = playerRig.rotation;
            posMarker.transform.SetParent(transform);
            originalPlayerPosition = posMarker.transform;
        }
    }
    
    private void SetupInputActions()
    {
        if (inputActions != null)
        {
            acceptAction = inputActions.FindAction("Accept");
            if (acceptAction == null)
            {
                Debug.LogError("Accept action not found in InputActionAsset!");
            }
        }
        else
        {
            Debug.LogError("InputActionAsset not assigned!");
        }
    }
    
    private void InitializeUI()
    {
        if (cutsceneCanvas != null)
            cutsceneCanvas.SetActive(false);
            
        if (skipUI != null)
        {
            skipUI.SetActive(false);
            skipUICanvasGroup.alpha = 0f;
        }
        
        if (videoPlayer != null && renderTexture != null && cutsceneDisplay != null)
        {
            videoPlayer.targetTexture = renderTexture;
            cutsceneDisplay.texture = renderTexture;
            
            if (cutsceneVideo != null)
                videoPlayer.clip = cutsceneVideo;
        }
    }
    
    private void OnEnable()
    {
        if (acceptAction != null)
        {
            acceptAction.Enable();
            acceptAction.performed += OnAcceptPressed;
        }
    }
    
    private void OnDisable()
    {
        if (acceptAction != null)
        {
            acceptAction.performed -= OnAcceptPressed;
            acceptAction.Disable();
        }
    }
    
    private void Start()
    {
        if (transitionManager == null)
        {
            transitionManager = FindObjectOfType<SceneTransitionManager>();
            if (transitionManager == null)
            {
                Debug.LogWarning("SceneTransitionManager not found in scene. Fade transitions will not work.");
            }
        }
        
        if (playOnStart)
        {
            PlayCutscene();
        }
    }
    
    public void PlayCutscene()
    {
        if (!cutscenePlaying)
        {
            StartCoroutine(CutsceneSequence());
        }
        else
        {
            Debug.LogWarning("Cutscene is already playing!");
        }
    }
    
    public void PlayCutscene(VideoClip video)
    {
        if (video != null)
        {
            cutsceneVideo = video;
            if (videoPlayer != null)
                videoPlayer.clip = video;
        }
        PlayCutscene();
    }
    
    private IEnumerator CutsceneSequence()
    {
        cutscenePlaying = true;
        cutsceneSkipped = false;
        cutsceneStarted = false;
        
        OnCutsceneStart?.Invoke();
        
        if (playerRig != null)
        {
            storedPlayerPosition = playerRig.position;
            storedPlayerRotation = playerRig.rotation;
        }
        
        if (playerRig != null && theaterPosition != null)
        {
            yield return StartCoroutine(TeleportToTheater());
        }
        
        yield return StartCoroutine(PlayCutsceneVideo());
        
        if (returnToOriginalPosition && playerRig != null)
        {
            yield return StartCoroutine(ReturnPlayerToOriginalPosition());
        }
        
        cutscenePlaying = false;
        
        if (cutsceneSkipped)
            OnCutsceneSkip?.Invoke();
        else
            OnCutsceneEnd?.Invoke();
    }
    
    private IEnumerator TeleportToTheater()
    {
        if (transitionManager != null)
        {
            yield return StartCoroutine(FadeToBlack());
        }
        
        playerRig.position = theaterPosition.position;
        playerRig.rotation = theaterPosition.rotation;
        
        yield return new WaitForSeconds(0.2f);
        
        if (transitionManager != null)
        {
            yield return StartCoroutine(FadeFromBlack());
        }
    }
    
    private IEnumerator ReturnPlayerToOriginalPosition()
    {
        if (transitionManager != null)
        {
            yield return StartCoroutine(FadeToBlack());
        }
        
        Vector3 targetPos = returnToOriginalPosition && originalPlayerPosition != null ? 
                           originalPlayerPosition.position : storedPlayerPosition;
        Quaternion targetRot = returnToOriginalPosition && originalPlayerPosition != null ? 
                              originalPlayerPosition.rotation : storedPlayerRotation;
        
        playerRig.position = targetPos;
        playerRig.rotation = targetRot;
        
        yield return new WaitForSeconds(0.2f);
        
        if (transitionManager != null)
        {
            yield return StartCoroutine(FadeFromBlack());
        }
    }
    
    private IEnumerator PlayCutsceneVideo()
    {
        cutsceneStarted = true;
        
        if (cutsceneCanvas != null)
            cutsceneCanvas.SetActive(true);
            
        if (videoPlayer != null && cutsceneVideo != null)
        {
            videoPlayer.clip = cutsceneVideo;
            videoPlayer.Play();
            
            yield return new WaitForSeconds(skipUIDelay);
            ShowSkipUI();
            
            while (videoPlayer.isPlaying && !cutsceneSkipped)
            {
                yield return null;
            }
            
            if (videoPlayer.isPlaying)
                videoPlayer.Stop();
        }
        else
        {
            yield return new WaitForSeconds(5f);
        }
        
        yield return StartCoroutine(HideSkipUI());
        
        if (cutsceneCanvas != null)
            cutsceneCanvas.SetActive(false);
            
        if (!cutsceneSkipped)
            yield return new WaitForSeconds(cutsceneEndDelay);
    }
    
    private void ShowSkipUI()
    {
        if (skipUI != null && !cutsceneSkipped)
        {
            skipUI.SetActive(true);
            StartCoroutine(FadeInSkipUI());
        }
    }
    
    private IEnumerator FadeInSkipUI()
    {
        float elapsedTime = 0f;
        while (elapsedTime < skipUIFadeTime)
        {
            elapsedTime += Time.deltaTime;
            skipUICanvasGroup.alpha = Mathf.Clamp01(elapsedTime / skipUIFadeTime);
            yield return null;
        }
        skipUICanvasGroup.alpha = 1f;
    }
    
    private IEnumerator HideSkipUI()
    {
        if (skipUI != null && skipUI.activeInHierarchy)
        {
            float elapsedTime = 0f;
            while (elapsedTime < skipUIFadeTime)
            {
                elapsedTime += Time.deltaTime;
                skipUICanvasGroup.alpha = 1f - Mathf.Clamp01(elapsedTime / skipUIFadeTime);
                yield return null;
            }
            skipUICanvasGroup.alpha = 0f;
            skipUI.SetActive(false);
        }
    }
    
    private void OnAcceptPressed(InputAction.CallbackContext context)
    {
        if (cutsceneStarted && !cutsceneSkipped && cutscenePlaying)
        {
            SkipCutscene();
        }
    }
    
    private void SkipCutscene()
    {
        cutsceneSkipped = true;
        Debug.Log("Skipped");
    }
    
    private IEnumerator FadeToBlack()
    {
        if (transitionManager == null)
        {
            transitionManager = FindObjectOfType<SceneTransitionManager>();
        }
        
        if (transitionManager != null && transitionManager.fadeCanvasGroup != null)
        {
            transitionManager.fadeCanvasGroup.blocksRaycasts = true;
            
            float elapsedTime = 0f;
            while (elapsedTime < transitionManager.fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                transitionManager.fadeCanvasGroup.alpha = Mathf.Clamp01(elapsedTime / transitionManager.fadeDuration);
                yield return null;
            }
            
            transitionManager.fadeCanvasGroup.alpha = 1f;
        }
        else
        {
            Debug.LogWarning("SceneTransitionManager or fadeCanvasGroup not available. Skipping fade to black.");
            yield return new WaitForSeconds(0.5f);
        }
    }
    
    private IEnumerator FadeFromBlack()
    {
        if (transitionManager == null)
        {
            transitionManager = FindObjectOfType<SceneTransitionManager>();
        }
        
        if (transitionManager != null && transitionManager.fadeCanvasGroup != null)
        {
            float elapsedTime = 0f;
            while (elapsedTime < transitionManager.fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                transitionManager.fadeCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsedTime / transitionManager.fadeDuration);
                yield return null;
            }
            
            transitionManager.fadeCanvasGroup.alpha = 0f;
            transitionManager.fadeCanvasGroup.blocksRaycasts = false;
        }
        else
        {
            Debug.LogWarning("SceneTransitionManager or fadeCanvasGroup not available. Skipping fade from black.");
            yield return new WaitForSeconds(0.5f);
        }
    }
    

    public bool IsCutscenePlaying()
    {
        return cutscenePlaying;
    }
    

    public void StopCutscene()
    {
        if (cutscenePlaying)
        {
            cutsceneSkipped = true;
        }
    }
}