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
    
    private bool activePauseUI = false;
    private Vector3 originalPlayerPosition;
    
    void Start()
    {
        pauseUI.SetActive(false);
        optionsPanel.SetActive(false);
        mainPausePanel.SetActive(true);
        Time.timeScale = 1;
        if (playerRig != null && heightSlider != null)
        {
                if (heightSlider != null)
            {
                heightSlider.minValue = minHeight;
                heightSlider.maxValue = maxHeight;
                heightSlider.value = initialScale;
                heightSlider.onValueChanged.AddListener(OnHeightChanged);
                UpdateHeightText();
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
            Vector3 position = cameraTransform.position + cameraTransform.forward * 1.5f; 
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
        Vector3 newScale = transform.localScale;
        newScale.y = value;
        transform.localScale = newScale;
        UpdateHeightText();
    }

    private void UpdateHeightText()
    {
        if (heightText != null)
        {
            heightText.text = $"Height Scale: {heightSlider.value:F2}x";
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