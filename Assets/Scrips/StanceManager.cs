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

    public void EnterStance(string stanceName)
    {
        if (stanceName == "BasicStrike" && currentStance != Stance.BasicStrike)
        {
            SetStance(Stance.BasicStrike);
        }
        else if (stanceName == "Redonda" && currentStance != Stance.Redonda)
        {
            SetStance(Stance.Redonda);
        }
    }

    private void SetStance(Stance newStance)
    {
        currentStance = newStance;
        timer = stanceTimeout;

        foreach (var detector in allDetectors)
        {
            detector.ResetStance();
        }

        // Set all boxes invisible and non-interactable first
        SetBoxesVisibleAndInteractable(defaultBoxes, false);
        SetBoxesVisibleAndInteractable(basicStrikeBoxes, false);
        SetBoxesVisibleAndInteractable(redondaBoxes, false);

        // Then set the appropriate boxes visible and interactable
        switch (currentStance)
        {
            case Stance.Default:
                SetBoxesVisibleAndInteractable(defaultBoxes, true);
                break;
            case Stance.BasicStrike:
                SetBoxesVisibleAndInteractable(basicStrikeBoxes, true);
                LevelManager.Instance.OnStanceEntered();
                break;
            case Stance.Redonda:
                SetBoxesVisibleAndInteractable(redondaBoxes, true);
                LevelManager.Instance.OnStanceEntered();
                break;
        }

        currentAttackSequence = null;
        sequenceCounter = 0;
        totalBoxesTouched = 0; // Reset total boxes touched
    }

    private void SetBoxesVisibleAndInteractable(GameObject[] boxes, bool state)
    {
        foreach (var box in boxes)
        {
            StanceDetector detector = box.GetComponent<StanceDetector>();
            if (detector != null)
            {
                detector.SetVisibleAndInteractable(state);
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

        foreach (var box in sequence.sequenceBoxes)
        {
            StanceDetector detector = box.GetComponent<StanceDetector>();
            if (detector != null)
            {
                detector.SetVisibleAndInteractable(true);
            }
        }
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
                    
                    // If this is the final box in the sequence, end the sequence
                    if (i == currentAttackSequence.sequenceBoxes.Length - 1)
                    {
                        Debug.Log($"{currentStance}.{currentAttackSequence.sequenceName} done. Boxes triggered: {totalBoxesTouched} out of {currentAttackSequence.sequenceBoxes.Length}");
                        NotifyObjectiveCompletion();
                        ResetSequence();
                        SetStance(Stance.Default);
                        return;
                    }
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
                detector.SetVisibleAndInteractable(false);
            }

            currentAttackSequence = null;
            sequenceCounter = 0;
        }
    }
    
    public void NotifyObjectiveCompletion()
    {
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
    public float timeLimit;
    [HideInInspector] public int currentIndex = 0; 
}