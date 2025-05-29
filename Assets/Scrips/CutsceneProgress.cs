using UnityEngine;
using UnityEngine.Video;

public class GameProgressManager : MonoBehaviour
{
    [Header("New Game Settings")]
    [SerializeField] private VideoClip newGameCutscene;
    [SerializeField] private CutsceneManager cutsceneManager;
    [SerializeField] private string nextSceneName = "Tutorial";
    
    private const string NEW_GAME_KEY = "newgame";
    private const string SCENE_VISITED_PREFIX = "visited_";
    
    public static GameProgressManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        MarkSceneVisited();
    }
    
    public void StartNewGameWithCutscene(System.Action onComplete = null)
    {
        if (cutsceneManager != null && newGameCutscene != null)
        {
            cutsceneManager.OnCutsceneEnd += () => OnNewGameCutsceneFinished(onComplete);
            cutsceneManager.OnCutsceneSkip += () => OnNewGameCutsceneFinished(onComplete);
            
            cutsceneManager.PlayCutscene(newGameCutscene);
        }
        else
        {
            MarkGameStarted();
            onComplete?.Invoke();
        }
    }
    
    private void OnNewGameCutsceneFinished(System.Action onComplete)
    {
        if (cutsceneManager != null)
        {
            cutsceneManager.OnCutsceneEnd -= () => OnNewGameCutsceneFinished(onComplete);
            cutsceneManager.OnCutsceneSkip -= () => OnNewGameCutsceneFinished(onComplete);
        }
        
        MarkGameStarted();
        
        onComplete?.Invoke();
    }
    
    private bool HasStartedGame()
    {
        return PlayerPrefs.GetInt(NEW_GAME_KEY, 0) == 1;
    }
    
    private void MarkGameStarted()
    {
        PlayerPrefs.SetInt(NEW_GAME_KEY, 1);
        PlayerPrefs.Save();
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
    
    public void PlayStoryCutscene(VideoClip cutsceneVideo)
    {
        if (cutsceneManager != null)
        {
            cutsceneManager.PlayCutscene(cutsceneVideo);
        }
    }

    public void SaveGameProgress()
    {
        UIManager.SaveGame();
    }
    
    public void ResetGameProgress()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("All game progress reset");
    }
}