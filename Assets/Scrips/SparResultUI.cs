using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SparResultsManager : MonoBehaviour
{
    public static SparResultsManager Instance;

    [Header("UI Elements")]
    public GameObject resultsPanel;
    public TextMeshProUGUI winnerText;
    public TextMeshProUGUI finalScoreText;

    [Header("Buttons")]
    public Button restartButton;
    public Button returnToLevelSelectButton;
    public Button exitToMenuButton;
    
    [Header("Level Management")]
    public LevelSelector levelSelector;
    private string currentLevelId;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        HideResults();
    }

    private void Start()
    {
        if (levelSelector == null)
        {
            levelSelector = FindObjectOfType<LevelSelector>();
        }
        
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartLevel);
            
        if (returnToLevelSelectButton != null)
            returnToLevelSelectButton.onClick.AddListener(ReturnToLevelSelector);
            
        if (exitToMenuButton != null)
            exitToMenuButton.onClick.AddListener(ExitToMenu);
    }

    public void InitializeForLevel(string levelId)
    {
        currentLevelId = levelId;
        Debug.Log($"SparResultsManager initialized for level: {levelId}");
    }

    public void ShowSparResults(int playerScore, int opponentScore, int totalRounds)
    {
        resultsPanel.SetActive(true);
        
        bool playerWon = false;
        
        if (winnerText != null)
        {
            if (playerScore > opponentScore)
            {
                winnerText.text = "Win";
                playerWon = true;
            }
            else if (opponentScore > playerScore)
            {
                winnerText.text = "Lose";
                playerWon = false;
            }
            else
            {
                winnerText.text = "Tie";
                playerWon = false; 
            }
        }

        if (finalScoreText != null)
        {
            finalScoreText.text = $"Final Score:\n Player {playerScore} - {opponentScore} Opponent";
        }
        
        
        if (!string.IsNullOrEmpty(currentLevelId))
        {
            SaveSparScore scoreSaver = FindObjectOfType<SaveSparScore>();
            if (scoreSaver != null)
            {
                
                int passScore = playerWon ? 1 : 0;
                scoreSaver.OnSparCompleted(passScore, 0); 
            }
            else if (levelSelector != null)
            {
                
                int levelPassScore = playerWon ? 1 : 0;
                levelSelector.SaveLevelScore(currentLevelId, levelPassScore);
                
                Debug.Log($"Spar level {currentLevelId} saved with result: {(playerWon ? "WIN (score: 1)" : "LOSS (score: 0)")}");
            }
            else
            {
                Debug.LogWarning("Cannot Save Spar Score - No SaveSparScore or LevelSelector found");
            }
        }
    }

    public void HideResults()
    {
        if (resultsPanel != null)
        {
            resultsPanel.SetActive(false);
        }
    }

    public void RestartLevel()
    {
        HideResults();
        
        Time.timeScale = 1f;
        
        IntroLevel introLevel = FindObjectOfType<IntroLevel>();
        SparManager sparManager = FindObjectOfType<SparManager>();
        
        GameObject[] sparringPartners = GameObject.FindGameObjectsWithTag("SparringPartner");
        foreach (GameObject partner in sparringPartners)
        {
            if (partner != null)
            {
                Destroy(partner);
            }
        }
       
        if (sparManager != null)
        {
            sparManager.gameObject.SetActive(false);
            
            if (StanceManager.Instance != null)
            {
                StanceManager.Instance.useSparManager = false;
            }
        }
        
        if (StanceManager.Instance != null)
        {
            StanceManager.Instance.EnterStance("Default");
            StanceManager.Instance.ClearAllStances();
        }
        
        if (introLevel != null)
        {
            
            introLevel.gameObject.SetActive(true);
            
            
            introLevel.enabled = true;
            
            introLevel.ActivateIntro();
        }
        else if (sparManager != null)
        {
            
            sparManager.gameObject.SetActive(true);
            sparManager.Restart();
        }
        else
        {
            Debug.LogError("No IntroLevel or SparManager found for restart!");
        }
    }
    
    public void ReturnToLevelSelector()
    {
        HideResults();
        
        if (levelSelector != null)
        {
            
            if (levelSelector.isInStoryMode)
            {
                levelSelector.storyUIPanel.SetActive(true);
            }
            else
            {
                levelSelector.levelSelectionPanel.SetActive(true);
            }
        }
        else
        {
            levelSelector = FindObjectOfType<LevelSelector>();
            if (levelSelector != null)
            {
                
                if (levelSelector.isInStoryMode)
                {
                    levelSelector.storyUIPanel.SetActive(true);
                }
                else
                {
                    levelSelector.levelSelectionPanel.SetActive(true);
                }
            }
            else
            {
                ExitToMenu();
            }
        }
    }
        
    public void ExitToMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
    }
    
    private void OnDestroy()
    {
        if (restartButton != null)
            restartButton.onClick.RemoveListener(RestartLevel);
            
        if (returnToLevelSelectButton != null)
            returnToLevelSelectButton.onClick.RemoveListener(ReturnToLevelSelector);
            
        if (exitToMenuButton != null)
            exitToMenuButton.onClick.RemoveListener(ExitToMenu);
    }
}