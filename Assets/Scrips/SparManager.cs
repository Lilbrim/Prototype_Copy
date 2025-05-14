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

    [Header("Game Speed ")]
    [Range(0.1f, 2.0f)]
    public float gameSpeed = 1.0f; 
    
    [Header("UI References")]
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
    public string sparringPartnerAnimation = "Fighting";
    public bool useSparringPartner = false;
    private GameObject sparringPartner;
    private Animator sparringPartnerAnimator;

    [field: Header("Phase 1 Settings")]
    [System.Serializable]
    public class PhaseObjective
    {
        public string stanceName;
        public float timeout;
        [Range(0.1f, 3.0f)]
        public float animationSpeed = 1f; 
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
    
    // Reference to the results manager
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
        
        // Find the results manager if we're using it
        if (useResultsManager)
        {
            resultsManager = FindObjectOfType<SparResultsManager>();
            if (resultsManager == null)
            {
                Debug.LogWarning("SparResultsManager not found but useResultsManager is true");
            }
        }
    }

    // Method to receive the sparring partner from IntroLevel
    public void SetSparringPartner(GameObject partner)
    {
        sparringPartner = partner;
        if (sparringPartner != null)
        {
            useSparringPartner = true;
            sparringPartnerAnimator = sparringPartner.GetComponent<Animator>();
            if (sparringPartnerAnimator != null)
            {
                // Keep the current animation running until we need to change it
                Debug.Log("Sparring partner animator found");
            }
            else
            {
                Debug.LogWarning("Sparring partner does not have an Animator component");
            }
        }
    }

    public void StartLevel()
    {
        isGameActive = true;
        isGameOver = false;
        playerScore = 0;
        opponentScore = 0;
        currentRound = 1;
        
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

        if (useSparringPartner && sparringPartnerAnimator != null && !string.IsNullOrEmpty(sparringPartnerAnimation))
        {
            sparringPartnerAnimator.Play(sparringPartnerAnimation);
            sparringPartnerAnimator.speed = 1f; // Default speed
        }
    }

    private void Update()
    {
        if (!isGameActive || isGameOver) return;

        if (Time.timeScale != gameSpeed)
        {
            Time.timeScale = gameSpeed;
        }

        roundTimer -= Time.deltaTime;
        UpdateTimerDisplay();

        if (roundTimer <= 0)
        {
            EndRound();
            return;
        }

        if (currentPhase == 2)
        {
            phase2Value -= Mathf.RoundToInt(Time.deltaTime);
            
            blockChanceResetTimer -= Time.deltaTime;
            if (blockChanceResetTimer <= 0)
            {
                ResetBlockChance();
                blockChanceResetTimer = blockChanceResetTime;
            }

            if (phase2Value <= 0)
            {
                Debug.Log("Phase 2 value reached zero, returning to Phase 1");
                // Clean up Phase 2 before starting Phase 1
                CleanupPhase2();
                StartCoroutine(StartPhase1());
            }
        }
        else if (currentPhase == 1 && objectiveActive)
        {
            timer -= Time.deltaTime;
            
            if (timer <= 0)
            {
                opponentScore++;
                UpdateScoreDisplay();
                Debug.Log($"Time expired for Phase 1 objective {currentObjectiveIndex + 1}. Opponent score: {opponentScore}");
                
                NextObjective();
            }
        }
    }

    private void EndRound()
    {
        currentRound++;
        Debug.Log($"Round {currentRound-1} ended.");
        
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

        if (useSparringPartner && sparringPartnerAnimator != null && !string.IsNullOrEmpty(sparringPartnerAnimation))
        {
            sparringPartnerAnimator.Rebind();
            sparringPartnerAnimator.Update(0f);
            
            sparringPartnerAnimator.Play(sparringPartnerAnimation);
            sparringPartnerAnimator.speed = 1f;
            
            Debug.Log("Resetting Phase 1");
        }

        StartNextObjective();
    }

    private void StartNextObjective()
    {
        if (currentObjectiveIndex < phase1Objectives.Count)
        {
            objectiveActive = true;
            
            float objectiveTimeout = phase1Objectives[currentObjectiveIndex].timeout > 0 ? 
                                     phase1Objectives[currentObjectiveIndex].timeout : 
                                     defaultObjectiveTimeout;
            
            timer = objectiveTimeout;
            
            string requiredStance = phase1Objectives[currentObjectiveIndex].stanceName;
            Debug.Log($"Starting objective {currentObjectiveIndex + 1}: {requiredStance} with timeout: {objectiveTimeout}s");
            
            if (useSparringPartner && sparringPartnerAnimator != null)
            {
                float animSpeed = phase1Objectives[currentObjectiveIndex].animationSpeed;
                sparringPartnerAnimator.speed = animSpeed;
                Debug.Log($"animation speed {animSpeed}");
            }
            
            StanceManager.Instance.EnterStance(requiredStance);
        }
        else
        {
            StartCoroutine(StartPhase2());
        }
    }

    private void NextObjective()
    {
        objectiveActive = false;
        currentObjectiveIndex++;
        StartNextObjective();
    }
    
    public void NotifyPhase1Completion(string stanceName, string sequenceName)
    {
        if (!isGameActive || isGameOver || currentPhase != 1 || !objectiveActive) return;
        
        string currentObjective = phase1Objectives[currentObjectiveIndex].stanceName;
        
        if (stanceName == currentObjective)
        {
            Debug.Log($"Phase 1 objective {currentObjectiveIndex + 1} completed: {stanceName}.{sequenceName}");
            NextObjective();
        }
    }

    private IEnumerator StartPhase2()
    {
        Debug.Log("Transitioning to Phase 2");
        
        StanceManager.Instance.EnterStance("Default");
        objectiveActive = false;
        
        yield return new WaitForSeconds(phaseTransitionDelay);
        
        if (phaseText != null)
        {
            phaseText.text = "Phase 2";
        }
        
        currentPhase = 2;

        if (useSparringPartner && sparringPartnerAnimator != null)
        {
            sparringPartnerAnimator.speed = 1f;
            sparringPartnerAnimator.Play("Idle");

        }

        ResetBlockChance();
        stanceUseCount.Clear();
        blockChanceResetTimer = blockChanceResetTime;
        
        ActivatePhase2Stances();
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
            bool blocked = CheckIfOpponentBlocks(stanceName);
            
            if (blocked)
            {
                ShowBlockFeedback();
                Debug.Log($"Opponent blocked {stanceName}.{sequenceName} in Phase 2!");
                phase2Value -= phase2BlockReduction;
                phase2Value = Mathf.Max(0, phase2Value);
            }
            else
            {
                playerScore++;
                Debug.Log($"Player completed {stanceName}.{sequenceName} in Phase 2! Score: {playerScore}");
                UpdateScoreDisplay();
                
                phase2Value -= phase2SequenceReduction;
                phase2Value = Mathf.Max(0, phase2Value);
            }
            
            IncreaseBlockChance(stanceName);
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
        
        // Clean up any active Phase 2 stances
        if (StanceManager.Instance != null)
        {
            StanceManager.Instance.OnStanceChanged -= HandlePhase2StanceChange;
        }
        
        // Reset to default stance
        StanceManager.Instance.EnterStance("Default");
        
        // Remove sparring partner if one exists
        if (useSparringPartner && sparringPartner != null)
        {
            sparringPartner.SetActive(false);
        }
        
        // Disable all stance boxes similar to LevelManager
        DisableAllBoxes();
        
        // Disable the StanceManager
        if (StanceManager.Instance != null)
        {
            StanceManager.Instance.SetGameActive(false);
            StanceManager.Instance.enabled = false;
        }
        
        if (useResultsManager && resultsManager != null)
        {
            resultsManager.ShowSparResults(playerScore, opponentScore, totalRounds);
        }
        else if (Result != null)
        {        
            if (finalScoreText != null)
            {
                finalScoreText.text = $"Player: {playerScore} - Opponent: {opponentScore}";
            }
            
            SaveSparScore scoreSaver = GetComponent<SaveSparScore>();
            if (scoreSaver != null)
            {
                scoreSaver.OnSparCompleted(playerScore, opponentScore);
            }
        }
    }
        private void DisableAllBoxes()
    {
        StanceManager sm = StanceManager.Instance;
        if (sm == null) return;
        
        // Disable default boxes
        if (sm.defaultBoxes != null)
        {
            foreach (var box in sm.defaultBoxes)
            {
                if (box != null)
                    box.SetActive(false);
            }
        }
        
        // Disable all style-specific boxes
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

        
        
        // If there's a current attack sequence, disable those boxes too
        if (sm.currentAttackSequence != null)
        {
            foreach (var box in sm.currentAttackSequence.sequenceBoxes)
            {
                if (box != null)
                {
                    box.SetActive(false);
                    
                    // Reset any StanceDetector components
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
    
    private void OnDestroy()
    {
        if (StanceManager.Instance != null)
        {
            StanceManager.Instance.OnStanceChanged -= HandlePhase2StanceChange;
        }
    }
}