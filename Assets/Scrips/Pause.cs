using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class Pause : MonoBehaviour
{
    [Header("Input Action")]
    [SerializeField] private InputActionAsset inputActions;
    private InputAction pauseAction;
    
    [Header("UI References")]
    public GameObject pauseUI;
    public GameObject mainPausePanel;
    public GameObject optionsPanel;
    
    [Header("Height Adjustment")]
    public Transform playerRig;
    public Slider heightSlider;
    public TextMeshProUGUI heightText;
    public float minHeight = 0.5f;
    public float maxHeight = 1.5f;
    [SerializeField] private float defaultHeight = 1.0f;
    private float initialScale;
    private const string HEIGHT_PREF_KEY = "PlayerHeight";
    
    [Header("Right-Hand Dominance")]
    public Toggle rightHandToggle;
    private const string RIGHT_HAND_PREF_KEY = "RightHandDominant";
    private const string HAND_DOMINANCE_SET_KEY = "HandDominanceAutoSet";
    [SerializeField] private bool defaultRightHand = true;
    
    [Header("Menu Following Settings")]
    public float followThreshold = 30f; 
    public float followSpeed = 5f; 
    public float minDistanceToCamera = 1.5f; 
    public float maxDistanceToCamera = 2.5f; 
    
    private bool activePauseUI = false;
    private Vector3 originalPlayerPosition;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool isInDirectView = true;
    
    private void Awake()
    {
        if (inputActions != null)
        {
            pauseAction = inputActions.FindAction("Pause");
        }
    }
    
    void Start()
    {
        pauseUI.SetActive(false);
        optionsPanel.SetActive(false);
        mainPausePanel.SetActive(true);
        Time.timeScale = 1;
        
        if (playerRig != null && heightSlider != null)
        {
            float savedHeight = PlayerPrefs.GetFloat(HEIGHT_PREF_KEY, defaultHeight);
            
            savedHeight = Mathf.Clamp(savedHeight, minHeight, maxHeight);
            
            initialScale = savedHeight;
            
            heightSlider.minValue = minHeight;
            heightSlider.maxValue = maxHeight;
            heightSlider.value = savedHeight;
            heightSlider.onValueChanged.AddListener(OnHeightChanged);
            
            Vector3 newScale = playerRig.localScale;
            newScale.y = savedHeight;
            playerRig.localScale = newScale;
            
            UpdateHeightText();
        }
        
        if (rightHandToggle != null)
        {
            bool savedRightHand = PlayerPrefs.GetInt(RIGHT_HAND_PREF_KEY, defaultRightHand ? 1 : 0) == 1;
            rightHandToggle.isOn = savedRightHand;
            rightHandToggle.onValueChanged.AddListener(OnRightHandToggleChanged);
            
            ApplyRightHandDominanceToStanceManager(savedRightHand);
        }
        
        if (inputActions != null)
        {
            inputActions.Enable();
        }
    }
    
    private void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.Enable();
            pauseAction.performed += OnPauseAction;
        }
    }
    
    private void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.performed -= OnPauseAction;
            pauseAction.Disable();
        }
    }
    
    private void OnPauseAction(InputAction.CallbackContext context)
    {
        DisplayPauseUI();
    }

    void Update()
    {
        if (activePauseUI && Camera.main != null)
        {
            UpdateMenuPosition();
        }
    }
    
    private void UpdateMenuPosition()
    {
        Transform cameraTransform = Camera.main.transform;
        
        Vector3 directionToMenu = (pauseUI.transform.position - cameraTransform.position).normalized;
        float angle = Vector3.Angle(cameraTransform.forward, directionToMenu);
        
        float currentDistance = Vector3.Distance(pauseUI.transform.position, cameraTransform.position);
        currentDistance = Mathf.Clamp(currentDistance, minDistanceToCamera, maxDistanceToCamera);
        
        if (angle > followThreshold)
        {
            isInDirectView = false;
            
            targetPosition = cameraTransform.position + cameraTransform.forward * currentDistance;
            targetRotation = Quaternion.LookRotation(targetPosition - cameraTransform.position);
            
            pauseUI.transform.position = Vector3.Lerp(pauseUI.transform.position, targetPosition, Time.unscaledDeltaTime * followSpeed);
            pauseUI.transform.rotation = Quaternion.Lerp(pauseUI.transform.rotation, targetRotation, Time.unscaledDeltaTime * followSpeed);
        }
        else
        {
            isInDirectView = true;
            
            if (currentDistance < minDistanceToCamera || currentDistance > maxDistanceToCamera)
            {
                Vector3 newPosition = cameraTransform.position + directionToMenu * Mathf.Clamp(currentDistance, minDistanceToCamera, maxDistanceToCamera);
                pauseUI.transform.position = Vector3.Lerp(pauseUI.transform.position, newPosition, Time.unscaledDeltaTime * followSpeed);
            }
        }
    }

    public void PausedButtonPressed(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            DisplayPauseUI();
        }
    }

    public void DisplayPauseUI()
    {
        activePauseUI = !activePauseUI;
        pauseUI.SetActive(activePauseUI);
        
        mainPausePanel.SetActive(true);
        optionsPanel.SetActive(false);
        
        Time.timeScale = activePauseUI ? 0 : 1;
        
        if (activePauseUI)
        {
            PositionPauseMenuInFrontOfPlayer();
        }
    }

    private void PositionPauseMenuInFrontOfPlayer()
    {
        if (Camera.main != null)
        {
            Transform cameraTransform = Camera.main.transform;
            Vector3 position = cameraTransform.position + cameraTransform.forward * minDistanceToCamera; 
            pauseUI.transform.position = position;
            pauseUI.transform.rotation = Quaternion.LookRotation(pauseUI.transform.position - cameraTransform.position);
        }
    }

    public void ShowOptions()
    {
        mainPausePanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    public void HideOptions()
    {
        mainPausePanel.SetActive(true);
        optionsPanel.SetActive(false);
    }

    public void OnHeightChanged(float value)
    {
        if (playerRig != null)
        {
            Vector3 newScale = playerRig.localScale;
            newScale.y = value;
            playerRig.localScale = newScale;
            
            PlayerPrefs.SetFloat(HEIGHT_PREF_KEY, value);
            PlayerPrefs.Save(); 
        }
        UpdateHeightText();
    }

    private void UpdateHeightText()
    {
        if (heightText != null)
        {
            heightText.text = $"Height: {heightSlider.value:F2}x";
        }
    }

    public void ResetHeight()
    {
        if (heightSlider != null)
        {
            heightSlider.value = defaultHeight;
            
            PlayerPrefs.SetFloat(HEIGHT_PREF_KEY, defaultHeight);
            PlayerPrefs.Save();
        }
    }

    public void OnRightHandToggleChanged(bool isRightHand)
    {
        PlayerPrefs.SetInt(RIGHT_HAND_PREF_KEY, isRightHand ? 1 : 0);
        PlayerPrefs.Save();
        
        ApplyRightHandDominanceToStanceManager(isRightHand);
        
        Debug.Log($"Right-hand dominance manually set to: {isRightHand}");
    }
    
    public void SetHandDominanceFromBatonPickup(bool isRightHand)
    {
        if (rightHandToggle != null)
        {
            rightHandToggle.onValueChanged.RemoveListener(OnRightHandToggleChanged);
            rightHandToggle.isOn = isRightHand;
            rightHandToggle.onValueChanged.AddListener(OnRightHandToggleChanged);
        }
        
        PlayerPrefs.SetInt(RIGHT_HAND_PREF_KEY, isRightHand ? 1 : 0);
        PlayerPrefs.SetInt(HAND_DOMINANCE_SET_KEY, 1);
        PlayerPrefs.Save();
        
        ApplyRightHandDominanceToStanceManager(isRightHand);
        
        Debug.Log($"Hand dominance auto-detected as: {(isRightHand ? "Right" : "Left")} handed");
    }
    
    private void ApplyRightHandDominanceToStanceManager(bool isRightHand)
    {
        StanceManager stanceManager = StanceManager.Instance;
        if (stanceManager != null)
        {
            stanceManager.SetRightHandDominant(isRightHand);
        }
        else
        {
            stanceManager = FindObjectOfType<StanceManager>();
            if (stanceManager != null)
            {
                stanceManager.SetRightHandDominant(isRightHand);
            }
            else
            {
                Debug.LogWarning("StanceManager not found in scene. Right-hand dominance setting will be applied when StanceManager is available.");
            }
        }
    }
    
    public void ResetRightHandDominance()
    {
        if (rightHandToggle != null)
        {
            rightHandToggle.isOn = defaultRightHand;
            
            PlayerPrefs.SetInt(RIGHT_HAND_PREF_KEY, defaultRightHand ? 1 : 0);
            PlayerPrefs.SetInt(HAND_DOMINANCE_SET_KEY, 0); 
            PlayerPrefs.Save();
            
            ApplyRightHandDominanceToStanceManager(defaultRightHand);
        }
    }
    
    public bool GetRightHandDominance()
    {
        return PlayerPrefs.GetInt(RIGHT_HAND_PREF_KEY, defaultRightHand ? 1 : 0) == 1;
    }

    public void Menu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("Start");
    }

    public void Resume()
    {
        DisplayPauseUI();
    }
}