using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SparManager : MonoBehaviour
{
    public static SparManager Instance;
    private bool isGameActive = true;

    [Header("Phase Settings")]
    public float phase2Duration = 120f; 
    public float phaseTransitionDelay = 2f;
    public float objectiveTimeout = 5f; 

    [Header("UI References")]
    public TextMeshProUGUI phaseText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI playerScoreText;
    public TextMeshProUGUI opponentScoreText;
    public GameObject Result;
    public TextMeshProUGUI winnerText;
    public TextMeshProUGUI finalScoreText;

    [Header("Phase 1 Settings")]
    public List<string> phase1Objectives = new List<string>(); 
    
    [Header("Phase 2 Settings")]
    public List<string> phase2Stances = new List<string>(); 
    
    private int currentPhase = 0;
    private int currentObjectiveIndex = 0;
    private float timer = 0f;
    private int playerScore = 0;
    private int opponentScore = 0;
    private bool objectiveActive = false;
    private bool isGameOver = false;

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
        if (Result != null)
        {
            Result.SetActive(false);
        }
        
        if (StanceManager.Instance != null)
        {
            StanceManager.Instance.useSparManager = true;
        }
        
        UpdateScoreDisplay();
        StartCoroutine(StartPhase1());
    }

    private void Update()
    {
        if (!isGameActive || isGameOver) return;

        if (currentPhase == 2)
        {
            timer -= Time.deltaTime;
            UpdateTimerDisplay();

            if (timer <= 0)
            {
                EndGame();
            }
        }
        else if (currentPhase == 1 && objectiveActive)
        {
            timer -= Time.deltaTime;
            UpdateTimerDisplay();

            if (timer <= 0)
            {
                opponentScore++;
                UpdateScoreDisplay();
                NextObjective();
            }
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
        StartNextObjective();
    }

    private void StartNextObjective()
    {
        if (currentObjectiveIndex < phase1Objectives.Count)
        {
            objectiveActive = true;
            timer = objectiveTimeout;
            UpdateTimerDisplay();
            
            string requiredStance = phase1Objectives[currentObjectiveIndex];
            Debug.Log($"Starting objective {currentObjectiveIndex + 1}: {requiredStance}");
            
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

    private IEnumerator StartPhase2()
    {
        Debug.Log("Transitioning to Phase 2");
        
        StanceManager.Instance.EnterStance("Default");
        
        yield return new WaitForSeconds(phaseTransitionDelay);
        
        if (phaseText != null)
        {
            phaseText.text = "Phase 2";
        }
        
        
        currentPhase = 2;
        timer = phase2Duration;
        UpdateTimerDisplay();
        
        ActivatePhase2Stances();
    }

    private void ActivatePhase2Stances()
    {

    }

    public void OnSequenceCompleted(string stanceName, string sequenceName)
    {
        if (!isGameActive || isGameOver) return;

        if (currentPhase == 1 && objectiveActive)
        {
            string currentObjective = phase1Objectives[currentObjectiveIndex];
            
            if (stanceName == currentObjective)
            {
                playerScore++;
                UpdateScoreDisplay();
                NextObjective();
            }
        }
        else if (currentPhase == 2)
        {
            playerScore++;
            UpdateScoreDisplay();
        }
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
            int minutes = Mathf.FloorToInt(timer / 60);
            int seconds = Mathf.FloorToInt(timer % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    public void SetGameActive(bool active)
    {
        isGameActive = active;
    }

    private void EndGame()
    {
        isGameOver = true;
        
        
        if (Result != null)
        {
            Result.SetActive(true);
            
            if (winnerText != null)
            {
                if (playerScore > opponentScore)
                {
                    winnerText.text = "You Win!";
                }
                else if (opponentScore > playerScore)
                {
                    winnerText.text = "Opponent Wins!";
                }
                else
                {
                    winnerText.text = "It's a Tie!";
                }
            }
            
            if (finalScoreText != null)
            {
                finalScoreText.text = $"Player: {playerScore} - Opponent: {opponentScore}";
            }
        }
        
        StanceManager.Instance.EnterStance("Default");
    }

    public void NotifySequenceCompletion(string stanceName, string sequenceName)
    {
        OnSequenceCompleted(stanceName, sequenceName);
    }
    
    public void RestartGame()
    {

    }
}