using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [Header("Level Settings")]
    public string levelName;
    public List<LevelObjective> objectives = new List<LevelObjective>();
    private int currentObjectiveIndex = 0;

    [Header("UI References")]
    public TextMeshProUGUI objectiveText;
    public TextMeshProUGUI scoreText;
    public Image objectiveImage;
    public Image feedbackImage;
    public Image stanceEntryImage;

    [Header("Feedback Sprites")]
    public Sprite missedSprite;
    public Sprite poorSprite;
    public Sprite goodSprite;
    public Sprite excellentSprite;
    public Sprite perfectSprite;

    [SerializeField] private bool isPracticeMode = false;
    private int totalScore = 0;
    private bool isWaitingForStanceEntry = false;

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
    }

    public void StartLevel()
    {
        gameObject.SetActive(true);
        currentObjectiveIndex = 0;
        totalScore = 0;
        
        if (objectives.Count > 0)
        {
            StartStanceEntry(objectives[currentObjectiveIndex]);
        }
        
        UpdateScoreDisplay();
    }

    private void StartStanceEntry(LevelObjective objective)
    {
        isWaitingForStanceEntry = true;
        objectiveText.text = objective.stanceEntryInstruction;
        stanceEntryImage.sprite = objective.stanceEntryImage;
        objectiveImage.gameObject.SetActive(false);
        stanceEntryImage.gameObject.SetActive(true);
        StanceManager.Instance.EnterStance(objective.requiredStance);
    }

    public void OnStanceEntered()
    {
        if (isWaitingForStanceEntry)
        {
            isWaitingForStanceEntry = false;
            StartObjective(objectives[currentObjectiveIndex]);
        }
    }

    public void StartObjective(LevelObjective objective)
    {
        objectiveText.text = objective.instruction;
        stanceEntryImage.gameObject.SetActive(false);
        objectiveImage.gameObject.SetActive(true);
        objectiveImage.sprite = objective.instructionImage;
    }

    public void EndObjective()
    {
        int score = CalculateScore();
        totalScore += score;
        DisplayFeedback(score);

        // Always progress to the next objective regardless of score or practice mode
        currentObjectiveIndex++;

        if (currentObjectiveIndex < objectives.Count)
        {
            StartStanceEntry(objectives[currentObjectiveIndex]);
        }
        else
        {
            EndLevel();
        }

        UpdateScoreDisplay();
    }

    private int CalculateScore()
    {
        int totalBoxes = StanceManager.Instance.currentAttackSequence.sequenceBoxes.Length;
        int touchedBoxes = StanceManager.Instance.totalBoxesTouched;

        float percentage = (float)touchedBoxes / totalBoxes;

        if (percentage == 0)
        {
            return 0;
        }
        else if (percentage <= 0.5f)
        {
            return 1;
        }
        else if (percentage <= 0.8f)
        {
            return 2;
        }
        else if (percentage < 1f)
        {
            return 3;
        }
        else
        {
            return 4;
        }
    }

    private void DisplayFeedback(int score)
    {
        switch (score)
        {
            case 0:
                feedbackImage.sprite = missedSprite;
                break;
            case 1:
                feedbackImage.sprite = poorSprite;
                break;
            case 2:
                feedbackImage.sprite = goodSprite;
                break;
            case 3:
                feedbackImage.sprite = excellentSprite;
                break;
            case 4:
                feedbackImage.sprite = perfectSprite;
                break;
        }

        feedbackImage.gameObject.SetActive(true);
        StartCoroutine(HideFeedbackAfterDelay(2f));
    }

    private IEnumerator HideFeedbackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        feedbackImage.gameObject.SetActive(false);
    }

    private void EndLevel()
    {
        float accuracy = CalculateAccuracy();
        ResultsManager.Instance.ShowResults(totalScore, accuracy, isPracticeMode);
    }

    private float CalculateAccuracy()
    {
        int totalBoxes = StanceManager.Instance.currentAttackSequence.sequenceBoxes.Length;
        int touchedBoxes = StanceManager.Instance.totalBoxesTouched;

        return (float)touchedBoxes / totalBoxes;
    }

    private void UpdateScoreDisplay()
    {
        if (isPracticeMode)
        {
            scoreText.text = "PRACTICE MODE";
        }
        else
        {
            scoreText.text = "Total Score\n " + totalScore;
        }
    }
}

[System.Serializable]
public class LevelObjective
{
    [Header("Stance Entry")]
    public string stanceEntryInstruction;
    public Sprite stanceEntryImage;

    [Header("Sequence")]
    public string instruction;
    public Sprite instructionImage;
    public string requiredStance;
    public float timeLimit;
}