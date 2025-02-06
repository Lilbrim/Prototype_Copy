using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Pause : MonoBehaviour
{
    public GameObject pauseUI;
    private bool activePauseUI = false; 
    void Start()
    {
        pauseUI.SetActive(false); 
        Time.timeScale = 1; 
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
        Time.timeScale = activePauseUI ? 0 : 1; 
    }

    public void Menu()
    {
        Time.timeScale = 1; 
        SceneManager.LoadScene("Start");
    }
}
