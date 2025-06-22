using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SparManager : MonoBehaviour, ILevelManager
{
    public static SparManager Instance;
    private bool isGameActive = false;

    [Header("Round Settings")]
    public float roundDuration = 120f;
    public int totalRounds = 3;
    private int currentRound = 1;
    private float roundTimer = 0f;

    [Header("Phase Settings")]
    public float phaseTransitionDelay = 2f;
    public float defaultObjectiveTimeout = 5f;
    public int phase2InitialValue = 300;
    public int phase2SequenceReduction = 5;
    public int phase2BlockReduction = 10;
    private int phase2Value;
    private Coroutine sparringPartnerCoroutine;

    [Header("Animation Event Settings")]
    public bool useAnimationEvents = true;
    public float earlyPenaltyThreshold = 0.5f;

    [Header("Timing Bar UI")]
    public GameObject timingBarPanel;
    public Image timingBarFill;
    public Image timingBarBackground;
    public Color barWhiteColor = Color.white;
    public Color barRedColor = Color.red;
    public float timingBarDuration = 3f;

    [Header("Animation Event")]
    public bool useAutomaticTiming = true;
    [Range(0.5f, 0.9f)]
    public float attackWindowPercentage = 0.7f;

    private float objectiveStartTime = 0f;
    private float objectiveEndTime = 0f;
    private float objectiveDuration = 0f;
    private float attackWindowStartTime = 0f;
    private bool isInAttackWindow = false;
    private bool canPlayerAct = false;

    public GameObject phase1FeedbackPanel;
    public TextMeshProUGUI phase1FeedbackText;
    public Color blockColor = Color.green;
    public Color missColor = Color.red;
    public float feedbackDisplayDuration = 2f;


    private bool objectiveWindowActive = false;
    private bool timingBarActive = false;
    private float timingBarTimer = 0f;
    private bool playerActedTooEarly = false;

    [Header("Game Speed ")]
    [Range(0.1f, 2.0f)]
    public float gameSpeed = 1.0f;

    [Header("UI References")]

    public GameObject sparManagerUIPanel;
    public TextMeshProUGUI phaseText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI playerScoreText;
    public TextMeshProUGUI opponentScoreText;
    public TextMeshProUGUI roundText;
    public GameObject Result;
    public TextMeshProUGUI winnerText;
    public TextMeshProUGUI finalScoreText;

    [Header("Results Management")]
    public bool useResultsManager = true;

    [Header("Sparring Partner Settings")]
    public string sparringPartnerAnimation = "Default";
    public bool useSparringPartner = false;
    [Range(0f, 10f)]
    public float sparringPartnerDelay = 2f;
    [Range(0.1f, 3.0f)]
    public float sparringPartnerAnimationSpeed = 1f;
    private GameObject sparringPartner;
    private Animator sparringPartnerAnimator;

    [System.Serializable]
    public class PhaseObjective
    {
        public string stanceName;
        public float timeout;
        [Range(0.1f, 3.0f)]

        [Header("Animation Event Timing")]
        public float objectiveActivationTime = 1f;
        public float attackHitTime = 2f;
        public bool useAnimationEvents = true;
    }
    public List<PhaseObjective> phase1Objectives = new List<PhaseObjective>();

    [Header("Phase 2 Settings")]
    public List<string> phase2Stances = new List<string>();

    [Header("Opponent Block Settings")]
    [Range(0.0f, 1.0f)]
    public float baseBlockChance = 0.2f;
    [Range(0.0f, 0.5f)]
    public float blockChanceIncrement = 0.05f;
    public float blockChanceResetTime = 15.0f;
    public GameObject blockFeedbackText;

    private int currentPhase = 0;
    private int currentObjectiveIndex = 0;
    private float timer = 0f;
    private int playerScore = 0;
    private int opponentScore = 0;
    private bool objectiveActive = false;
    private bool isGameOver = false;

    private float currentBlockChance;
    private float blockChanceResetTimer;
    private string lastPlayerStance = "";
    private Dictionary<string, int> stanceUseCount = new Dictionary<string, int>();

    [Header("Tutorial Settings")]
    public bool isTutorialMode = false;
    public float tutorialAnimationPauseTime = 3f;
    public string tutorialInstructionText = "One";
    public string tutorialPhase2Text = "Two";
    public GameObject tutorialInstructionPanel;
    public TextMeshProUGUI tutorialInstructionTextUI;

    [Header("Stance Guide ")]
    public bool enableAutoDetectSequences = false;


    private bool tutorialAnimationPaused = false;
    private bool tutorialInstructionShown = false;
    private int tutorialPhase2CompletedStances = 0;
    private HashSet<string> tutorialCompletedStanceTypes = new HashSet<string>();
    private SparResultsManager resultsManager;

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

    private void Start()
    {
        Time.timeScale = gameSpeed;
        ConfigureStanceGuide();

        if (useResultsManager)
        {
            resultsManager = FindObjectOfType<SparResultsManager>();
            if (resultsManager == null)
            {
                Debug.LogWarning("SparResultsManager not found but useResultsManager is true");
            }
        }
    }

    private void ConfigureStanceGuide()
    {
        StanceGuide stanceGuide = FindObjectOfType<StanceGuide>();
        if (stanceGuide != null)
        {
            stanceGuide.autoDetectSequences = enableAutoDetectSequences;
        }
    }

    public void SetSparringPartner(GameObject partner)
    {
        sparringPartner = partner;
        if (sparringPartner != null)
        {
            useSparringPartner = true;
            sparringPartnerAnimator = sparringPartner.GetComponent<Animator>();
            if (sparringPartnerAnimator != null)
            {
                sparringPartnerAnimator.enabled = false;
                Debug.Log("Sparring partner animator found and disabled to prevent T-pose");
            }
            else
            {
                Debug.LogWarning("Sparring partner does not have an Animator component");
            }
        }
    }



    public void StartLevel()
    {
        if (sparManagerUIPanel != null)
        {
            sparManagerUIPanel.SetActive(true);
            Debug.Log("SparManager UI enabled");
        }

        isGameActive = true;
        isGameOver = false;
        playerScore = 0;
        opponentScore = 0;
        currentRound = 1;


        if (isTutorialMode)
        {
            tutorialAnimationPaused = false;
            tutorialInstructionShown = false;
            tutorialPhase2CompletedStances = 0;
            tutorialCompletedStanceTypes.Clear();


            if (timerText != null)
            {
                timerText.gameObject.SetActive(false);
            }

            Debug.Log("Tutorial mode activated");
        }
        else
        {

            if (timerText != null)
            {
                timerText.gameObject.SetActive(true);
            }
        }

        currentBlockChance = baseBlockChance;
        blockChanceResetTimer = blockChanceResetTime;
        stanceUseCount.Clear();

        if (Result != null && !useResultsManager)
        {
            Result.SetActive(false);
        }

        if (StanceManager.Instance != null)
        {
            StanceManager.Instance.useSparManager = true;
        }

        UpdateScoreDisplay();
        UpdateRoundDisplay();

        phase2Value = phase2InitialValue;
        roundTimer = roundDuration;
        StartCoroutine(StartPhase1());
    }


    private void Update()
    {
        if (!isGameActive || isGameOver) return;

        if (Time.timeScale != gameSpeed)
        {
            Time.timeScale = gameSpeed;
        }
        if (timingBarActive)
        {
            UpdateTimingBar();
        }
        if (useAutomaticTiming && objectiveWindowActive && currentPhase == 1)
        {
            CheckAttackWindow();
        }

        if (isTutorialMode && canPlayerAct && !tutorialInstructionShown && currentPhase == 1)
        {
            ShowTutorialInstructionWhenReady();
        }

        if (!isTutorialMode)
        {
            roundTimer -= Time.deltaTime;
            UpdateTimerDisplay();

            if (roundTimer <= 0)
            {
                EndRound();
                return;
            }
        }

        if (currentPhase == 2)
        {
            if (!isTutorialMode)
            {
                phase2Value -= Mathf.RoundToInt(Time.deltaTime);
            }

            blockChanceResetTimer -= Time.deltaTime;
            if (blockChanceResetTimer <= 0)
            {
                ResetBlockChance();
                blockChanceResetTimer = blockChanceResetTime;
            }

            if (!isTutorialMode && phase2Value <= 0)
            {
                Debug.Log("Phase 2 value reached zero, returning to Phase 1");
                CleanupPhase2();
                StartCoroutine(StartPhase1());
            }
        }
        else if (currentPhase == 1 && objectiveActive && !isTutorialMode)
        {
            timer -= Time.deltaTime;

            if (timer <= 0)
            {
                Debug.Log($"Time expired for Phase 1 objective {currentObjectiveIndex + 1}. Moving to next objective.");
                NextObjective();
            }
        }
    }

    private void UpdateTimingBar()
    {
        if (!timingBarActive || timingBarPanel == null) return;

        if (timingBarFill != null)
        {

            timingBarFill.fillAmount = 1f;

            if (canPlayerAct)
            {
                timingBarFill.color = barRedColor;
                if (timingBarBackground != null)
                {
                    timingBarBackground.color = barRedColor;
                }
            }
            else
            {
                timingBarFill.color = barWhiteColor;
                if (timingBarBackground != null)
                {
                    timingBarBackground.color = barWhiteColor;
                }
            }
        }
    }


    private void EndRound()
    {
        currentRound++;
        Debug.Log($"Round {currentRound - 1} ended.");

        if (currentRound > totalRounds)
        {
            EndGame();
            return;
        }

        roundTimer = roundDuration;
        phase2Value = phase2InitialValue;
        UpdateRoundDisplay();

        if (currentPhase == 2)
        {
            CleanupPhase2();
        }

        StartCoroutine(StartPhase1());
    }

    private void UpdateRoundDisplay()
    {
        if (roundText != null)
        {
            roundText.text = $"Round {currentRound}/{totalRounds}";
        }
    }

    private IEnumerator StartPhase1()
    {
        yield return new WaitForSeconds(1f);

        if (phaseText != null)
        {
            phaseText.text = "Phase 1";
        }

        currentPhase = 1;
        currentObjectiveIndex = 0;

        if (useSparringPartner && sparringPartner != null)
        {
            Debug.Log("Preparing sparring partner for Phase 1");
            PrepareSparringPartnerForPhase1();
        }

        StartNextObjective();
    }

    private void PrepareSparringPartnerForPhase1()
    {
        if (sparringPartner == null) return;

        if (sparringPartnerCoroutine != null)
        {
            StopCoroutine(sparringPartnerCoroutine);
            sparringPartnerCoroutine = null;
        }

        if (sparringPartnerAnimator == null)
        {
            sparringPartnerAnimator = sparringPartner.GetComponent<Animator>();
        }

        if (sparringPartnerAnimator != null && !string.IsNullOrEmpty(sparringPartnerAnimation))
        {
            sparringPartnerAnimator.enabled = false;
            Debug.Log("Sparring partner animator disabled to prevent T-pose during delay");


            sparringPartnerCoroutine = StartCoroutine(StartSparringPartnerWithProperSetup());
        }
        else
        {
            Debug.LogWarning("Sparring partner animator or animation name is missing");
        }
    }


    private IEnumerator StartSparringPartnerWithProperSetup()
    {
        Debug.Log($"Waiting {sparringPartnerDelay} seconds before enabling sparring partner animation...");

        yield return new WaitForSeconds(sparringPartnerDelay);

        if (currentPhase == 1 && isGameActive && !isGameOver &&
            sparringPartnerAnimator != null && !string.IsNullOrEmpty(sparringPartnerAnimation))
        {
            sparringPartnerAnimator.enabled = true;
            yield return null;

            sparringPartnerAnimator.speed = sparringPartnerAnimationSpeed;
            sparringPartnerAnimator.Play(sparringPartnerAnimation, 0, 0f);

            Debug.Log($"Sparring partner animation '{sparringPartnerAnimation}' started with speed {sparringPartnerAnimationSpeed}");


            if (isTutorialMode)
            {
                Debug.Log($"Tutorial mode: Will pause animation at {tutorialAnimationPauseTime} seconds");
                yield return new WaitForSeconds(tutorialAnimationPauseTime);

                if (sparringPartnerAnimator != null)
                {
                    sparringPartnerAnimator.speed = 0f;
                    tutorialAnimationPaused = true;
                    Debug.Log("Tutorial: Animation paused for instruction");

                    ShowTutorialInstruction();
                }
            }
        }
        else
        {
            Debug.LogWarning($"Cannot start sparring partner animation - Phase: {currentPhase}, Active: {isGameActive}, GameOver: {isGameOver}");
        }
    }


    private void StopSparringPartnerAnimation()
    {
        if (sparringPartnerCoroutine != null)
        {
            StopCoroutine(sparringPartnerCoroutine);
            sparringPartnerCoroutine = null;
        }

        if (sparringPartnerAnimator != null)
        {
            sparringPartnerAnimator.enabled = false;
            Debug.Log("Sparring partner animation stopped and animator disabled");
        }
    }


  private void StartNextObjective()
{
    if (currentObjectiveIndex < phase1Objectives.Count)
    {
        ClearCurrentObjectiveBoxes();

        objectiveActive = true;
        canPlayerAct = false;
        playerActedTooEarly = false;

        string requiredStance = phase1Objectives[currentObjectiveIndex].stanceName;
        float objectiveTimeout = phase1Objectives[currentObjectiveIndex].timeout;

        Debug.Log($"Starting objective {currentObjectiveIndex + 1}: {requiredStance}");

        if (!isTutorialMode)
        {
            timer = objectiveTimeout > 0 ? objectiveTimeout : defaultObjectiveTimeout;
            Debug.Log($"Objective timer set to: {timer} seconds");
        }

        if (timingBarPanel != null)
        {
            StartTimingBar();
        }

        if (useSparringPartner && sparringPartnerAnimator != null && sparringPartnerAnimator.enabled)
        {
            sparringPartnerAnimator.speed = sparringPartnerAnimationSpeed;
            Debug.Log($"Updated sparring partner animation speed to: {sparringPartnerAnimationSpeed}");
        }

        
        StartCoroutine(StartSequenceAfterDelay(requiredStance, 0.1f));
    }
    else
    {
        StartCoroutine(StartPhase2());
    }
}

private IEnumerator StartSequenceAfterDelay(string stanceName, float delay)
{
    yield return new WaitForSeconds(delay);
    
    
    SetAutoDetectSequences(false);
    
    StartSequenceForObjective(stanceName);
}



private IEnumerator ForceStanceGuideUpdate()
{
    yield return new WaitForEndOfFrame();
    
    
    StanceGuide stanceGuide = FindObjectOfType<StanceGuide>();
    if (stanceGuide != null)
    {
        stanceGuide.ResetSequenceDetection();
        Debug.Log("Forced StanceGuide to reset sequence detection");
    }
}
    private void StartSequenceForObjective(string stanceName)
    {
        if (StanceManager.Instance == null) return;


        StanceManager.Instance.EnterStance(stanceName, false);


        foreach (var style in StanceManager.Instance.arnisStyles)
        {
            if (style.styleName == stanceName && style.sequences.Count > 0)
            {
                var firstSequence = style.sequences[0];
                StanceManager.Instance.StartAttackSequence(firstSequence);
                Debug.Log($"Started sequence {firstSequence.sequenceName} for stance {stanceName}");
                break;
            }
        }
    }
    private void StartTutorialSequenceDirectly(string stanceName)
    {
        if (StanceManager.Instance == null) return;


        StanceManager.Instance.EnterStance(stanceName, false);


        foreach (var style in StanceManager.Instance.arnisStyles)
        {
            if (style.styleName == stanceName && style.sequences.Count > 0)
            {

                var firstSequence = style.sequences[0];


                StanceManager.Instance.StartAttackSequence(firstSequence);

                Debug.Log($"Tutorial: Started sequence {firstSequence.sequenceName} directly for stance {stanceName}");
                break;
            }
        }
    }



private void ClearCurrentObjectiveBoxes()
{
    if (StanceManager.Instance == null) return;

    
    SetAutoDetectSequences(false);
    
    StanceManager.Instance.ClearAllStances();

    ResetStanceGuide();

    Debug.Log($"Cleared boxes and disabled StanceGuide for objective transition");
}
private void NextObjective()
{
    objectiveActive = false;
    objectiveWindowActive = false;
    StopTimingBar();
    
    
    SetAutoDetectSequences(false);
    
    
    ResetStanceGuide();
    
    
    if (!useAnimationEvents || !phase1Objectives[currentObjectiveIndex].useAnimationEvents)
    {
        if (timer <= 0)
        {
            opponentScore++;
            Debug.Log($"Player failed Phase 1 objective {currentObjectiveIndex + 1}. Opponent scores! Opponent Score: {opponentScore}");
            UpdateScoreDisplay();
        }
    }
    
    currentObjectiveIndex++;
    StartNextObjective();
}



    public void NotifyPhase1Completion(string stanceName, string sequenceName)
{
    if (!isGameActive || isGameOver || currentPhase != 1 || !objectiveActive) return;
    
    string currentObjective = phase1Objectives[currentObjectiveIndex].stanceName;
    
    if (stanceName == currentObjective)
    {
        if (!canPlayerAct)
        {
            playerActedTooEarly = true;
            opponentScore++;
            Debug.Log($"Player acted too early for objective {currentObjectiveIndex + 1}. Opponent scores! Opponent Score: {opponentScore}");
            UpdateScoreDisplay();
            
            ShowPhase1Feedback("MISS", missColor);
            return;
        }
        else
        {
            playerScore++;
            Debug.Log($"Phase 1 objective {currentObjectiveIndex + 1} completed successfully: {stanceName}.{sequenceName}");
            UpdateScoreDisplay();
            
            ShowPhase1Feedback("BLOCK", blockColor);
            
            objectiveActive = false;
            canPlayerAct = false;
            StopTimingBar();
            
            
            SetAutoDetectSequences(false);
            ResetStanceGuide();
            
            if (isTutorialMode)
            {
                if (tutorialInstructionPanel != null)
                {
                    tutorialInstructionPanel.SetActive(false);
                }
                
                if (tutorialAnimationPaused && sparringPartnerAnimator != null)
                {
                    sparringPartnerAnimator.speed = sparringPartnerAnimationSpeed;
                    tutorialAnimationPaused = false;
                    Debug.Log("Tutorial: Animation resumed");
                    
                    StartCoroutine(TutorialTransitionToPhase2());
                    return;
                }
            }
            
            
            StartCoroutine(DelayedNextObjective());
        }
    }
}

    private IEnumerator TutorialTransitionToPhase2()
    {
        yield return new WaitForSeconds(2f);

        Debug.Log("Tutorial: Transitioning to Phase 2");
        StartCoroutine(StartPhase2());
    }

    private IEnumerator StartPhase2()
    {
        Debug.Log("Transitioning to Phase 2");

        if (useSparringPartner)
        {
            StopSparringPartnerAnimation();
        }

        if (StanceManager.Instance != null)
        {
            StanceManager.Instance.ClearAllStances();
        }

        StanceManager.Instance.EnterStance("Default");
        objectiveActive = false;

        yield return new WaitForSeconds(phaseTransitionDelay);

        if (phaseText != null)
        {
            phaseText.text = "Phase 2";
        }

        currentPhase = 2;


        SetAutoDetectSequences(true);

        ResetBlockChance();
        stanceUseCount.Clear();
        blockChanceResetTimer = blockChanceResetTime;

        ActivatePhase2Stances();

        if (isTutorialMode)
        {
            yield return new WaitForSeconds(0.5f);
            ShowTutorialPhase2Instruction();
        }
    }

    private void ActivatePhase2Stances()
    {
        if (StanceManager.Instance != null)
        {
            Debug.Log("Activating all Phase 2 stance options");

            StanceManager.Instance.ActivatePhase2Stances(phase2Stances);

            StanceManager.Instance.OnStanceChanged += HandlePhase2StanceChange;
        }
    }

    private void CleanupPhase2()
    {
        if (StanceManager.Instance != null)
        {
            Debug.Log("Cleaning up Phase 2 stances");

            StanceManager.Instance.OnStanceChanged -= HandlePhase2StanceChange;

            StanceManager.Instance.ClearAllStances();

            StanceManager.Instance.EnterStance("Default");
        }
    }

    private void HandlePhase2StanceChange(string newStance)
    {
        if (currentPhase != 2 || !isGameActive || isGameOver) return;

        if (newStance != "Default")
        {
            Debug.Log($"Player entered {newStance} stance in Phase 2");
            lastPlayerStance = newStance;

            if (!stanceUseCount.ContainsKey(newStance))
            {
                stanceUseCount[newStance] = 0;
            }
            stanceUseCount[newStance]++;
        }
        else
        {
            Debug.Log("Player returned to default stance, reactivating Phase 2 options");
            StanceManager.Instance.ActivatePhase2Stances(phase2Stances);
        }
    }

    public void OnSequenceCompleted(string stanceName, string sequenceName)
    {
        if (!isGameActive || isGameOver) return;

        if (currentPhase == 2)
        {

            if (isTutorialMode && tutorialInstructionPanel != null && tutorialInstructionPanel.activeInHierarchy)
            {
                tutorialInstructionPanel.SetActive(false);
                Debug.Log("Tutorial Phase 2 instruction hidden after first objective completion");
            }

            if (isTutorialMode)
            {

                Debug.Log("Tutorial completed after 1 sequence! Ending game.");
                EndGame();
                return;
            }

            bool blocked = CheckIfOpponentBlocks(stanceName);

            if (blocked)
            {
                ShowBlockFeedback();
                Debug.Log($"Opponent blocked {stanceName}.{sequenceName} in Phase 2!");

                if (!isTutorialMode)
                {
                    phase2Value -= phase2BlockReduction;
                    phase2Value = Mathf.Max(0, phase2Value);
                }
            }
            else
            {
                playerScore++;
                Debug.Log($"Player completed {stanceName}.{sequenceName} in Phase 2! Score: {playerScore}");
                UpdateScoreDisplay();

                if (!isTutorialMode)
                {
                    phase2Value -= phase2SequenceReduction;
                    phase2Value = Mathf.Max(0, phase2Value);
                }
            }

            IncreaseBlockChance(stanceName);


            if (StanceManager.Instance != null)
            {
                StanceManager.Instance.EnterStance("Default");
                StartCoroutine(ReactivatePhase2StancesAfterDelay());
            }
        }
    }
private IEnumerator ReactivatePhase2StancesAfterDelay()
{
    yield return new WaitForSeconds(0.1f); 
    if (currentPhase == 2 && StanceManager.Instance != null)
    {
        StanceManager.Instance.ActivatePhase2Stances(phase2Stances);
        Debug.Log("Phase 2 stances reactivated after sequence completion");
    }
}

    private bool CheckIfOpponentBlocks(string stanceName)
    {
        float finalBlockChance = currentBlockChance;

        if (stanceUseCount.ContainsKey(stanceName))
        {
            int timesUsed = stanceUseCount[stanceName];
            finalBlockChance += blockChanceIncrement * (timesUsed - 1);
        }

        finalBlockChance = Mathf.Min(finalBlockChance, 0.8f);

        Debug.Log($"Block chance for {stanceName}: {finalBlockChance:P}");

        return Random.value < finalBlockChance;
    }

    private void IncreaseBlockChance(string stanceName)
    {
        currentBlockChance += blockChanceIncrement * 0.5f;

        blockChanceResetTimer = blockChanceResetTime;

        Debug.Log($"Base block chance increased to: {currentBlockChance:P}");
    }

    private void ResetBlockChance()
    {
        currentBlockChance = baseBlockChance;
        Debug.Log("Block chance reset to base value");
    }

    private void ShowBlockFeedback()
    {
        if (blockFeedbackText != null)
        {
            StartCoroutine(ShowBlockFeedbackCoroutine());
        }
    }

    private IEnumerator ShowBlockFeedbackCoroutine()
    {
        blockFeedbackText.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        blockFeedbackText.SetActive(false);
    }

    private void UpdateScoreDisplay()
    {
        if (playerScoreText != null)
        {
            playerScoreText.text = $"Player: {playerScore}";
        }

        if (opponentScoreText != null)
        {
            opponentScoreText.text = $"Opponent: {opponentScore}";
        }
    }
    private void SetAutoDetectSequences(bool enabled)
    {
        StanceGuide stanceGuide = FindObjectOfType<StanceGuide>();
        if (stanceGuide != null)
        {
            stanceGuide.autoDetectSequences = enabled;
            Debug.Log($"Auto-detect sequences set to: {enabled}");
        }
    }


    private void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(roundTimer / 60);
            int seconds = Mathf.FloorToInt(roundTimer % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    public void SetGameActive(bool active)
    {
        isGameActive = active;

        if (!active)
        {
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = gameSpeed;
        }
    }

    public void SetGameSpeed(float speed)
    {
        gameSpeed = Mathf.Clamp(speed, 0.1f, 2.0f);
        if (isGameActive)
        {
            Time.timeScale = gameSpeed;
        }
    }

    private void EndGame()
    {
        isGameOver = true;

        if (StanceManager.Instance != null)
        {
            StanceManager.Instance.OnStanceChanged -= HandlePhase2StanceChange;
        }

        StanceManager.Instance.EnterStance("Default");

        if (useSparringPartner && sparringPartner != null)
        {
            sparringPartner.SetActive(false);
        }

        DisableAllBoxes();

        if (phase1FeedbackPanel != null)
        {
            phase1FeedbackPanel.SetActive(false);
        }

        if (sparManagerUIPanel != null)
        {
            sparManagerUIPanel.SetActive(false);
            Debug.Log("SparManager UI disabled");
        }

        if (sparManagerUIPanel != null)
        {
            sparManagerUIPanel.SetActive(false);
            Debug.Log("SparManager UI disabled");
        }

        if (useResultsManager && resultsManager != null)
        {
            resultsManager.ShowSparResults(playerScore, opponentScore, totalRounds);
        }
        else if (Result != null)
        {
            if (finalScoreText != null)
            {
                if (isTutorialMode)
                {
                    finalScoreText.text = "Tutorial Completed!\nWell done!";
                }
                else
                {
                    finalScoreText.text = $"Player: {playerScore} - Opponent: {opponentScore}";
                }
            }

            if (winnerText != null && isTutorialMode)
            {
                winnerText.text = "Tutorial Complete";
            }

            SaveSparScore scoreSaver = GetComponent<SaveSparScore>();
            if (scoreSaver != null && !isTutorialMode)
            {
                scoreSaver.OnSparCompleted(playerScore, opponentScore);
            }

            Result.SetActive(true);
        }
    }

private void ShowTutorialInstructionWhenReady()
{
    if (sparringPartnerAnimator != null)
    {
        sparringPartnerAnimator.speed = 0f;
        tutorialAnimationPaused = true;
        Debug.Log("Tutorial: Animation paused when player can act");
    }

    
    SetAutoDetectSequences(true);
    StartCoroutine(ForceStanceGuideUpdateWhenActive());

    ShowTutorialInstruction();
}

    private void DisableAllBoxes()
    {
        StanceManager sm = StanceManager.Instance;
        if (sm == null) return;

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



        if (sm.currentAttackSequence != null)
        {
            foreach (var box in sm.currentAttackSequence.sequenceBoxes)
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

            if (sm.currentAttackSequence.endBoxLeft != null)
                sm.currentAttackSequence.endBoxLeft.SetActive(false);

            if (sm.currentAttackSequence.endBoxRight != null)
                sm.currentAttackSequence.endBoxRight.SetActive(false);
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

    public void NotifySequenceCompletion(string stanceName, string sequenceName)
    {
        if (currentPhase == 1)
        {
            NotifyPhase1Completion(stanceName, sequenceName);
        }
        else if (currentPhase == 2)
        {
            OnSequenceCompleted(stanceName, sequenceName);
        }
    }

    public void Restart()
    {
        if (StanceManager.Instance != null)
        {
            StanceManager.Instance.OnStanceChanged -= HandlePhase2StanceChange;
        }

        if (sparManagerUIPanel != null)
        {
            sparManagerUIPanel.SetActive(false);
            Debug.Log("SparManager UI disabled for restart");
        }

        IntroLevel introLevel = FindObjectOfType<IntroLevel>();
        if (introLevel != null)
        {
            isGameActive = false;
            isGameOver = false;
            playerScore = 0;
            opponentScore = 0;
            currentRound = 1;
            currentPhase = 0;

            UpdateScoreDisplay();
            UpdateRoundDisplay();

            gameObject.SetActive(false);

            introLevel.ActivateIntro();
        }
    }

    private void ShowTutorialInstruction()
    {
        if (tutorialInstructionPanel != null && tutorialInstructionTextUI != null)
        {
            tutorialInstructionPanel.SetActive(true);
            tutorialInstructionTextUI.text = tutorialInstructionText;
            tutorialInstructionShown = true;

            Debug.Log("Tutorial instruction shown");
        }
    }

    private void ShowTutorialPhase2Instruction()
    {
        if (tutorialInstructionPanel != null && tutorialInstructionTextUI != null)
        {
            tutorialInstructionPanel.SetActive(true);
            tutorialInstructionTextUI.text = tutorialPhase2Text;

            Debug.Log("Tutorial Phase 2 instruction shown");



        }
    }

    private IEnumerator HideTutorialInstructionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (tutorialInstructionPanel != null)
        {
            tutorialInstructionPanel.SetActive(false);
        }
    }
    private void OnDestroy()
    {
        if (StanceManager.Instance != null)
        {
            StanceManager.Instance.OnStanceChanged -= HandlePhase2StanceChange;
        }
    }
    public void OnAnimationEventObjectiveStart(int objectiveIndex)
    {
        if (!isGameActive || isGameOver || currentPhase != 1) return;

        if (objectiveIndex != currentObjectiveIndex) return;

        canPlayerAct = true;

        
        SetAutoDetectSequences(true);

        
        StartCoroutine(ForceStanceGuideUpdateWhenActive());

        Debug.Log($"Animation Event: Player can now act for objective {objectiveIndex + 1} - StanceGuide activated");

        if (isTutorialMode && !tutorialInstructionShown)
        {
            ShowTutorialInstructionWhenReady();
        }
    }
private IEnumerator ForceStanceGuideUpdateWhenActive()
{
    yield return new WaitForEndOfFrame();
    
    StanceGuide stanceGuide = FindObjectOfType<StanceGuide>();
    if (stanceGuide != null)
    {
        stanceGuide.ResetSequenceDetection();
        Debug.Log("StanceGuide activated and reset for player action window");
    }
}


    public void OnAnimationEventObjectiveEnd(int objectiveIndex)
{
    if (!isGameActive || isGameOver || currentPhase != 1) return;
    
    if (objectiveIndex != currentObjectiveIndex) return;
    
    canPlayerAct = false;
    
    
    SetAutoDetectSequences(false);
    ResetStanceGuide();
    
    if (objectiveActive)
    {
        opponentScore++;
        Debug.Log($"Player was too late for objective {objectiveIndex + 1}. Opponent scores! Opponent Score: {opponentScore}");
        UpdateScoreDisplay();
        
        ShowPhase1Feedback("MISS", missColor);
    }
    
    objectiveActive = false;
    StopTimingBar();
    
    Debug.Log($"Animation Event: Timing window ended for objective {objectiveIndex + 1}");
    
    StartCoroutine(DelayedNextObjective());
}


    private void ShowPhase1Feedback(string feedbackText, Color textColor)
    {
        if (phase1FeedbackPanel != null && phase1FeedbackText != null)
        {
            StartCoroutine(ShowPhase1FeedbackCoroutine(feedbackText, textColor));
        }
    }


    private IEnumerator ShowPhase1FeedbackCoroutine(string feedbackText, Color textColor)
    {
        phase1FeedbackPanel.SetActive(true);
        phase1FeedbackText.text = feedbackText;
        phase1FeedbackText.color = textColor;

        Debug.Log($"Phase 1 Feedback: {feedbackText}");

        yield return new WaitForSeconds(feedbackDisplayDuration);

        phase1FeedbackPanel.SetActive(false);
    }

    private IEnumerator DelayedNextObjective()
    {
        yield return new WaitForSeconds(0.5f);
        NextObjective();
    }

    private void StartTimingBar()
    {
        if (timingBarPanel != null)
        {
            timingBarPanel.SetActive(true);
            timingBarActive = true;

            if (timingBarFill != null)
            {
                timingBarFill.fillAmount = 1f;
                timingBarFill.color = barWhiteColor;
            }

            if (timingBarBackground != null)
            {
                timingBarBackground.color = barWhiteColor;
            }

            Debug.Log("Timing bar started - white (waiting for timing window)");
        }
    }
    private void CheckAttackWindow()
    {
        if (!objectiveWindowActive || currentPhase != 1) return;

        float currentTime = Time.time;

        if (!isInAttackWindow && currentTime >= attackWindowStartTime)
        {
            isInAttackWindow = true;
            Debug.Log("Entered attack window - player can now act without penalty");
        }
    }


    private void StopTimingBar()
    {
        if (timingBarPanel != null)
        {
            timingBarPanel.SetActive(false);
            timingBarActive = false;
            Debug.Log("Timing bar stopped");
        }
    }
        private void ResetStanceGuide()
    {
        StanceGuide stanceGuide = FindObjectOfType<StanceGuide>();
        if (stanceGuide != null)
        {
            
            stanceGuide.ResetSequenceDetection(); 
            stanceGuide.autoDetectSequences = false;
            Debug.Log("StanceGuide reset and disabled");
        }
    }

}