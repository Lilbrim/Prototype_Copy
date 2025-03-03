using UnityEngine;
using TMPro;

public class ResultsManager : MonoBehaviour
{
    public static ResultsManager Instance;

    public GameObject resultsCanvas;
    public CanvasGroup resultsCanvasGroup;
    public TextMeshProUGUI totalScoreText;
    public TextMeshProUGUI accuracyText;
    
    [Header("Additional Stats Display")]
    public TextMeshProUGUI boxesStatsText;
    public bool showDetailedStats = true;

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
        
        // Show detailed stats if enabled and if we have valid data
        if (showDetailedStats && boxesStatsText != null && totalBoxes > 0)
        {
            boxesStatsText.gameObject.SetActive(true);
            boxesStatsText.text = string.Format("Boxes Touched: {0}/{1}", totalBoxesTouched, totalBoxes);
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
        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
    }
}