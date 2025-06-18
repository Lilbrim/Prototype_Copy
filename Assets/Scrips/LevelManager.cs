using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.InputSystem;
using UnityEngine.Video;

public class LevelManager : MonoBehaviour, ILevelManager
{
    public static LevelManager Instance;

    [Header("Level Settings")]
    public string levelName;
    public List<LevelObjective> objectives = new List<LevelObjective>();
    private int currentObjectiveIndex = 0;

    [Header("Level Settings")]
    
    private int currentRepeatCount = 0;
    private int maxRepeatsForCurrentObjective = 0;
    private List<float> currentObjectiveRepeatAccuracies = new List<float>();
    

    [Header("UI References")]
    public TextMeshProUGUI objectiveText;
    public TextMeshProUGUI repeatCountText;
    public TextMeshProUGUI scoreText;
    public Image objectiveImage;
    public VideoPlayer objectiveVideoPlayer; 
    public Image feedbackImage;
    public Image stanceEntryImage;
    public VideoPlayer videoPlayer;
    public Button toggleVideoButton;
    [Header("Video Toggle")]
    private bool isVideoMode = false;
    public RawImage objectiveVideoRawImage;

    

    
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

    [Header("Stance Guide Settings")]
    public bool enableAutoDetectSequences = false;
    public Button stanceGuideToggleButton; 
    public TextMeshProUGUI stanceGuideButtonText; 
    private StanceGuide stanceGuideReference;

    
    private int totalScore = 0;
    private bool isWaitingForStanceEntry = false;
    private string currentRequiredStance = "";
    private bool isWaitingForTrainingInput = false;
    private float currentObjectiveAccuracy = 0f;
    
    private Dictionary<int, float> objectiveAccuracies = new Dictionary<int, float>();

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

        
        if (objectiveVideoPlayer != null)
        {
            objectiveVideoPlayer.isLooping = true;
            objectiveVideoPlayer.playOnAwake = false;
        }
    }

    private void Start()
    {
        inputActions.Enable();
        ConfigureStanceGuide();
        
        if (toggleVideoButton != null)
        {
            toggleVideoButton.onClick.AddListener(OnToggleVideoButtonPressed);
        }

        
        if (stanceGuideToggleButton != null)
        {
            stanceGuideToggleButton.onClick.AddListener(OnStanceGuideTogglePressed);
            UpdateStanceGuideButtonText(); 
        }
    }

    private void ConfigureStanceGuide()
    {
        stanceGuideReference = FindObjectOfType<StanceGuide>();
        if (stanceGuideReference != null)
        {
            stanceGuideReference.autoDetectSequences = enableAutoDetectSequences;
            
            
            UpdateStanceGuideState();
        }
        else
        {
            Debug.LogWarning("StanceGuide not found in scene");
        }
    }

    private void OnStanceGuideTogglePressed()
    {
        
        enableAutoDetectSequences = !enableAutoDetectSequences;
        
        
        UpdateStanceGuideState();
        
        
        UpdateStanceGuideButtonText();
        
        Debug.Log($"StanceGuide auto-detection {(enableAutoDetectSequences ? "enabled" : "disabled")}");
    }

    private void UpdateStanceGuideState()
    {
        if (stanceGuideReference == null)
        {
            stanceGuideReference = FindObjectOfType<StanceGuide>();
        }
        
        if (stanceGuideReference != null)
        {
            stanceGuideReference.autoDetectSequences = enableAutoDetectSequences;
            
            
            if (!enableAutoDetectSequences)
            {
                stanceGuideReference.StopAllBatons();
                stanceGuideReference.SetHideBatonsWhenNoSequence(true);
                stanceGuideReference.HideAllBatons();
            }
            else
            {
                
                stanceGuideReference.SetHideBatonsWhenNoSequence(true);
                
                
                if (StanceManager.Instance?.currentAttackSequence != null)
                {
                    stanceGuideReference.ShowAllBatons();
                    stanceGuideReference.StartAllBatons();
                }
            }
        }
    }

    private void UpdateStanceGuideButtonText()
    {
        if (stanceGuideButtonText != null)
        {
            stanceGuideButtonText.text = enableAutoDetectSequences ? "Disable Guide" : "Enable Guide";
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
        
        
        ResetRepeatTracking();

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
    if (objectiveVideoRawImage != null) objectiveVideoRawImage.gameObject.SetActive(false); 
    if (stanceEntryImage != null) stanceEntryImage.gameObject.SetActive(true);
    if (repeatCountText != null) repeatCountText.gameObject.SetActive(false); 
    
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
        if (objectiveVideoRawImage != null) objectiveVideoRawImage.gameObject.SetActive(false);
        if (objectiveVideoPlayer != null)
        {
            objectiveVideoPlayer.Stop();
        }
        if (feedbackImage != null) feedbackImage.gameObject.SetActive(false);
        if (stanceEntryImage != null) stanceEntryImage.gameObject.SetActive(false);
        if (repeatCountText != null) repeatCountText.gameObject.SetActive(false);
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

        if (currentRepeatCount == 0)
        {
            maxRepeatsForCurrentObjective = objective.enableRepeat ? objective.repeatCount : 1;
            currentObjectiveRepeatAccuracies.Clear();
        }

        isWaitingForStanceEntry = true;
        isWaitingForTrainingInput = false;
        currentRequiredStance = objective.requiredStance;
        
        
        bool isRightHandDominant = GetRightHandDominance();
        objectiveText.text = objective.GetInstruction(isRightHandDominant);
        
        UpdateRepeatCountDisplay();
        
        
        stanceEntryImage.gameObject.SetActive(false);
        
        
        isVideoMode = false;

        if (objective.instructionImage != null)
        {
            
            objectiveVideoRawImage.gameObject.SetActive(false);
            objectiveImage.gameObject.SetActive(true);
            objectiveImage.sprite = objective.instructionImage;
        }
        else if (objective.instructionVideo != null && objectiveVideoPlayer != null)
        {
            
            objectiveImage.gameObject.SetActive(false);
            objectiveVideoRawImage.gameObject.SetActive(true);
            objectiveVideoPlayer.clip = objective.instructionVideo;
            objectiveVideoPlayer.Play();
            isVideoMode = true;
        }
        else
        {
            
            objectiveVideoRawImage.gameObject.SetActive(false);
            objectiveImage.gameObject.SetActive(false);
        }

        
        if (objectiveVideoPlayer != null && !isVideoMode)
        {
            objectiveVideoPlayer.Stop();
        }
        
        if (trainingModePanel != null)
        {
            trainingModePanel.SetActive(false);
        }
        
        StanceManager.Instance.EnterStance(objective.requiredStance, trainingMode);
    }

    
    private void UpdateRepeatCountDisplay()
    {
        if (repeatCountText != null && currentObjectiveIndex >= 0 && currentObjectiveIndex < objectives.Count)
        {
            LevelObjective currentObjective = objectives[currentObjectiveIndex];
            if (currentObjective.enableRepeat && currentObjective.repeatCount > 1)
            {
                repeatCountText.text = $"Repeat: {currentRepeatCount + 1}/{currentObjective.repeatCount}";
                repeatCountText.gameObject.SetActive(true);
            }
            else
            {
                repeatCountText.gameObject.SetActive(false);
            }
        }
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
    
    
    ResetRepeatTracking();
    
    
    if (objectiveAccuracies.ContainsKey(currentObjectiveIndex))
    {
        objectiveAccuracies.Remove(currentObjectiveIndex);
    }
    
    
    if (AccuracyTracker.Instance != null)
    {
        AccuracyTracker.Instance.ResetTracking();
    }
    
    
    if (StanceManager.Instance != null)
    {
        StanceManager.Instance.ClearAllStances();
        StanceManager.Instance.totalBoxesTouched = 0;
        StanceManager.Instance.SetGameActive(true);
        
        
        if (StanceManager.Instance.currentAttackSequence != null)
        {
            foreach (var box in StanceManager.Instance.currentAttackSequence.sequenceBoxes)
            {
                if (box != null)
                {
                    StanceDetector detector = box.GetComponent<StanceDetector>();
                    if (detector != null)
                    {
                        detector.IsCompleted = false;
                        detector.ForceResetTriggerState();
                    }
                }
            }
        }
    }
    
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
            ResetRepeatTracking(); 
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

        var sequence = StanceManager.Instance.currentAttackSequence;
        if (sequence != null)
        {
            if (sequence.startBoxLeft == null && sequence.startBoxRight == null)
            {
                Debug.LogError($"Sequence {sequence.sequenceName} has no start boxes configured!");
            }

            if (sequence.endBoxLeft == null && sequence.endBoxRight == null)
            {
                Debug.LogError($"Sequence {sequence.sequenceName} has no end boxes configured!");
            }
        }

        
        bool isRightHandDominant = GetRightHandDominance();
        objectiveText.text = objective.GetInstruction(isRightHandDominant);

        
        
        stanceEntryImage.gameObject.SetActive(false);

        StanceManager.Instance.EnterStance(objective.requiredStance, trainingMode);
    }


    
    private void ToggleVideoImageDisplay(LevelObjective objective)
    {
        isVideoMode = !isVideoMode;
        
        if (isVideoMode && objective.instructionVideo != null)
        {
            
            objectiveImage.gameObject.SetActive(false);
            objectiveVideoRawImage.gameObject.SetActive(true);
            
            
            if (objectiveVideoPlayer != null)
            {
                objectiveVideoPlayer.clip = objective.instructionVideo;
                objectiveVideoPlayer.Play();
            }
        }
        else if (objective.instructionImage != null)
        {
            
            objectiveVideoRawImage.gameObject.SetActive(false);
            objectiveImage.gameObject.SetActive(true);
            objectiveImage.sprite = objective.instructionImage;
            
            
            if (objectiveVideoPlayer != null && objectiveVideoPlayer.isPlaying)
            {
                objectiveVideoPlayer.Stop();
            }
        }
    }

    public void OnToggleVideoButtonPressed()
    {
        if (currentObjectiveIndex >= 0 && currentObjectiveIndex < objectives.Count)
        {
            LevelObjective currentObjective = objectives[currentObjectiveIndex];
            if (currentObjective.instructionVideo != null && currentObjective.instructionImage != null)
            {
                ToggleVideoImageDisplay(currentObjective);
            }
        }
    }

    private bool GetRightHandDominance()
    {
        Pause pauseScript = FindObjectOfType<Pause>();
        if (pauseScript != null)
        {
            return pauseScript.GetRightHandDominance();
        }

        const string RIGHT_HAND_PREF_KEY = "RightHandDominant";
        return PlayerPrefs.GetInt(RIGHT_HAND_PREF_KEY, 1) == 1;
    }

    
public void EndObjective()
{
    
    if (objectiveVideoPlayer != null && objectiveVideoPlayer.isPlaying)
    {
        objectiveVideoPlayer.Stop();
    }

    
    currentObjectiveAccuracy = CalculateCurrentObjectiveAccuracy();
    
    int score = 0;
    if (!trainingMode)
    {
        score = CalculateScore();
    }

    
    if (currentObjectiveIndex >= 0 && currentObjectiveIndex < objectives.Count)
    {
        LevelObjective currentObjective = objectives[currentObjectiveIndex];

        
        currentObjectiveRepeatAccuracies.Add(currentObjectiveAccuracy);
        currentRepeatCount++;

        
        if (currentObjective.enableRepeat && currentRepeatCount < currentObjective.repeatCount)
        {
            
            if (trainingMode)
            {
                
                StartCoroutine(StartRepeatAfterDelay(1f));
                return;
            }
            else
            {
                totalScore += score;
                UpdateScoreDisplay();
                DisplayFeedback(score);
                StartCoroutine(StartRepeatAfterDelay(2f));
                return;
            }
        }
        else
        {
            
            float finalAccuracy = CalculateAverageAccuracy(currentObjectiveRepeatAccuracies);
            objectiveAccuracies[currentObjectiveIndex] = finalAccuracy;
            
            
            ResetRepeatTracking();
        }
    }

    
    if (trainingMode)
    {
        ShowTrainingModePanel(); 
    }
    else
    {
        totalScore += score;
        DisplayFeedback(score);
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

    UpdateScoreDisplay();
}

    
    private void ResetRepeatTracking()
    {
        currentRepeatCount = 0;
        maxRepeatsForCurrentObjective = 0;
        currentObjectiveRepeatAccuracies.Clear();
    }


    private float CalculateAverageAccuracy(List<float> accuracies)
    {
        if (accuracies.Count == 0) return 0f;
        
        float total = 0f;
        foreach (float accuracy in accuracies)
        {
            total += accuracy;
        }
        return total / accuracies.Count;
    }

    
    private IEnumerator StartRepeatAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        feedbackImage.gameObject.SetActive(false);
        
        if (currentObjectiveIndex >= 0 && currentObjectiveIndex < objectives.Count)
        {
            StartStanceEntry(objectives[currentObjectiveIndex]);
        }
    }



private void ShowTrainingModePanel()
{
    isWaitingForTrainingInput = true;

    if (trainingModePanel != null)
    {
        trainingModePanel.SetActive(true);

        if (accuracyText != null)
        {
            
            float finalAvgAccuracy = 0f;
            if (objectiveAccuracies.ContainsKey(currentObjectiveIndex))
            {
                finalAvgAccuracy = objectiveAccuracies[currentObjectiveIndex];
            }
            
            
            string accuracyDetails = $"Average Accuracy: {(finalAvgAccuracy * 100):F1}%";
            
            accuracyText.text = accuracyDetails;
        }

        if (trainingModeInstructionText != null)
        {
            trainingModeInstructionText.text = "Press B to Retry Objective\nPress A to Continue";
        }
    }
    else
    {
        Debug.LogError("Training Mode Panel not assigned!");
        ContinueToNextObjective();
    }
}


private void RetryCurrentObjective()
{
    isWaitingForTrainingInput = false;
    
    if (trainingModePanel != null)
    {
        trainingModePanel.SetActive(false);
    }
    
    
    if (AccuracyTracker.Instance != null)
    {
        AccuracyTracker.Instance.ResetTracking(); 
    }
    
    
    if (StanceManager.Instance != null)
    {
        StanceManager.Instance.ClearAllStances();
        StanceManager.Instance.totalBoxesTouched = 0; 
        StanceManager.Instance.SetGameActive(true); 
        
        
        if (StanceManager.Instance.currentAttackSequence != null)
        {
            
            foreach (var box in StanceManager.Instance.currentAttackSequence.sequenceBoxes)
            {
                if (box != null)
                {
                    StanceDetector detector = box.GetComponent<StanceDetector>();
                    if (detector != null)
                    {
                        detector.IsCompleted = false;
                        detector.ForceResetTriggerState();
                    }
                }
            }
        }
    }
    
    
    ResetRepeatTracking();
    
    
    if (objectiveAccuracies.ContainsKey(currentObjectiveIndex))
    {
        objectiveAccuracies.Remove(currentObjectiveIndex);
    }
    
    
    if (currentObjectiveIndex >= 0 && currentObjectiveIndex < objectives.Count)
    {
        StartStanceEntry(objectives[currentObjectiveIndex]);
    }
    else
    {
        Debug.LogError("Retry failed: Invalid objective index");
        EndLevel();
    }
}
    
    
    private void ContinueToNextObjective()
    {
        isWaitingForTrainingInput = false;

        if (trainingModePanel != null)
        {
            trainingModePanel.SetActive(false);
        }

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
    if (StanceManager.Instance == null || StanceManager.Instance.currentAttackSequence == null)
    {
        Debug.LogWarning("StanceManager or currentAttackSequence is null when calculating accuracy");
        return 0f;
    }

    int totalBoxes = StanceManager.Instance.currentAttackSequence.sequenceBoxes.Length;
    int touchedBoxes = StanceManager.Instance.totalBoxesTouched;

    Debug.Log($"Calculating accuracy: {touchedBoxes}/{totalBoxes} = {(totalBoxes > 0 ? (float)touchedBoxes / totalBoxes : 0f)}");
    
    return totalBoxes > 0 ? (float)touchedBoxes / totalBoxes : 0f;
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
        
        if (objectiveVideoPlayer != null && objectiveVideoPlayer.isPlaying)
        {
            objectiveVideoPlayer.Stop();
        }

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

        float accuracy;
        if (trainingMode) {
            accuracy = CalculateTrainingModeFinalAccuracy();
        } else if (AccuracyTracker.Instance != null) {
            accuracy = AccuracyTracker.Instance.CalculateAccuracy();
        } else {
            accuracy = CalculateAccuracy();
        }

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
            if (AccuracyTracker.Instance != null && !trainingMode)
            {
                int totalBoxes = AccuracyTracker.Instance.GetTotalBoxes();
                int totalBoxesTouched = AccuracyTracker.Instance.GetTotalBoxesTouched();

                ResultsManager.Instance.ShowResults(totalScore, accuracy, trainingMode, totalBoxes, totalBoxesTouched);
            }
            else
            {
                ResultsManager.Instance.ShowResults(totalScore, accuracy, trainingMode);
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

[System.Serializable]
public class LevelObjective
{
    [Header("Instructions")]
    public string instruction;
    public string rightHandInstruction;

    [Header("Visual Content")]
    public Sprite instructionImage;
    public VideoClip instructionVideo;

    [Header("Requirements")]
    public string requiredStance;
    public float timeLimit;

    [Header("Repeat Settings")]
    public bool enableRepeat = false;
    [Range(1, 10)]
    public int repeatCount = 1;

    public string GetInstruction(bool isRightHandDominant)
    {
        if (isRightHandDominant && !string.IsNullOrEmpty(rightHandInstruction))
        {
            return rightHandInstruction;
        }

        return instruction;
    }
}