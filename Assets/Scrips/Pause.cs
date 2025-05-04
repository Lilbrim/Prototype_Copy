using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class Pause : MonoBehaviour
{
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
    private float initialScale;
    
    [Header("Menu Following Settings")]
    public float followThreshold = 30f; // Angle in degrees
    public float followSpeed = 5f; // How fast the menu follows
    public float minDistanceToCamera = 1.5f; // Minimum distance to maintain from camera
    public float maxDistanceToCamera = 2.5f; // Maximum distance from camera
    
    private bool activePauseUI = false;
    private Vector3 originalPlayerPosition;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool isInDirectView = true;
    
    void Start()
    {
        pauseUI.SetActive(false);
        optionsPanel.SetActive(false);
        mainPausePanel.SetActive(true);
        Time.timeScale = 1;
        
        if (playerRig != null && heightSlider != null)
        {
            initialScale = playerRig.localScale.y;
            heightSlider.minValue = minHeight;
            heightSlider.maxValue = maxHeight;
            heightSlider.value = initialScale;
            heightSlider.onValueChanged.AddListener(OnHeightChanged);
            UpdateHeightText();
        }
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
        
        // Check if menu is in direct view
        Vector3 directionToMenu = (pauseUI.transform.position - cameraTransform.position).normalized;
        float angle = Vector3.Angle(cameraTransform.forward, directionToMenu);
        
        // Calculate current distance to maintain min/max constraints
        float currentDistance = Vector3.Distance(pauseUI.transform.position, cameraTransform.position);
        currentDistance = Mathf.Clamp(currentDistance, minDistanceToCamera, maxDistanceToCamera);
        
        if (angle > followThreshold)
        {
            // Not looking directly at the menu, so it should follow
            isInDirectView = false;
            
            // Calculate new target position
            targetPosition = cameraTransform.position + cameraTransform.forward * currentDistance;
            targetRotation = Quaternion.LookRotation(targetPosition - cameraTransform.position);
            
            // Smoothly move menu
            pauseUI.transform.position = Vector3.Lerp(pauseUI.transform.position, targetPosition, Time.unscaledDeltaTime * followSpeed);
            pauseUI.transform.rotation = Quaternion.Lerp(pauseUI.transform.rotation, targetRotation, Time.unscaledDeltaTime * followSpeed);
        }
        else
        {
            // Looking directly at the menu, keep it stable
            isInDirectView = true;
            
            // Adjust only the distance if needed
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
            heightSlider.value = initialScale;
        }
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