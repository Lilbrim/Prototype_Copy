using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class TutorialResultsManager : MonoBehaviour
{
    public static TutorialResultsManager Instance;

    public GameObject resultsCanvas;
    public CanvasGroup resultsCanvasGroup;
    public TextMeshProUGUI totalScoreText;
    public TextMeshProUGUI accuracyText;

    [Header("Display")]
    public TextMeshProUGUI boxesStatsText;
    public bool showDetailedStats = true;

    [Header("Navigation")]
    public string nextSceneName = "Main";

    [Header("Buttons")]
    public Button restartButton;
    public Button exitButton;

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
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartLevel);

        if (exitButton != null)
            exitButton.onClick.AddListener(ExitToNextScene);
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
        
        if (resultsCanvas != null) 
        {
            resultsCanvas.SetActive(true);
        }

        if (resultsCanvasGroup != null)
        {
            resultsCanvasGroup.alpha = 1;
            resultsCanvasGroup.interactable = true;
            resultsCanvasGroup.blocksRaycasts = true;
        }
    }

    public void HideResults()
    {
        if (resultsCanvas != null) 
        {
            resultsCanvas.SetActive(false);
        }

        if (resultsCanvasGroup != null)
        {
            resultsCanvasGroup.alpha = 0;
            resultsCanvasGroup.interactable = false;
            resultsCanvasGroup.blocksRaycasts = false;
        }
    }

    public void RestartLevel()
    {
        HideResults();
        
        if (TutorialLevelManager.Instance != null)
        {
            TutorialLevelManager.Instance.StartLevel();
        }
        else
        {
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.LoadSceneWithTransition(SceneManager.GetActiveScene().name);
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
    }

    public void ExitToNextScene()
    {
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadSceneWithTransition(nextSceneName);
        }
        else
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private void OnDestroy()
    {
        if (restartButton != null)
            restartButton.onClick.RemoveListener(RestartLevel);

        if (exitButton != null)
            exitButton.onClick.RemoveListener(ExitToNextScene);
    }
}