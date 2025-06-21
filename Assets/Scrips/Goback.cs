using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Goback : MonoBehaviour
{
    public Button restartButton;

    private void Start()
    {
        if (restartButton == null)
        {
            restartButton = GetComponent<Button>();
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