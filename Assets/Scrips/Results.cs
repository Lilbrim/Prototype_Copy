using UnityEngine;
using TMPro;

public class ResultsManager : MonoBehaviour
{
    public static ResultsManager Instance;

    public GameObject resultsCanvas;
    public CanvasGroup resultsCanvasGroup;
    public TextMeshProUGUI totalScoreText;
    public TextMeshProUGUI accuracyText;

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