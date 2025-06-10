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

    [Header("Mirroring Settings")]
    public bool isRightHandDominant = false;
    public Transform mirrorPlane;
    public Vector3 mirrorNormal = Vector3.right;

    private Dictionary<Transform, Vector3> originalPositions = new Dictionary<Transform, Vector3>();
    private Dictionary<Transform, Quaternion> originalRotations = new Dictionary<Transform, Quaternion>();
    private Dictionary<Transform, Vector3> originalScales = new Dictionary<Transform, Vector3>();
    
    private Transform mirroredObjectsParent;
    private const string MIRRORED_PARENT_NAME = "MirroredObjects";

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

    [Header("Intro Level Integration")]
    public GameObject[] introStanceBoxes;

    [Header("Manager Settings")]
    public bool useSparManager = false;
    public bool useTutorialManager = false;

    public delegate void StanceChangedDelegate(string newStance);
    public event StanceChangedDelegate OnStanceChanged;

    [Header("Hand-to-Baton Transform Settings")]
    public bool enableHandToBatonTransform = true;
    public GameObject leftHandBaton;  
    public GameObject rightHandBaton; 
    public Transform leftHandTransform;
    public Transform rightHandTransform;

    [Header("Baton Configuration")]
    public BatonMode defaultBatonMode = BatonMode.BothHands;
    public enum BatonMode
    {
        BothHands,
        SingleBaton,
        NoHands
    }
    private GameObject leftHandOriginal;
    private GameObject rightHandOriginal;
    private bool handsTransformed = false;

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

    public GameObject[] GetIntroStanceBoxes()
    {
        return introStanceBoxes;
    }

   private void Start()
    {
        gameObject.SetActive(true);

        StoreOriginalTransforms();
        

        if (leftHandTransform != null)
            StoreTransform(leftHandTransform);
        if (rightHandTransform != null)
            StoreTransform(rightHandTransform);

        allDetectors = FindObjectsOfType<StanceDetector>();

        if (isRightHandDominant)
        {
            ApplyMirroringToAllObjects();
        }

        if (enableHandToBatonTransform && !IsInLevel())
        {
            ApplyBatonTransformation();
        }

        AssignSequencePositions();
        currentStance = "Default";
    }



    private bool IsInLevel()
    {
        return (useSparManager && SparManager.Instance != null) ||
            (useTutorialManager && TutorialLevelManager.Instance != null) ||
            (LevelManager.Instance != null);
    }

    private void StoreOriginalTransforms()
    {
        foreach (var box in defaultBoxes)
        {
            if (box != null)
            {
                StoreTransform(box.transform);
            }
        }

        if (introStanceBoxes != null)
        {
            foreach (var box in introStanceBoxes)
            {
                if (box != null)
                {
                    StoreTransform(box.transform);
                }
            }
        }

        foreach (var style in arnisStyles)
        {
            foreach (var box in style.stanceBoxes)
            {
                if (box != null)
                {
                    StoreTransform(box.transform);
                }
            }

            foreach (var sequence in style.sequences)
            {
                foreach (var box in sequence.sequenceBoxes)
                {
                    if (box != null)
                    {
                        StoreTransform(box.transform);
                    }
                }

                if (sequence.startBoxLeft != null) StoreTransform(sequence.startBoxLeft.transform);
                if (sequence.startBoxRight != null) StoreTransform(sequence.startBoxRight.transform);
                if (sequence.endBoxLeft != null) StoreTransform(sequence.endBoxLeft.transform);
                if (sequence.endBoxRight != null) StoreTransform(sequence.endBoxRight.transform);
            }
        }
    }

    public void SetBatonMode(BatonMode mode)
    {
        defaultBatonMode = mode;

        if (enableHandToBatonTransform)
        {
            ApplyBatonTransformation();
        }
    }

    private void ApplyBatonTransformation()
    {
        
        ResetHandTransformation();

        switch (defaultBatonMode)
        {
            case BatonMode.BothHands:
                ActivateBaton(leftHandTransform, leftHandBaton, ref leftHandOriginal);
                ActivateBaton(rightHandTransform, rightHandBaton, ref rightHandOriginal);
                break;

            case BatonMode.SingleBaton:
                if (isRightHandDominant)
                {
                    ActivateBaton(rightHandTransform, rightHandBaton, ref rightHandOriginal);
                }
                else
                {
                    ActivateBaton(leftHandTransform, leftHandBaton, ref leftHandOriginal);
                }
                break;

            case BatonMode.NoHands:
                
                break;
        }

        handsTransformed = (defaultBatonMode != BatonMode.NoHands);
    }
    
    private void ActivateBaton(Transform handTransform, GameObject batonObject, ref GameObject originalHand)
    {
        if (handTransform == null || batonObject == null) return;

        
        originalHand = handTransform.gameObject;
        originalHand.SetActive(false);

        
        batonObject.SetActive(true);

        
        if (defaultBatonMode == BatonMode.SingleBaton)
        {
            batonObject.tag = isRightHandDominant ? "Right Baton" : "Left Baton";
        }
        else
        {
            if (handTransform == leftHandTransform)
            {
                batonObject.tag = "Left Baton";
            }
            else if (handTransform == rightHandTransform)
            {
                batonObject.tag = "Right Baton";
            }
        }
    }


    public void ResetHandTransformation()
    {
        
        if (leftHandBaton != null)
        {
            leftHandBaton.SetActive(false);
        }

        if (rightHandBaton != null)
        {
            rightHandBaton.SetActive(false);
        }

        
        if (leftHandOriginal != null)
        {
            leftHandOriginal.SetActive(true);
        }

        if (rightHandOriginal != null)
        {
            rightHandOriginal.SetActive(true);
        }

        handsTransformed = false;
    }



    private void StoreTransform(Transform t)
    {
        originalPositions[t] = t.position;
        originalRotations[t] = t.rotation;
        originalScales[t] = t.localScale;
    }

    public void SetRightHandDominant(bool rightHandDominant)
    {
        if (isRightHandDominant != rightHandDominant)
        {
            isRightHandDominant = rightHandDominant;

            if (isRightHandDominant)
            {
                ApplyMirroringToAllObjects();
            }
            else
            {
                RestoreOriginalTransforms();
            }

            
            if (enableHandToBatonTransform && handsTransformed)
            {
                ApplyBatonTransformation();
            }

            RefreshAllVisualMirroring();
        }
    }

    public void ToggleHandToBatonTransform(bool enable)
    {
        enableHandToBatonTransform = enable;

        if (enableHandToBatonTransform)
        {
            ApplyBatonTransformation();
        }
        else
        {
            ResetHandTransformation();
        }
    }



    private void ApplyMirroringToAllObjects()
    {
        foreach (var kvp in originalPositions)
        {
            Transform t = kvp.Key;
            if (t != null)
            {
                
                if (t == leftHandTransform || t == rightHandTransform)
                {
                    continue;
                }
                
                ApplyMirroredTransform(t, kvp.Value, originalRotations[t], originalScales[t]);
            }
        }
    }

    private void RestoreOriginalTransforms()
    {
        foreach (var kvp in originalPositions)
        {
            Transform t = kvp.Key;
            if (t != null)
            {
                t.position = kvp.Value;
                t.rotation = originalRotations[t];
                t.localScale = originalScales[t];
            }
        }
    }

    private void ApplyMirroredTransform(Transform target, Vector3 originalPos, Quaternion originalRot, Vector3 originalScale)
    {
        if (mirrorPlane == null)
        {
            SetupDefaultMirrorPlane();
        }

        Vector3 mirrorPosition = mirrorPlane.position;
        Vector3 worldMirrorNormal = mirrorPlane.TransformDirection(mirrorNormal).normalized;

        Vector3 toOriginal = originalPos - mirrorPosition;
        float distanceToPlane = Vector3.Dot(toOriginal, worldMirrorNormal);
        Vector3 mirroredPos = originalPos - 2 * distanceToPlane * worldMirrorNormal;
        target.position = mirroredPos;

        Vector3 originalForward = originalRot * Vector3.forward;
        Vector3 originalUp = originalRot * Vector3.up;
        Vector3 mirroredForward = Vector3.Reflect(originalForward, worldMirrorNormal);
        Vector3 mirroredUp = Vector3.Reflect(originalUp, worldMirrorNormal);
        target.rotation = Quaternion.LookRotation(mirroredForward, mirroredUp);

        Vector3 mirroredScale = originalScale;
        if (Mathf.Abs(worldMirrorNormal.x) > 0.5f)
            mirroredScale.x *= -1;
        else if (Mathf.Abs(worldMirrorNormal.y) > 0.5f)
            mirroredScale.y *= -1;
        else if (Mathf.Abs(worldMirrorNormal.z) > 0.5f)
            mirroredScale.z *= -1;

        target.localScale = mirroredScale;
    }


    private void SetupDefaultMirrorPlane()
    {
        Debug.LogWarning("Mirror plane not set! Using world center as mirror plane.");
        GameObject tempPlane = new GameObject("TempMirrorPlane");
        tempPlane.transform.position = Vector3.zero;
        tempPlane.transform.SetParent(transform);
        mirrorPlane = tempPlane.transform;
    }



    private void RefreshAllVisualMirroring()
    {
        StanceDetector[] allStanceDetectors = FindObjectsOfType<StanceDetector>();

        foreach (var detector in allStanceDetectors)
        {
            if (detector != null)
            {
                detector.RefreshVisualMirroring();
            }
        }
    }

    private GameObject[] GetActiveDefaultBoxes()
    {
        return defaultBoxes;
    }

    private List<ArnisStyle> GetActiveArnisStyles()
    {
        return arnisStyles;
    }

    private void OnDestroy()
    {
        ResetHandTransformation();

        originalPositions.Clear();
        originalRotations.Clear();
        originalScales.Clear();
    }

    void OnDrawGizmos()
    {
        if (isRightHandDominant && mirrorPlane != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 worldNormal = mirrorPlane.TransformDirection(mirrorNormal);

            Gizmos.matrix = Matrix4x4.TRS(mirrorPlane.position, Quaternion.LookRotation(worldNormal), Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(2, 2, 0.1f));

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(mirrorPlane.position, worldNormal * 2);
        }
    }

    private void AssignSequencePositions()
    {
        foreach (var style in GetActiveArnisStyles())
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
        if (Input.GetKeyDown(KeyCode.Q))
        {
            TestActivateFirstSequence();
        }

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

        foreach (var box in GetActiveDefaultBoxes()) box.SetActive(true);
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
            foreach (var style in GetActiveArnisStyles())
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

        ForceResetTriggerStates(GetActiveDefaultBoxes());
        foreach (var box in GetActiveDefaultBoxes()) box.SetActive(false);

        foreach (var style in GetActiveArnisStyles())
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
        }
        else
        {
            foreach (var style in GetActiveArnisStyles())
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

        foreach (var box in GetActiveDefaultBoxes())
        {
            if (box != null)
            {
                box.SetActive(false);
            }
        }

        foreach (var style in GetActiveArnisStyles())
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
            foreach (var box in GetActiveDefaultBoxes()) box.SetActive(false);

            List<AttackSequence> targetSequences = new List<AttackSequence>();

            foreach (var style in GetActiveArnisStyles())
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
            foreach (var style in GetActiveArnisStyles())
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

            if (isRightHandDominant)
            {
                return leftDetector.IsRightHandInStance() && rightDetector.IsLeftHandInStance();
            }
            else
            {
                return leftDetector.IsLeftHandInStance() && rightDetector.IsRightHandInStance();
            }
        }
        return false;
    }
    public void StartAttackSequence(AttackSequence sequence)
    {
        currentAttackSequence = sequence;
        timer = stanceTimeout;
        totalBoxesTouched = 0;
        sequenceCounter = 0;

        ForceResetTriggerStates(currentAttackSequence.sequenceBoxes);

        foreach (var box in GetActiveDefaultBoxes()) box.SetActive(false);

        foreach (var style in GetActiveArnisStyles())
        {
            foreach (var box in style.stanceBoxes) box.SetActive(false);

            foreach (var seq in style.sequences)
            {
                if (seq.startBoxLeft != null) seq.startBoxLeft.SetActive(false);
                if (seq.startBoxRight != null) seq.startBoxRight.SetActive(false);
                if (seq.endBoxLeft != null) seq.endBoxLeft.SetActive(false);
                if (seq.endBoxRight != null) seq.endBoxRight.SetActive(false);
            }
        }

        foreach (var box in sequence.sequenceBoxes)
        {
            box.SetActive(true);
        }

        if (sequence.endBoxLeft != null) sequence.endBoxLeft.SetActive(true);
        if (sequence.endBoxRight != null) sequence.endBoxRight.SetActive(true);

        UpdateSequenceColors();

        Debug.Log($"Started attack sequence: {sequence.sequenceName} with {sequence.sequenceBoxes.Length} boxes");
    }

    private void CheckAttackSequence()
    {
        if (currentAttackSequence != null)
        {
            for (int i = 0; i < currentAttackSequence.sequenceBoxes.Length; i++)
            {
                var box = currentAttackSequence.sequenceBoxes[i];
                var detector = box.GetComponent<StanceDetector>();

                bool handInStance = false;

                if (isRightHandDominant)
                {
                    handInStance = detector.IsLeftHandInStance() || detector.IsRightHandInStance();
                }
                else
                {
                    handInStance = detector.IsLeftHandInStance() || detector.IsRightHandInStance();
                }

                if (handInStance && !detector.IsCompleted)
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

                bool endConditionMet = false;

                if (isRightHandDominant)
                {
                    endConditionMet = leftEndDetector.IsRightHandInStance() && rightEndDetector.IsLeftHandInStance();
                }
                else
                {
                    endConditionMet = leftEndDetector.IsLeftHandInStance() && rightEndDetector.IsRightHandInStance();
                }

                if (endConditionMet)
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

        foreach (var box in GetActiveDefaultBoxes()) box.SetActive(false);

        foreach (var style in GetActiveArnisStyles())
        {
            foreach (var box in style.stanceBoxes) box.SetActive(false);
        }

        foreach (var stanceName in availableStances)
        {
            foreach (var style in GetActiveArnisStyles())
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
    private void TestActivateFirstSequence()
    {

        if (GetActiveArnisStyles().Count > 0)
        {
            var firstStyle = GetActiveArnisStyles()[0];
            if (firstStyle.sequences.Count > 0)
            {
                var firstSequence = firstStyle.sequences[0];

                SetStance(firstStyle.styleName);
                currentArnisStyle = firstStyle;

                Debug.Log($"Set stance to: {firstStyle.styleName}");

                StartAttackSequence(firstSequence);

                Debug.Log($"Activated sequence: {firstSequence.sequenceName} from style: {firstStyle.styleName}");
            }
        }
    }
    public bool AreHandsTransformed()
{
    return handsTransformed;
}

    public BatonMode GetCurrentBatonMode()
    {
        return defaultBatonMode;
    }

        public void SetHandTransforms(Transform leftHand, Transform rightHand)
        {
            leftHandTransform = leftHand;
            rightHandTransform = rightHand;

            if (leftHandTransform != null)
                StoreTransform(leftHandTransform);
            if (rightHandTransform != null)
                StoreTransform(rightHandTransform);
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