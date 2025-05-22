using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.InputSystem;

public class TutorialLevelManager : MonoBehaviour, ILevelManager
{
    public static TutorialLevelManager Instance;

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

    [Header("Training Mode")]
    [SerializeField] private bool trainingMode = false;
    public GameObject trainingModePanel;
    public TextMeshProUGUI accuracyText;
    public TextMeshProUGUI trainingModeInstructionText;
    public InputActionAsset inputActions;
    private InputAction acceptAction;
    private InputAction backAction;
    
    [Header("Tutorial References")]
    public string nextSceneName = "Main"; 
    
    private int totalScore = 0;
    private bool isWaitingForStanceEntry = false;
    private string currentRequiredStance = "";
    private bool isWaitingForTrainingInput = false;
    private float currentObjectiveAccuracy = 0f;
    
    private Dictionary<int, float> objectiveAccuracies = new Dictionary<int, float>();
    private AttackSequence currentSequence = null; 

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
        
        if (inputActions != null)
        {
            acceptAction = inputActions.FindAction("Accept");
            backAction = inputActions.FindAction("Back");
            
            if (acceptAction == null)
            {
                Debug.LogError("Continue action not found in Input Action Asset");
            }
            
            if (backAction == null)
            {
                Debug.LogError("Retry action not found in Input Action Asset");
            }
        }
        else
        {
            Debug.LogError("Input Action Asset not assigned");
        }
    }

    private void OnEnable()
    {
        if (acceptAction != null)
        {
            acceptAction.performed += OnacceptAction;
            acceptAction.Enable();
        }
        
        if (backAction != null)
        {
            backAction.performed += OnbackAction;
            backAction.Enable();
        }
    }

    private void OnDisable()
    {
        if (acceptAction != null)
        {
            acceptAction.performed -= OnacceptAction;
            acceptAction.Disable();
        }
        
        if (backAction != null)
        {
            backAction.performed -= OnbackAction;
            backAction.Disable();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            if (isWaitingForTrainingInput)
            {
                ContinueToNextObjective();
            }
            else
            {
                SkipObjective();
            }
        }
        
        if (trainingMode && Input.GetKeyDown(KeyCode.B) && isWaitingForTrainingInput)
        {
            RetryCurrentObjective();
        }
        
        if (Input.GetKeyDown(KeyCode.Y))
        {
            if (isWaitingForTrainingInput)
            {
                ContinueToNextObjective();
            }
            else
            {
                SkipObjective();
            }
        }
        
        if (trainingMode && Input.GetKeyDown(KeyCode.N) && isWaitingForTrainingInput)
        {
            RetryCurrentObjective();
        }
    }

    private void OnacceptAction(InputAction.CallbackContext context)
    {
        if (isWaitingForTrainingInput)
        {
            ContinueToNextObjective();
        }
    }

    private void OnbackAction(InputAction.CallbackContext context)
    {
        if (trainingMode && isWaitingForTrainingInput)
        {
            RetryCurrentObjective();
        }
    }

    private void SkipObjective()
    {
        if (currentObjectiveIndex < objectives.Count && !isWaitingForTrainingInput)
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
        objectiveAccuracies.Clear();
        isWaitingForTrainingInput = false;
        currentSequence = null;

        if (AccuracyTracker.Instance != null)
        {
            AccuracyTracker.Instance.ResetTracking();
        }

        if (StanceManager.Instance != null)
        {
            StanceManager.Instance.SetGameActive(true);
            StanceManager.Instance.enabled = true;
        }

        DisableAllSequenceBoxes();

        if (objectives != null && objectives.Count > 0)
        {
            StartStanceEntry(objectives[currentObjectiveIndex]);
        }
        else
        {
            Debug.LogError("No objectives found for level: " + levelName);
        }
        
        UpdateScoreDisplay();
        
        if (trainingModePanel != null)
        {
            trainingModePanel.SetActive(false);
        }
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
        
        if (trainingModePanel != null)
        {
            trainingModePanel.SetActive(false);
        }
    }
    
    private void StartStanceEntry(LevelObjective objective)
    {
        if (objective == null)
        {
            Debug.LogError("Attempted to start null objective");
            return;
        }

        DisablePreviousSequenceBoxes();

        isWaitingForStanceEntry = true;
        isWaitingForTrainingInput = false;
        currentRequiredStance = objective.requiredStance;
        objectiveText.text = objective.stanceEntryInstruction;
        stanceEntryImage.sprite = objective.stanceEntryImage;
        objectiveImage.gameObject.SetActive(false);
        stanceEntryImage.gameObject.SetActive(true);
        
        if (trainingModePanel != null)
        {
            trainingModePanel.SetActive(false);
        }
        
        StanceManager.Instance.EnterStance(objective.requiredStance, trainingMode);
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
                
                if (trainingMode)
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

        if (StanceManager.Instance != null)
        {
            currentSequence = StanceManager.Instance.currentAttackSequence;
        }

        StanceManager.Instance.EnterStance(objective.requiredStance, trainingMode);
    }

    public void EndObjective()
    {
        int score = 0;
        if (!trainingMode)
        {
            score = CalculateScore();
            totalScore += score;
        }
        
        currentObjectiveAccuracy = CalculateCurrentObjectiveAccuracy();
        objectiveAccuracies[currentObjectiveIndex] = currentObjectiveAccuracy;
        
        if (trainingMode)
        {
            ShowTrainingModePanel();
        }
        else
        {
            currentObjectiveIndex++;
            if (currentObjectiveIndex < objectives.Count)
            {
                StartStanceEntry(objectives[currentObjectiveIndex]);
            }
            else
            {
                EndLevel();
            }
        }

        UpdateScoreDisplay();
    }

    private void ShowTrainingModePanel()
    {
        isWaitingForTrainingInput = true;
        
        if (trainingModePanel != null)
        {
            trainingModePanel.SetActive(true);
            
            if (accuracyText != null)
            {
                accuracyText.text = $"Accuracy: {(currentObjectiveAccuracy * 100):F1}%";
            }
            
            if (trainingModeInstructionText != null)
            {
                trainingModeInstructionText.text = "Press B to Retry\nPress Y to Continue";
            }
        }
        else
        {
            Debug.LogError("Training Mode Panel not assigned!");
            ContinueToNextObjective();
        }
    }
    

    private void ContinueToNextObjective()
    {
        isWaitingForTrainingInput = false;
        
        if (trainingModePanel != null)
        {
            trainingModePanel.SetActive(false);
        }
        
        // Enhanced cleanup - disable all boxes first
        DisableAllSequenceBoxes();
        
        // Clear stance manager state
        if (StanceManager.Instance != null)
        {
            StanceManager.Instance.ClearAllStances();
            StanceManager.Instance.totalBoxesTouched = 0;
            // Reset current sequence reference
            StanceManager.Instance.currentAttackSequence = null;
        }
        
        // Reset current sequence reference
        currentSequence = null;
        
        currentObjectiveIndex++;
        
        if (currentObjectiveIndex < objectives.Count)
        {
            // Add a small delay to ensure cleanup is complete
            StartCoroutine(StartNextObjectiveAfterCleanup());
        }
        else
        {
            EndLevel();
        }
    }

    // New coroutine to ensure proper cleanup before starting next objective
    private IEnumerator StartNextObjectiveAfterCleanup()
    {
        yield return new WaitForEndOfFrame(); // Wait one frame for cleanup to complete
        
        if (currentObjectiveIndex >= 0 && currentObjectiveIndex < objectives.Count)
        {
            StartStanceEntry(objectives[currentObjectiveIndex]);
        }
        else
        {
            EndLevel();
        }
    }

    // Enhanced DisableAllSequenceBoxes method
    private void DisableAllSequenceBoxes()
    {
        if (StanceManager.Instance != null)
        {
            // Disable current sequence boxes first
            if (currentSequence != null)
            {
                DisableSequenceBoxes(currentSequence);
            }
            
            if (StanceManager.Instance.currentAttackSequence != null)
            {
                DisableSequenceBoxes(StanceManager.Instance.currentAttackSequence);
            }
            
            // Disable all boxes from all styles and sequences
            foreach (var style in StanceManager.Instance.arnisStyles)
            {
                // Disable stance boxes
                if (style.stanceBoxes != null)
                {
                    foreach (var box in style.stanceBoxes)
                    {
                        if (box != null)
                            box.SetActive(false);
                    }
                }
                
                // Disable all sequence boxes
                if (style.sequences != null)
                {
                    foreach (var sequence in style.sequences)
                    {
                        DisableSequenceBoxes(sequence);
                    }
                }
            }
            
            // Disable default boxes
            if (StanceManager.Instance.defaultBoxes != null)
            {
                foreach (var box in StanceManager.Instance.defaultBoxes)
                {
                    if (box != null)
                        box.SetActive(false);
                }
            }
            
            // Reset all detectors
            StanceDetector[] allDetectors = FindObjectsOfType<StanceDetector>();
            foreach (var detector in allDetectors)
            {
                detector.ForceResetTriggerState();
                detector.IsCompleted = false;
            }
            
            StanceManager.Instance.totalBoxesTouched = 0;
        }
    }

    // Also update your RetryCurrentObjective method:
    private void RetryCurrentObjective()
    {
        isWaitingForTrainingInput = false;
        
        if (trainingModePanel != null)
        {
            trainingModePanel.SetActive(false);
        }
        
        if (StanceManager.Instance != null)
        {
            StanceManager.Instance.ClearAllStances();
            StanceManager.Instance.totalBoxesTouched = 0;
            StanceManager.Instance.currentAttackSequence = null;
        }
        
        // Complete cleanup before restarting
        DisableAllSequenceBoxes();
        currentSequence = null;
        
        if (currentObjectiveIndex >= 0 && currentObjectiveIndex < objectives.Count)
        {
            // Add small delay for cleanup
            StartCoroutine(RestartObjectiveAfterCleanup());
        }
    }

    private IEnumerator RestartObjectiveAfterCleanup()
    {
        yield return new WaitForEndOfFrame();
        
        if (currentObjectiveIndex >= 0 && currentObjectiveIndex < objectives.Count)
        {
            StartStanceEntry(objectives[currentObjectiveIndex]);
        }
    }

    private void DisablePreviousSequenceBoxes()
    {
        if (StanceManager.Instance != null)
        {
            if (currentSequence != null)
            {
                DisableSequenceBoxes(currentSequence);
            }
            
            if (StanceManager.Instance.currentAttackSequence != null)
            {
                DisableSequenceBoxes(StanceManager.Instance.currentAttackSequence);
            }
            
            StanceDetector[] allDetectors = FindObjectsOfType<StanceDetector>();
            foreach (var detector in allDetectors)
            {
                detector.ForceResetTriggerState();
                detector.IsCompleted = false;
            }
            
            StanceManager.Instance.totalBoxesTouched = 0;
        }
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

    private float CalculateCurrentObjectiveAccuracy()
    {
        int totalBoxes = StanceManager.Instance.currentAttackSequence != null ? 
            StanceManager.Instance.currentAttackSequence.sequenceBoxes.Length : 0;
        int touchedBoxes = StanceManager.Instance.totalBoxesTouched;

        return totalBoxes > 0 ? (float)touchedBoxes / totalBoxes : 0;
    }

    private void DisplayFeedback(int score)
    {
        if (trainingMode)
        {
            float accuracy = currentObjectiveAccuracy;
            
            if (accuracy == 0)
                feedbackImage.sprite = missedSprite;
            else if (accuracy <= 0.5f)
                feedbackImage.sprite = poorSprite;
            else if (accuracy <= 0.8f)
                feedbackImage.sprite = goodSprite;
            else if (accuracy < 1f)
                feedbackImage.sprite = excellentSprite;
            else
                feedbackImage.sprite = perfectSprite;
        }
        else
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
        }

        feedbackImage.gameObject.SetActive(true);
        
        if (!trainingMode)
        {
            StartCoroutine(HideFeedbackAfterDelay(2f));
        }
    }

    private IEnumerator HideFeedbackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        feedbackImage.gameObject.SetActive(false);
    }

    private void EndLevel()
    {
        DisableLevelUI();
        DisableAllBoxes();
        
        if (StanceManager.Instance != null)
        {
            StanceManager.Instance.SetGameActive(false);
            StanceManager.Instance.enabled = false;
        }

        float accuracy;
        if (trainingMode) {
            accuracy = CalculateTrainingModeFinalAccuracy();
        } else if (AccuracyTracker.Instance != null) {
            accuracy = AccuracyTracker.Instance.CalculateAccuracy();
        } else {
            accuracy = CalculateAccuracy();
        }

        if (TutorialResultsManager.Instance != null)
        {
            if (AccuracyTracker.Instance != null && !trainingMode)
            {
                int totalBoxes = AccuracyTracker.Instance.GetTotalBoxes();
                int totalBoxesTouched = AccuracyTracker.Instance.GetTotalBoxesTouched();

                TutorialResultsManager.Instance.ShowResults(totalScore, accuracy, trainingMode, totalBoxes, totalBoxesTouched);
            }
            else
            {
                TutorialResultsManager.Instance.ShowResults(totalScore, accuracy, trainingMode);
            }
        }
        else
        {
            Debug.LogError("ResultsManager not found!");
        }
    }

    private float CalculateTrainingModeFinalAccuracy()
    {
        float totalAccuracy = 0f;
        int validObjectives = 0;
        
        foreach (var entry in objectiveAccuracies)
        {
            totalAccuracy += entry.Value;
            validObjectives++;
        }
        
        return validObjectives > 0 ? totalAccuracy / validObjectives : 0f;
    }

    private void DisableAllBoxes()
    {
        StanceManager sm = StanceManager.Instance;
        
        if (sm == null)
            return;
            
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
            if (trainingMode)
            {
                scoreText.text = "TRAINING MODE";
            }
            else
            {
                scoreText.text = "Total Score\n " + totalScore;
            }
        }
    }

    public bool IsTrainingMode()
    {
        return trainingMode;
    }

    public string GetCurrentRequiredStance()
    {
        return currentRequiredStance;
    }
}