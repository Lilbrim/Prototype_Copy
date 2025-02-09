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

    private AttackSequence currentAttackSequence; 
    private StanceDetector[] allDetectors;

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

        foreach (var box in defaultBoxes) box.SetActive(false);
        foreach (var box in basicStrikeBoxes) box.SetActive(false);
        foreach (var box in redondaBoxes) box.SetActive(false);

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

        currentAttackSequence = null;
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

        switch (currentStance)
        {
            case Stance.BasicStrike:
                foreach (var box in basicStrikeBoxes) box.SetActive(false);
                break;
            case Stance.Redonda:
                foreach (var box in redondaBoxes) box.SetActive(false);
                break;
        }

        foreach (var box in sequence.sequenceBoxes)
        {
            box.SetActive(true);
        }
    }

    private void CheckAttackSequence()
    {
        if (currentAttackSequence != null)
        {
            var currentBox = currentAttackSequence.sequenceBoxes[currentAttackSequence.currentIndex];
            var detector = currentBox.GetComponent<StanceDetector>();

            if (detector.IsLeftHandInStance() || detector.IsRightHandInStance())
            {
                currentAttackSequence.currentIndex++;
                if (currentAttackSequence.currentIndex >= currentAttackSequence.sequenceBoxes.Length)
                {
                    Debug.Log($"{currentStance}.{currentAttackSequence.sequenceName} done.");
                    ResetSequence();
                    SetStance(Stance.Default);
                }
            }
        }
    }

    private void ResetSequence()
    {
        if (currentAttackSequence != null)
        {
            currentAttackSequence.currentIndex = 0;
            currentAttackSequence = null;
        }
    }
}

[System.Serializable]
public class AttackSequence
{
    public string sequenceName; 
    public GameObject startBoxLeft; 
    public GameObject startBoxRight; 
    public GameObject[] sequenceBoxes; 
    [HideInInspector] public int currentIndex = 0; 
}