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
        
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartCurrentScene);
        }
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("R key pressed - restarting scene");
            RestartCurrentScene();
        }
    }
    
    public void RestartCurrentScene()
    {
        Debug.Log("Restarting current scene...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void RestartCurrentSceneByIndex()
    {
        Debug.Log("Restarting current scene by index...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}