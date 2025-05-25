using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public GameObject start;
    public GameObject menu;
    public GameObject load;
    public GameObject confirmationPanel;  
    
    public TextMeshProUGUI confirmationText;
    
    public string newGameSceneName = "Tutorial";
    public string loadGameSceneName = "GameLevel";
    
    private SceneTransitionManager transitionManager;
    private const string NEW_GAME_KEY = "newgame";
    
    void Start()
    {
        start.SetActive(true);
        menu.SetActive(false);
        load.SetActive(false);
        
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
        
        transitionManager = SceneTransitionManager.Instance;
        
        CheckForSaveData();
    }
    
    private void CheckForSaveData()
    {
        if (HasSavedGame())
        {
            if (transform.Find("Start/StartButton/Text") != null)
            {
                var startText = transform.Find("Start/StartButton/Text").GetComponent<Text>();
                if (startText != null)
                    startText.text = "Continue";
            }
            
            if (transform.Find("Start/StartButton/ButtonText") != null)
            {
                var buttonText = transform.Find("Start/StartButton/ButtonText").GetComponent<TextMeshProUGUI>();
                if (buttonText != null)
                    buttonText.text = "Continue";
            }
        }
    }
    
    private bool HasSavedGame()
    {
        return PlayerPrefs.GetInt(NEW_GAME_KEY, 0) == 1;
    }
    
    public void OnStartButtonPressed()
    {
        start.SetActive(false);
        menu.SetActive(true);
    }
    
    public void OnNewGamePressed()
    {
        if (HasSavedGame())
        {
            if (confirmationPanel != null)
            {
                confirmationPanel.SetActive(true);
                if (confirmationText != null)
                    confirmationText.text = "This will delete all saved data";
                return;
            }
        }
        
        StartNewGame();
    }
    
    private void StartNewGame()
    {
        LoadScene(newGameSceneName);
    }
    
    public void OnConfirmNewGame()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
        
        StartNewGame();
    }
    
    public void OnCancelNewGame()
    {
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
    }
    
    public void OnLoadButtonPressed()
    {
        if (HasSavedGame())
        {
            menu.SetActive(false);
            load.SetActive(true);
        }
        else
        {
            Debug.LogWarning("No save data found");
        }
    }
    
    public void OnLoadGamePressed()
    {
        LoadScene(loadGameSceneName);
    }
    
    public void OnExitButtonPressed()
    {
        if (load.activeSelf)
        {
            load.SetActive(false);
            menu.SetActive(true);
        }
        else if (menu.activeSelf)
        {
            menu.SetActive(false);
            start.SetActive(true);
        }
        else if (confirmationPanel != null && confirmationPanel.activeSelf)
        {
            confirmationPanel.SetActive(false);
        }
    }
    
    public void OnTutorialButtonPressed()
    {
        LoadScene("Tutorial");
    }
    
    private void LoadScene(string sceneName)
    {
        if (transitionManager != null)
        {
            transitionManager.LoadSceneWithTransition(sceneName);
        }
        else
        {
            Debug.LogWarning("SceneTransitionManager not found, loading scene directly");
            SceneManager.LoadScene(sceneName);
        }
    }
    
    public void OnQuitButtonPressed()
    {
        Application.Quit();
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
    
    public static void SaveGame()
    {
        PlayerPrefs.SetInt(NEW_GAME_KEY, 1);
        PlayerPrefs.Save();
        Debug.Log("Game saved");
    }
}