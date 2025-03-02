using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StanceManager : MonoBehaviour
{
    public static StanceManager Instance;

    public enum Stance { Default, BasicStrike, Redonda }
    public Stance currentStance = Stance.Default;
    public GameObject[] defaultBoxes;
    public GameObject[] basicStrikeBoxes;
    public GameObject[] redondaBoxes;

    public List<AttackSequence> basicStrikeSequences = new List<AttackSequence>();
    public List<AttackSequence> redondaSequences = new List<AttackSequence>();

    public float stanceTimeout = 2f;
    private float timer;

    public AttackSequence currentAttackSequence; 
    private StanceDetector[] allDetectors;
    public int sequenceCounter; 
    public int totalBoxesTouched; // New variable to track total boxes touched
    
    private bool isPracticeMode = false;
    private string requiredStanceForPractice = "";

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
        allDetectors = FindObjectsOfType<StanceDetector>();
        SetStance(Stance.Default);
    }

    private void Update()
    {
        if (currentStance != Stance.Default && currentAttackSequence == null)
        {
            CheckForSequenceStart();
        }

        if (currentAttackSequence != null)
        {
            CheckAttackSequence();

            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                ResetSequence();
                SetStance(Stance.Default);
            }
        }
        else if (currentStance != Stance.Default)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                SetStance(Stance.Default);
            }
        }
    }

    public void EnterStance(string stanceName, bool practiceMode = false)
    {
        // Store practice mode and required stance
        isPracticeMode = practiceMode;
        requiredStanceForPractice = stanceName;
        
        // If we're already in the default stance and in practice mode,
        // we DON'T want to re-enter the stance, just update the boxes
        if (currentStance == Stance.Default && practiceMode)
        {
            // Just update the boxes to show the entry points
            ActivateBoxesForPracticeMode(Stance.Default);
            return;
        }

        // Process the stance entry based on current stance and requested stance
        if (stanceName == "BasicStrike" && currentStance != Stance.BasicStrike)
        {
            // Only allow entry if we're in the default stance first
            if (currentStance == Stance.Default)
            {
                SetStance(Stance.BasicStrike);
                LevelManager.Instance.OnStanceEntered("BasicStrike");
            }
        }
        else if (stanceName == "Redonda" && currentStance != Stance.Redonda)
        {
            // Only allow entry if we're in the default stance first
            if (currentStance == Stance.Default)
            {
                SetStance(Stance.Redonda);
                LevelManager.Instance.OnStanceEntered("Redonda");
            }
        }
        else
        {
            // Incorrect stance entered
            LevelManager.Instance.OnStanceEntered("Incorrect");
        }
    }

    private void SetStance(Stance newStance)
    {
        currentStance = newStance;
        timer = stanceTimeout;

        // Reset all detectors BEFORE disabling any boxes
        foreach (var detector in allDetectors)
        {
            detector.ResetStance();
        }

        // Force clear trigger states before deactivating boxes
        ForceResetTriggerStates(defaultBoxes);
        ForceResetTriggerStates(basicStrikeBoxes);
        ForceResetTriggerStates(redondaBoxes);

        // Now deactivate all boxes
        foreach (var box in defaultBoxes) box.SetActive(false);
        foreach (var box in basicStrikeBoxes) box.SetActive(false);
        foreach (var box in redondaBoxes) box.SetActive(false);

        // In practice mode, show only the necessary boxes for the next sequence
        if (isPracticeMode)
        {
            ActivateBoxesForPracticeMode(newStance);
        }
        else
        {
            // Regular mode: Activate the appropriate boxes for the new stance
            switch (currentStance)
            {
                case Stance.Default:
                    foreach (var box in defaultBoxes) box.SetActive(true);
                    break;
                case Stance.BasicStrike:
                    foreach (var box in basicStrikeBoxes) box.SetActive(true);
                    break;
                case Stance.Redonda:
                    foreach (var box in redondaBoxes) box.SetActive(true);
                    break;
            }
        }

        currentAttackSequence = null;
        sequenceCounter = 0;
        totalBoxesTouched = 0; // Reset total boxes touched
    }

    private void ActivateBoxesForPracticeMode(Stance newStance)
    {
        if (newStance == Stance.Default)
        {
            // Deactivate ALL default boxes first
            foreach (var box in defaultBoxes) box.SetActive(false);

            // Find the target sequences for the required stance
            List<AttackSequence> targetSequences = new List<AttackSequence>();
            
            if (requiredStanceForPractice == "BasicStrike")
            {
                targetSequences = basicStrikeSequences;
            }
            else if (requiredStanceForPractice == "Redonda")
            {
                targetSequences = redondaSequences;
            }

            // Activate ONLY the start boxes for the required stance's sequences
            foreach (var sequence in targetSequences)
            {
                if (sequence.startBoxLeft != null)
                {
                    sequence.startBoxLeft.SetActive(true);
                    Debug.Log($"Activated startBoxLeft for sequence: {sequence.sequenceName}");
                }
                if (sequence.startBoxRight != null)
                {
                    sequence.startBoxRight.SetActive(true);
                    Debug.Log($"Activated startBoxRight for sequence: {sequence.sequenceName}");
                }
            }
        }
        else
        {
            // Once we're in a stance other than Default,
            // show the appropriate stance boxes
            switch (newStance)
            {
                case Stance.BasicStrike:
                    foreach (var box in basicStrikeBoxes) box.SetActive(true);
                    break;
                case Stance.Redonda:
                    foreach (var box in redondaBoxes) box.SetActive(true);
                    break;
            }
        }
    }

    // New method to force reset trigger states
    private void ForceResetTriggerStates(GameObject[] boxes)
    {
        foreach (var box in boxes)
        {
            StanceDetector detector = box.GetComponent<StanceDetector>();
            if (detector != null)
            {
                detector.ForceResetTriggerState();
            }

            // If boxes have child colliders, reset those too
            Collider[] colliders = box.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                bool wasEnabled = col.enabled;
                col.enabled = false;
                col.enabled = wasEnabled;
            }
        }
    }

    private void CheckForSequenceStart()
    {
        List<AttackSequence> sequences = GetSequencesForCurrentStance();

        foreach (var sequence in sequences)
        {
            if (IsSequenceStartConditionMet(sequence))
            {
                StartAttackSequence(sequence);
                break; 
            }
        }
    }

    private List<AttackSequence> GetSequencesForCurrentStance()
    {
        switch (currentStance)
        {
            case Stance.BasicStrike:
                return basicStrikeSequences;
            case Stance.Redonda:
                return redondaSequences;
            default:
                return new List<AttackSequence>();
        }
    }

    private bool IsSequenceStartConditionMet(AttackSequence sequence)
    {
        if (sequence.startBoxLeft != null && sequence.startBoxRight != null)
        {
            var leftDetector = sequence.startBoxLeft.GetComponent<StanceDetector>();
            var rightDetector = sequence.startBoxRight.GetComponent<StanceDetector>();

            return leftDetector.IsLeftHandInStance() && rightDetector.IsRightHandInStance();
        }
        return false;
    }

    private void StartAttackSequence(AttackSequence sequence)
    {
        currentAttackSequence = sequence;
        timer = stanceTimeout;
        totalBoxesTouched = 0; // Reset total boxes touched counter

        // Force reset trigger states before activating sequence boxes
        ForceResetTriggerStates(currentAttackSequence.sequenceBoxes);

        // Deactivate all existing boxes first
        foreach (var box in defaultBoxes) box.SetActive(false);
        foreach (var box in basicStrikeBoxes) box.SetActive(false);
        foreach (var box in redondaBoxes) box.SetActive(false);

        // Activate only the sequence boxes
        foreach (var box in sequence.sequenceBoxes)
        {
            box.SetActive(true);
        }

        // Also activate end boxes if they exist
        if (sequence.endBoxLeft != null) sequence.endBoxLeft.SetActive(true);
        if (sequence.endBoxRight != null) sequence.endBoxRight.SetActive(true);
    }

    private void CheckAttackSequence()
    {
        if (currentAttackSequence != null)
        {
            // Process all boxes to record which ones are touched
            for (int i = 0; i < currentAttackSequence.sequenceBoxes.Length; i++)
            {
                var box = currentAttackSequence.sequenceBoxes[i];
                var detector = box.GetComponent<StanceDetector>();
                
                if ((detector.IsLeftHandInStance() || detector.IsRightHandInStance()) && !detector.IsCompleted)
                {
                    detector.IsCompleted = true;
                    sequenceCounter++;
                    totalBoxesTouched++; // Increment the total boxes touched
                    Debug.Log($"Box {box.name} completed. Total completed: {sequenceCounter}");
                }
            }
            
            // Check if both end boxes are triggered to complete the sequence
            if (currentAttackSequence.endBoxLeft != null && currentAttackSequence.endBoxRight != null)
            {
                var leftEndDetector = currentAttackSequence.endBoxLeft.GetComponent<StanceDetector>();
                var rightEndDetector = currentAttackSequence.endBoxRight.GetComponent<StanceDetector>();
                
                if (leftEndDetector.IsLeftHandInStance() && rightEndDetector.IsRightHandInStance())
                {
                    Debug.Log($"{currentStance}.{currentAttackSequence.sequenceName} done. Boxes triggered: {totalBoxesTouched} out of {currentAttackSequence.sequenceBoxes.Length}");
                    
                    // Only check sequence correctness after completion
                    NotifyObjectiveCompletion();
                    
                    // Reset sequence and return to default stance
                    ResetSequence();
                    SetStance(Stance.Default);
                    return;
                }
            }
        }
    }

    private void ResetSequence()
    {
        if (currentAttackSequence != null)
        {
            foreach (var box in currentAttackSequence.sequenceBoxes)
            {
                var detector = box.GetComponent<StanceDetector>();
                detector.IsCompleted = false; 
            }

            currentAttackSequence = null;
            sequenceCounter = 0;
        }
        SetStance(Stance.Default);
    }
        
    public void NotifyObjectiveCompletion()
    {
        int touchedBoxes = totalBoxesTouched;
        
        LevelManager.Instance.EndObjective();
    }
}

[System.Serializable]
public class AttackSequence
{
    public string sequenceName; 
    public GameObject startBoxLeft; 
    public GameObject startBoxRight; 
    public GameObject[] sequenceBoxes;
    public GameObject endBoxLeft; 
    public GameObject endBoxRight;
    public float timeLimit;
    [HideInInspector] public int currentIndex = 0; 
}