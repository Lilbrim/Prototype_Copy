using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResultsManager : MonoBehaviour
{
    public static ResultsManager Instance;

    public GameObject resultsCanvas;
    public CanvasGroup resultsCanvasGroup;
    public TextMeshProUGUI totalScoreText;
    public TextMeshProUGUI accuracyText;

    [Header("Display")]
    public TextMeshProUGUI boxesStatsText;
    public bool showDetailedStats = true;

    [Header("LevelSelector")]
    public LevelSelector levelSelector;

    [Header("Buttons")]
    public Button restartButton;
    public Button exitButton;
    public Button returnToSelectorButton;

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

        if (exitButton != null)
            exitButton.onClick.AddListener(ExitToMenu);

        if (returnToSelectorButton != null)
            returnToSelectorButton.onClick.AddListener(ReturnToLevelSelector);
    }

    public void ShowResults(int totalScore, float accuracy, bool isPracticeMode)
    {
        ShowResults(totalScore, accuracy, isPracticeMode, 0, 0);
    }

    public void ShowResults(int totalScore, float accuracy, bool isPracticeMode, int totalBoxes, int totalBoxesTouched)
    {
        if (isPracticeMode)
        {
            totalScoreText.text = "PRACTICE MODE";
            accuracyText.text = "Accuracy\n " + (accuracy * 100).ToString("F2") + "%";
        }
        else
        {
            totalScoreText.text = "Total Score: " + totalScore;
            accuracyText.text = "Accuracy\n " + (accuracy * 100).ToString("F2") + "%";
        }

        if (showDetailedStats && boxesStatsText != null && totalBoxes > 0)
        {
            boxesStatsText.gameObject.SetActive(true);
            boxesStatsText.text = $"Boxes Touched: {totalBoxesTouched}/{totalBoxes}";
        }
        else if (boxesStatsText != null)
        {
            boxesStatsText.gameObject.SetActive(false);
        }

        resultsCanvasGroup.alpha = 1;
        resultsCanvasGroup.interactable = true;
        resultsCanvasGroup.blocksRaycasts = true;
    }

    public void HideResults()
    {
        resultsCanvasGroup.alpha = 0;
        resultsCanvasGroup.interactable = false;
        resultsCanvasGroup.blocksRaycasts = false;
    }

    public void RestartLevel()
    {
        HideResults();
        
        LevelManager.Instance.StartLevel();
    }


    public void ExitToMenu()
    {
        CleanupDummyPartners();
        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
    }
public void ReturnToLevelSelector()
{
    HideResults();
    
    CleanupDummyPartners();

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
    private void CleanupDummyPartners()
    {
        GameObject[] dummyPartners = GameObject.FindGameObjectsWithTag("DummyPartner");
        foreach (GameObject dummy in dummyPartners)
        {
            if (dummy != null)
            {
                Destroy(dummy);
            }
        }
    }
}
