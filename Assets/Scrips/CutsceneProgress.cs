using UnityEngine;
using UnityEngine.Video;

public class GameProgressManager : MonoBehaviour
{
    [Header("New Game Settings")]
    [SerializeField] private bool isNewGameScene = false;
    [SerializeField] private VideoClip newGameCutscene;
    [SerializeField] private CutsceneManager cutsceneManager;
    [SerializeField] private float delayBeforeCutscene = 1f;
    [SerializeField] private bool loadNextSceneAfterCutscene = false;
    [SerializeField] private string nextSceneName = "Tutorial";
    
    private const string NEW_GAME_KEY = "newgame";
    private const string SCENE_VISITED_PREFIX = "visited_";
    
    private void Start()
    {
        bool isNewGame = !HasStartedGame();
        
        if (isNewGame && isNewGameScene)
        {
            Invoke(nameof(PlayNewGameCutscene), delayBeforeCutscene);
        }
        
        if (isNewGameScene)
        {
            MarkSceneVisited();
        }
    }
    
    private void PlayNewGameCutscene()
    {
        if (cutsceneManager != null)
        {
            cutsceneManager.OnCutsceneEnd += OnCutsceneFinished;
            cutsceneManager.OnCutsceneSkip += OnCutsceneFinished;
            
            if (newGameCutscene != null)
                cutsceneManager.PlayCutscene(newGameCutscene);
            else
                cutsceneManager.PlayCutscene();
        }
        else
        {
            Debug.LogWarning("CutsceneManager not found! Cannot play new game cutscene.");
            MarkGameStarted();
        }
    }
    
    private void OnCutsceneFinished()
    {
        if (cutsceneManager != null)
        {
            cutsceneManager.OnCutsceneEnd -= OnCutsceneFinished;
            cutsceneManager.OnCutsceneSkip -= OnCutsceneFinished;
        }
        
        MarkGameStarted();
        
        Debug.Log("New game cutscene finished - game progress saved");
        
        if (loadNextSceneAfterCutscene && !string.IsNullOrEmpty(nextSceneName))
        {
            LoadNextScene();
        }
    }
    
    private void LoadNextScene()
    {
        SceneTransitionManager transitionManager = FindObjectOfType<SceneTransitionManager>();
        if (transitionManager != null)
        {
            transitionManager.LoadSceneWithTransition(nextSceneName);
        }
        else
        {
            Debug.LogWarning("SceneTransitionManager not found, loading scene directly");
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
        }
    }
    
    private bool HasStartedGame()
    {
        return PlayerPrefs.GetInt(NEW_GAME_KEY, 0) == 1;
    }
    
    private void MarkGameStarted()
    {
        PlayerPrefs.SetInt(NEW_GAME_KEY, 1);
        PlayerPrefs.Save();
        Debug.Log("Game progress saved - player will see 'Continue' option next time");
    }
    
    private void MarkSceneVisited()
    {
        string sceneKey = SCENE_VISITED_PREFIX + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        PlayerPrefs.SetInt(sceneKey, 1);
        PlayerPrefs.Save();
    }
    
    private bool HasVisitedScene(string sceneName)
    {
        string sceneKey = SCENE_VISITED_PREFIX + sceneName;
        return PlayerPrefs.GetInt(sceneKey, 0) == 1;
    }
    

    /// Call this method to trigger a cutscene for story events
    public void PlayStoryCutscene(VideoClip cutsceneVideo)
    {
        if (cutsceneManager != null)
        {
            cutsceneManager.PlayCutscene(cutsceneVideo);
        }
        else
        {
            Debug.LogWarning("CutsceneManager not found! Cannot play story cutscene.");
        }
    }
    

    /// Call this method when the player reaches a save point
    public void SaveGameProgress()
    {
        UIManager.SaveGame();
        Debug.Log("Game progress saved at checkpoint");
    }
    
    /// Reset all game progress (for testing or new game+)
    public void ResetGameProgress()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("All game progress reset");
    }
}