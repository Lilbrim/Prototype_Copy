using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    public string levelName;
    public List<LevelObjective> objectives = new List<LevelObjective>();
    private int currentObjectiveIndex = 0;

    public TextMeshProUGUI objectiveText;
    public TextMeshProUGUI scoreText;
    public Image objectiveImage;
    public Image feedbackImage;

    public Sprite missedSprite;
    public Sprite poorSprite;
    public Sprite goodSprite;
    public Sprite excellentSprite;
    public Sprite perfectSprite;

    [SerializeField] private bool isPracticeMode = false; 

    private int totalScore = 0;

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
        gameObject.SetActive(false);
    }

    private void Start()
    {
        if (objectives.Count > 0)
        {
            StartObjective(objectives[currentObjectiveIndex]);
        }

        UpdateScoreDisplay();
    }

    public void StartObjective(LevelObjective objective)
    {
        objectiveText.text = objective.instruction;
        objectiveImage.sprite = objective.instructionImage;
        StanceManager.Instance.EnterStance(objective.requiredStance);
    }

    public void EndObjective()
    {
        int score = CalculateScore();
        totalScore += score;
        DisplayFeedback(score);

        if (isPracticeMode)
        {
            if (score == 3 || score == 4)
            {
                currentObjectiveIndex++;
            }
        }
        else
        {
            currentObjectiveIndex++;
        }

        if (currentObjectiveIndex < objectives.Count)
        {
            StartObjective(objectives[currentObjectiveIndex]);
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
        int completedBoxes = StanceManager.Instance.sequenceCounter;

        float percentage = (float)completedBoxes / totalBoxes;

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
        UpdateScoreDisplay();
    }

    private void UpdateScoreDisplay()
    {
        if (isPracticeMode)
        {
            scoreText.text = "PRACTICE MODE";
        }
        else
        {
            scoreText.text = "Total Score: " + totalScore;
        }
    }
}

[System.Serializable]
public class LevelObjective
{
    public string instruction;
    public Sprite instructionImage;
    public string requiredStance;
    public float timeLimit;
}