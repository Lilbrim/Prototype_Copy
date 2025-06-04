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
        Debug.Log($" initialized for level: {levelId}");
    }

    public void ShowSparResults(int playerScore, int opponentScore, int totalRounds)
    {
        resultsPanel.SetActive(true);
        
        if (winnerText != null)
        {
            if (playerScore > opponentScore)
            {
                winnerText.text = "Win";
            }
            else if (opponentScore > playerScore)
            {
                winnerText.text = "Lose";
            }
            else
            {
                winnerText.text = "Tie";
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
                scoreSaver.OnSparCompleted(playerScore, opponentScore);
            }
            else if (levelSelector != null)
            {
                levelSelector.SaveLevelScore(currentLevelId, playerScore);
            }
            else
            {
                Debug.LogWarning("Cannot Save Score");
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
        
        IntroLevel introLevel = FindObjectOfType<IntroLevel>();
        SparManager sparManager = FindObjectOfType<SparManager>();
        
        if (introLevel != null)
        {
            if (introLevel.includeSparringPartner)
            {
                GameObject sparringPartner = GameObject.FindGameObjectWithTag("SparringPartner");
                if (sparringPartner != null)
                {
                    Destroy(sparringPartner);
                }
            }
            
            introLevel.ActivateIntro();
        }
        else if (sparManager != null)
        {
            sparManager.Restart();
        }
    }
    
    public void ReturnToLevelSelector()
    {
        HideResults();
        
        if (levelSelector != null)
        {
            levelSelector.levelSelectionPanel.SetActive(true);
        }
        else
        {
            levelSelector = FindObjectOfType<LevelSelector>();
            if (levelSelector != null)
            {
                levelSelector.levelSelectionPanel.SetActive(true);
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