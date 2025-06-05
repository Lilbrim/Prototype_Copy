using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Goback : MonoBehaviour
{
    [Header("Button Settings")]
    [Tooltip("The button that will restart the scene when clicked")]
    public Button restartButton;
    
    [Header("Optional Settings")]
    [Tooltip("Show confirmation dialog before restarting")]
    public bool showConfirmation = false;
    
    [Tooltip("Confirmation message (only used if showConfirmation is true)")]
    public string confirmationMessage = "Are you sure you want to restart the scene?";

    private void Start()
    {
        if (restartButton == null)
        {
            restartButton = GetComponent<Button>();
        }
        
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartButtonClicked);
        }
        else
        {
            Debug.LogError("RestartSceneButton: No button component found! Please assign a button or attach this script to a GameObject with a Button component.");
        }
    }
    
    private void OnDestroy()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnRestartButtonClicked);
        }
    }
    
    private void OnRestartButtonClicked()
    {
        if (showConfirmation)
        {
            ShowConfirmationDialog();
        }
        else
        {
            RestartCurrentScene();
        }
    }
    
    private void ShowConfirmationDialog()
    {
        if (Application.isEditor)
        {
            if (UnityEditor.EditorUtility.DisplayDialog("Restart Scene", confirmationMessage, "Yes", "No"))
            {
                RestartCurrentScene();
            }
        }
        else
        {
            Debug.Log("Confirmation dialog not available in build. Restarting scene...");
            RestartCurrentScene();
        }
    }
    
    public void RestartCurrentScene()
    {
        if (SceneTransitionManager.Instance != null)
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            
            Debug.Log($"Restarting scene: {currentSceneName}");
            
            SceneTransitionManager.Instance.LoadSceneWithTransition(currentSceneName);
        }
        else
        {
            
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void RestartCurrentSceneByIndex()
    {
        if (SceneTransitionManager.Instance != null)
        {
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            
            Debug.Log($"Restarting scene by index: {currentSceneIndex}");
            
            SceneTransitionManager.Instance.LoadSceneWithTransition(currentSceneIndex);
        }
        else
        {
                        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}