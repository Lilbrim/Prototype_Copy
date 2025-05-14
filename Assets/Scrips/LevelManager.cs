using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelManager : MonoBehaviour ,ILevelManager
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
    
    [Header("UI Container")]
    public GameObject levelUI; 

    [Header("Feedback Sprites")]
    public Sprite incorrectStanceSprite;
    public Sprite missedSprite;
    public Sprite poorSprite;
    public Sprite goodSprite;
    public Sprite excellentSprite;
    public Sprite perfectSprite;

    [SerializeField] private bool isPracticeMode = false;
    private int totalScore = 0;
    private bool isWaitingForStanceEntry = false;
    private string currentRequiredStance = "";

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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            SkipObjective();
        }
    }

    private void SkipObjective()
    {
        if (currentObjectiveIndex < objectives.Count)
        {
            Debug.Log("Skipping objective: " + currentObjectiveIndex);
            EndObjective();
        }
    }


    public void StartLevel()
    {
        gameObject.SetActive(true);
        
        EnableLevelUI();
        
        currentObjectiveIndex = 0;
        totalScore = 0;

        if (AccuracyTracker.Instance != null)
        {
            AccuracyTracker.Instance.ResetTracking();
        }

        if (StanceManager.Instance != null)
        {
            StanceManager.Instance.SetGameActive(true);
            
            StanceManager.Instance.enabled = true;
        }

        if (objectives != null && objectives.Count > 0)
        {
            StartStanceEntry(objectives[currentObjectiveIndex]);
        }
        else
        {
            Debug.LogError("No objectives found for level: " + levelName);
        }
        
        UpdateScoreDisplay();
    }
    
    private void EnableLevelUI()
    {
        if (levelUI != null)
        {
            levelUI.SetActive(true);
        }
        
        if (objectiveText != null) objectiveText.gameObject.SetActive(true);
        if (scoreText != null) scoreText.gameObject.SetActive(true);
        if (objectiveImage != null) objectiveImage.gameObject.SetActive(true);
        if (stanceEntryImage != null) stanceEntryImage.gameObject.SetActive(true);
        
        if (feedbackImage != null) feedbackImage.gameObject.SetActive(false);
    }
    
    private void DisableLevelUI()
    {
        if (levelUI != null)
        {
            levelUI.SetActive(false);
        }
        else
        {
            if (objectiveText != null) objectiveText.gameObject.SetActive(false);
            if (scoreText != null) scoreText.gameObject.SetActive(false);
            if (objectiveImage != null) objectiveImage.gameObject.SetActive(false);
            if (feedbackImage != null) feedbackImage.gameObject.SetActive(false);
            if (stanceEntryImage != null) stanceEntryImage.gameObject.SetActive(false);
        }
    }
    
    private void StartStanceEntry(LevelObjective objective)
    {
        if (objective == null)
        {
            Debug.LogError("Attempted to start null objective");
            return;
        }

        isWaitingForStanceEntry = true;
        currentRequiredStance = objective.requiredStance;
        objectiveText.text = objective.stanceEntryInstruction;
        stanceEntryImage.sprite = objective.stanceEntryImage;
        objectiveImage.gameObject.SetActive(false);
        stanceEntryImage.gameObject.SetActive(true);
        
        StanceManager.Instance.EnterStance(objective.requiredStance, isPracticeMode);
    }

    public void OnStanceEntered(string enteredStance)
    {
        if (isWaitingForStanceEntry)
        {
            if (enteredStance == currentRequiredStance)
            {
                isWaitingForStanceEntry = false;
                if (currentObjectiveIndex >= 0 && currentObjectiveIndex < objectives.Count)
                {
                    StartObjective(objectives[currentObjectiveIndex]);
                }
                else
                {
                    Debug.LogError("Current objective index out of range: " + currentObjectiveIndex);
                }
            }
            else
            {
                DisplayIncorrectStanceFeedback();
                
                if (isPracticeMode)
                {
                    StartCoroutine(RestartCurrentObjectiveAfterDelay(2f));
                }
                else
                {
                    totalScore += 0;
                    UpdateScoreDisplay();
                    
                    currentObjectiveIndex++;
                    if (currentObjectiveIndex < objectives.Count)
                    {
                        StartCoroutine(StartNextObjectiveAfterDelay(2f));
                    }
                    else
                    {
                        StartCoroutine(EndLevelAfterDelay(2f));
                    }
                }
            }
        }
    }

    private IEnumerator RestartCurrentObjectiveAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        feedbackImage.gameObject.SetActive(false);
        
        if (currentObjectiveIndex >= 0 && currentObjectiveIndex < objectives.Count)
        {
            StartStanceEntry(objectives[currentObjectiveIndex]);
        }
        else
        {
            Debug.LogError("Restart objective failed: Index out of range: " + currentObjectiveIndex);
            if (objectives.Count > 0)
            {
                currentObjectiveIndex = 0;
                StartStanceEntry(objectives[currentObjectiveIndex]);
            }
            else
            {
                EndLevel();
            }
        }
    }

    private IEnumerator StartNextObjectiveAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        feedbackImage.gameObject.SetActive(false);
        
        if (currentObjectiveIndex >= 0 && currentObjectiveIndex < objectives.Count)
        {
            StartStanceEntry(objectives[currentObjectiveIndex]);
        }
        else
        {
            Debug.LogError("Start next objective failed: Index out of range: " + currentObjectiveIndex);
            EndLevel();
        }
    }

    private IEnumerator EndLevelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        EndLevel();
    }

    private void DisplayIncorrectStanceFeedback()
    {
        feedbackImage.sprite = incorrectStanceSprite;
        feedbackImage.gameObject.SetActive(true);
    }

    public void StartObjective(LevelObjective objective)
    {
        if (objective == null)
        {
            Debug.LogError("Attempted to start null objective");
            return;
        }

        objectiveText.text = objective.instruction;
        stanceEntryImage.gameObject.SetActive(false);
        objectiveImage.gameObject.SetActive(true);
        objectiveImage.sprite = objective.instructionImage;

        StanceManager.Instance.EnterStance(objective.requiredStance, isPracticeMode);
    }

    public void EndObjective()
    {
        int score = CalculateScore();
        totalScore += score;
        DisplayFeedback(score);

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
        int totalBoxes = StanceManager.Instance.currentAttackSequence != null ? 
        StanceManager.Instance.currentAttackSequence.sequenceBoxes.Length : 0;
        int touchedBoxes = StanceManager.Instance.totalBoxesTouched;

        float percentage = totalBoxes > 0 ? (float)touchedBoxes / totalBoxes : 0;

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
        DisableLevelUI();
        
        if (StanceManager.Instance != null)
        {
            StanceManager.Instance.SetGameActive(false);
            DisableAllBoxes();
            StanceManager.Instance.enabled = false;
            if (StanceManager.Instance.currentAttackSequence != null)
            {
                foreach (var box in StanceManager.Instance.currentAttackSequence.sequenceBoxes)
                {
                    if (box != null)
                    {
                        box.SetActive(false);

                        StanceDetector detector = box.GetComponent<StanceDetector>();
                        if (detector != null)
                        {
                            detector.IsCompleted = false;
                            detector.ForceResetTriggerState();
                        }
                    }
                }

                if (StanceManager.Instance.currentAttackSequence.endBoxLeft != null)
                    StanceManager.Instance.currentAttackSequence.endBoxLeft.SetActive(false);

                if (StanceManager.Instance.currentAttackSequence.endBoxRight != null)
                    StanceManager.Instance.currentAttackSequence.endBoxRight.SetActive(false);
            }
        }

        float accuracy = AccuracyTracker.Instance != null ? 
        AccuracyTracker.Instance.CalculateAccuracy() : CalculateAccuracy();

        SaveAccuracy saveAccuracy = FindObjectOfType<IntroLevel>().GetComponent<SaveAccuracy>();
        if (saveAccuracy != null)
        {
            saveAccuracy.OnLevelCompleted(accuracy);
        }
        else
        {
            Debug.LogError("SaveAccuracy component not found");
        }

        if (ResultsManager.Instance != null)
        {
            if (AccuracyTracker.Instance != null)
            {
                int totalBoxes = AccuracyTracker.Instance.GetTotalBoxes();
                int totalBoxesTouched = AccuracyTracker.Instance.GetTotalBoxesTouched();

                ResultsManager.Instance.ShowResults(totalScore, accuracy, isPracticeMode, totalBoxes, totalBoxesTouched);
            }
            else
            {
                ResultsManager.Instance.ShowResults(totalScore, accuracy, isPracticeMode);
            }
        }
        else
        {
            Debug.LogError("ResultsManager not found!");
        }
    }


    private void DisableAllBoxes()
    {
        StanceManager sm = StanceManager.Instance;
        
        if (sm.defaultBoxes != null)
        {
            foreach (var box in sm.defaultBoxes)
            {
                if (box != null)
                    box.SetActive(false);
            }
        }
        
        foreach (var style in sm.arnisStyles)
        {
            if (style.stanceBoxes != null)
            {
                foreach (var box in style.stanceBoxes)
                {
                    if (box != null)
                        box.SetActive(false);
                }
            }
            
            if (style.sequences != null)
            {
                foreach (var sequence in style.sequences)
                {
                    DisableSequenceBoxes(sequence);
                }
            }
        }
    }
    
    private void DisableSequenceBoxes(AttackSequence sequence)
    {
        if (sequence == null)
            return;
            
        if (sequence.startBoxLeft != null)
            sequence.startBoxLeft.SetActive(false);
            
        if (sequence.startBoxRight != null)
            sequence.startBoxRight.SetActive(false);
            
        if (sequence.sequenceBoxes != null)
        {
            foreach (var box in sequence.sequenceBoxes)
            {
                if (box != null)
                    box.SetActive(false);
            }
        }
        
        if (sequence.endBoxLeft != null)
            sequence.endBoxLeft.SetActive(false);
            
        if (sequence.endBoxRight != null)
            sequence.endBoxRight.SetActive(false);
    }

    private float CalculateAccuracy()
    {
        if (AccuracyTracker.Instance != null)
        {
            return AccuracyTracker.Instance.CalculateAccuracy();
        }
        
        int totalBoxes = 0;
        int touchedBoxes = 0;

        foreach (var objective in objectives)
        {
            if (StanceManager.Instance != null && StanceManager.Instance.currentAttackSequence != null)
            {
                totalBoxes += StanceManager.Instance.currentAttackSequence.sequenceBoxes.Length;
                touchedBoxes += StanceManager.Instance.totalBoxesTouched;
            }
        }

        return totalBoxes > 0 ? (float)touchedBoxes / totalBoxes : 0;
    }


    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
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

    public bool IsPracticeMode()
    {
        return isPracticeMode;
    }

    public string GetCurrentRequiredStance()
    {
        return currentRequiredStance;
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