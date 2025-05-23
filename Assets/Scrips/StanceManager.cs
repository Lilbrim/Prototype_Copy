using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StanceManager : MonoBehaviour
{
    public static StanceManager Instance;
    private bool isGameActive = true;

    [System.Serializable]
    public class ArnisStyle
    {
        public string styleName;
        public GameObject[] stanceBoxes;
        public List<AttackSequence> sequences = new List<AttackSequence>();
    }

    public List<ArnisStyle> arnisStyles = new List<ArnisStyle>();
    public GameObject[] defaultBoxes;

    public float stanceTimeout = 2f;
    private float timer;

    public AttackSequence currentAttackSequence;
    private StanceDetector[] allDetectors;
    public int sequenceCounter;
    public int totalBoxesTouched;

    private bool isPracticeMode = false;
    private string requiredStanceForPractice = "";

    private string currentStance = "Default";
    private ArnisStyle currentArnisStyle;

    [Header("Manager Settings")]
    public bool useSparManager = false;
    public bool useTutorialManager = false;

    public delegate void StanceChangedDelegate(string newStance);
    public event StanceChangedDelegate OnStanceChanged;

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

        foreach (var box in defaultBoxes) box.SetActive(false);

        foreach (var style in arnisStyles)
        {
            foreach (var box in style.stanceBoxes) box.SetActive(false);
        }

        AssignSequencePositions();

        currentStance = "Default";
    }

    private void AssignSequencePositions()
    {
        foreach (var style in arnisStyles)
        {
            foreach (var sequence in style.sequences)
            {
                for (int i = 0; i < sequence.sequenceBoxes.Length; i++)
                {
                    StanceDetector detector = sequence.sequenceBoxes[i].GetComponent<StanceDetector>();
                    if (detector != null)
                    {
                        detector.isPartOfSequence = true;
                        detector.sequencePosition = i;
                    }
                }

                SetupSpecialBox(sequence.startBoxLeft, "Left Baton", false, 0);
                SetupSpecialBox(sequence.startBoxRight, "Right Baton", false, 0);
                SetupSpecialBox(sequence.endBoxLeft, "Left Baton", false, 1);
                SetupSpecialBox(sequence.endBoxRight, "Right Baton", false, 1);

                UpdateSequenceColorsForSequence(sequence);
            }
        }
    }

    private void SetupSpecialBox(GameObject box, string batonTag, bool isSequenceBox, int position)
    {
        if (box == null) return;

        StanceDetector detector = box.GetComponent<StanceDetector>();
        if (detector != null)
        {
            if (!string.IsNullOrEmpty(batonTag) && string.IsNullOrEmpty(box.tag))
            {
                box.tag = batonTag;
            }

            detector.isPartOfSequence = isSequenceBox;
            detector.sequencePosition = position;
        }
    }

    private void Update()
    {
        if (currentStance != "Default" && currentAttackSequence == null)
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
                SetStance("Default");
            }
        }
        else if (currentStance != "Default")
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                SetStance("Default");
            }
        }
    }

    public void SetGameActive(bool active)
    {
        isGameActive = active;
    }

    public void ActivateDefaultStance()
    {
        if (!isGameActive) return;

        foreach (var box in defaultBoxes) box.SetActive(true);
    }

    public void EnterStance(string stanceName, bool practiceMode = false)
    {
        if (!isGameActive) return;

        isPracticeMode = practiceMode;
        requiredStanceForPractice = stanceName;

        if (currentStance == "Default" && practiceMode)
        {
            ActivateBoxesForPracticeMode("Default");
            return;
        }

        if (stanceName != currentStance && currentStance == "Default")
        {
            bool validStance = false;
            foreach (var style in arnisStyles)
            {
                if (style.styleName == stanceName)
                {
                    validStance = true;
                    break;
                }
            }

            if (validStance)
            {
                SetStance(stanceName);

                if (useSparManager && SparManager.Instance != null)
                {
                }
                else if (useTutorialManager && TutorialLevelManager.Instance != null)
                {
                    TutorialLevelManager.Instance.OnStanceEntered(stanceName);
                }
                else if (LevelManager.Instance != null)
                {
                    LevelManager.Instance.OnStanceEntered(stanceName);
                }
            }
            else
            {
                if (useTutorialManager && TutorialLevelManager.Instance != null)
                {
                    TutorialLevelManager.Instance.OnStanceEntered("Incorrect");
                }
                else if (LevelManager.Instance != null && !useSparManager)
                {
                    LevelManager.Instance.OnStanceEntered("Incorrect");
                }
            }
        }
        else
        {
            if (useTutorialManager && TutorialLevelManager.Instance != null)
            {
                TutorialLevelManager.Instance.OnStanceEntered("Incorrect");
            }
            else if (LevelManager.Instance != null && !useSparManager)
            {
                LevelManager.Instance.OnStanceEntered("Incorrect");
            }
        }
    }

    private void SetStance(string newStance)
    {

        timer = stanceTimeout;
        currentArnisStyle = null;
        OnStanceChanged?.Invoke(newStance);

        foreach (var detector in allDetectors)
        {
            detector.ResetStance();
        }

        ForceResetTriggerStates(defaultBoxes);
        foreach (var box in defaultBoxes) box.SetActive(false);

        foreach (var style in arnisStyles)
        {
            ForceResetTriggerStates(style.stanceBoxes);
            foreach (var box in style.stanceBoxes) box.SetActive(false);
        }

        if (newStance == "Default")
        {
            if (isPracticeMode)
            {
                ActivateBoxesForPracticeMode(newStance);
            }
            else
            {

            }
        }
        else
        {
            foreach (var style in arnisStyles)
            {
                if (style.styleName == newStance)
                {
                    currentArnisStyle = style;
                    if (isPracticeMode)
                    {
                        ActivateBoxesForPracticeMode(newStance);
                    }
                    else
                    {
                        foreach (var box in style.stanceBoxes) box.SetActive(true);
                    }
                    break;
                }
            }
        }

        currentAttackSequence = null;
        sequenceCounter = 0;
        totalBoxesTouched = 0;
        currentStance = newStance; 
    }

    public void ClearAllStances()
    {
        Debug.Log("Clearing all stances and sequence boxes");

        foreach (var detector in allDetectors)
        {
            if (detector != null)
            {
                detector.ResetStance();
                detector.ForceResetTriggerState();
            }
        }

        foreach (var box in defaultBoxes)
        {
            if (box != null)
            {
                box.SetActive(false);
            }
        }

        foreach (var style in arnisStyles)
        {
            foreach (var box in style.stanceBoxes)
            {
                if (box != null)
                {
                    box.SetActive(false);
                }
            }

            foreach (var sequence in style.sequences)
            {
                foreach (var box in sequence.sequenceBoxes)
                {
                    if (box != null)
                    {
                        box.SetActive(false);
                    }
                }

                if (sequence.startBoxLeft != null) sequence.startBoxLeft.SetActive(false);
                if (sequence.startBoxRight != null) sequence.startBoxRight.SetActive(false);
                if (sequence.endBoxLeft != null) sequence.endBoxLeft.SetActive(false);
                if (sequence.endBoxRight != null) sequence.endBoxRight.SetActive(false);
            }
        }

        if (currentAttackSequence != null)
        {
            foreach (var box in currentAttackSequence.sequenceBoxes)
            {
                if (box != null)
                {
                    var detector = box.GetComponent<StanceDetector>();
                    if (detector != null)
                    {
                        detector.IsCompleted = false;
                    }
                    box.SetActive(false);
                }
            }
        }

        currentAttackSequence = null;
        sequenceCounter = 0;
        totalBoxesTouched = 0;
        currentStance = "Default";
        currentArnisStyle = null;

        System.Delegate[] delegates = OnStanceChanged?.GetInvocationList();
        if (delegates != null)
        {
            foreach (var del in delegates)
            {
                OnStanceChanged -= (StanceChangedDelegate)del;
            }
        }
    }

    private void ActivateBoxesForPracticeMode(string newStance)
    {
        if (!isGameActive) return;

        if (newStance == "Default")
        {
            foreach (var box in defaultBoxes) box.SetActive(false);

            List<AttackSequence> targetSequences = new List<AttackSequence>();

            foreach (var style in arnisStyles)
            {
                if (style.styleName == requiredStanceForPractice)
                {
                    targetSequences = style.sequences;
                    break;
                }
            }

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
            foreach (var style in arnisStyles)
            {
                if (style.styleName == newStance)
                {
                    foreach (var box in style.stanceBoxes) box.SetActive(true);
                    break;
                }
            }
        }
    }

    private void ForceResetTriggerStates(GameObject[] boxes)
    {
        foreach (var box in boxes)
        {
            StanceDetector detector = box.GetComponent<StanceDetector>();
            if (detector != null)
            {
                detector.ForceResetTriggerState();
            }

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
        if (currentArnisStyle == null) return;

        foreach (var sequence in currentArnisStyle.sequences)
        {
            if (IsSequenceStartConditionMet(sequence))
            {
                StartAttackSequence(sequence);
                break;
            }
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
        totalBoxesTouched = 0;

        ForceResetTriggerStates(currentAttackSequence.sequenceBoxes);

        foreach (var box in defaultBoxes) box.SetActive(false);

        foreach (var style in arnisStyles)
        {
            foreach (var box in style.stanceBoxes) box.SetActive(false);
        }

        foreach (var box in sequence.sequenceBoxes)
        {
            box.SetActive(true);
        }

        if (sequence.endBoxLeft != null) sequence.endBoxLeft.SetActive(true);
        if (sequence.endBoxRight != null) sequence.endBoxRight.SetActive(true);

        UpdateSequenceColors();
    }

    private void CheckAttackSequence()
    {
        if (currentAttackSequence != null)
        {
            for (int i = 0; i < currentAttackSequence.sequenceBoxes.Length; i++)
            {
                var box = currentAttackSequence.sequenceBoxes[i];
                var detector = box.GetComponent<StanceDetector>();

                if ((detector.IsLeftHandInStance() || detector.IsRightHandInStance()) && !detector.IsCompleted)
                {
                    detector.IsCompleted = true;
                    sequenceCounter++;
                    totalBoxesTouched++;
                    Debug.Log($"Box {box.name} completed. Total completed: {sequenceCounter}");
                }
            }

            if (currentAttackSequence.endBoxLeft != null && currentAttackSequence.endBoxRight != null)
            {
                var leftEndDetector = currentAttackSequence.endBoxLeft.GetComponent<StanceDetector>();
                var rightEndDetector = currentAttackSequence.endBoxRight.GetComponent<StanceDetector>();

                if (leftEndDetector.IsLeftHandInStance() && rightEndDetector.IsRightHandInStance())
                {
                    Debug.Log($"{currentStance}.{currentAttackSequence.sequenceName} done. Boxes triggered: {totalBoxesTouched} out of {currentAttackSequence.sequenceBoxes.Length}");

                    NotifyObjectiveCompletion();

                    ResetSequence();
                    SetStance("Default");
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
        SetStance("Default");
    }

    public void NotifyObjectiveCompletion()
    {
        int touchedBoxes = totalBoxesTouched;
        int sequenceBoxCount = currentAttackSequence != null ? currentAttackSequence.sequenceBoxes.Length : 0;

        if (useSparManager && SparManager.Instance != null)
        {
            SparManager.Instance.NotifySequenceCompletion(currentStance, currentAttackSequence.sequenceName);
        }
        else if (useTutorialManager && TutorialLevelManager.Instance != null)
        {
            if (AccuracyTracker.Instance != null)
            {
                AccuracyTracker.Instance.RecordSequenceData(sequenceBoxCount, touchedBoxes);
            }
            TutorialLevelManager.Instance.EndObjective();
        }
        else
        {
            if (AccuracyTracker.Instance != null)
            {
                AccuracyTracker.Instance.RecordSequenceData(sequenceBoxCount, touchedBoxes);
            }

            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.EndObjective();
            }
        }
    }

    public void ActivatePhase2Stances(List<string> availableStances)
    {
        Debug.Log("Activating Phase 2 stances");

        foreach (var box in defaultBoxes) box.SetActive(false);

        foreach (var style in arnisStyles)
        {
            foreach (var box in style.stanceBoxes) box.SetActive(false);
        }

        foreach (var stanceName in availableStances)
        {
            foreach (var style in arnisStyles)
            {
                if (style.styleName == stanceName)
                {
                    foreach (var sequence in style.sequences)
                    {
                        if (sequence.startBoxLeft != null)
                        {
                            sequence.startBoxLeft.SetActive(true);
                            Debug.Log($"Activated start box left for {stanceName}.{sequence.sequenceName}");
                        }
                        if (sequence.startBoxRight != null)
                        {
                            sequence.startBoxRight.SetActive(true);
                            Debug.Log($"Activated start box right for {stanceName}.{sequence.sequenceName}");
                        }
                    }
                    break;
                }
            }
        }

    }
    private void UpdateSequenceColorsForSequence(AttackSequence sequence)
    {
        if (sequence == null) return;
        
        int totalBoxes = sequence.sequenceBoxes.Length;
        
        for (int i = 0; i < totalBoxes; i++)
        {
            StanceDetector detector = sequence.sequenceBoxes[i].GetComponent<StanceDetector>();
            if (detector != null)
            {
                detector.UpdateColorForSequence(totalBoxes);
            }
        }
    }
    
    public void UpdateSequenceColors()
    {
        if (currentAttackSequence != null)
        {
            UpdateSequenceColorsForSequence(currentAttackSequence);
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
    public GameObject endBoxLeft; 
    public GameObject endBoxRight;
    public float timeLimit;
    [HideInInspector] public int currentIndex = 0; 
}