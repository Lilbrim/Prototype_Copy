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

    [SerializeField] public AttackSequence currentAttackSequence;
    private StanceDetector[] allDetectors;
    [SerializeField] private int sequenceCounter;
    [SerializeField] public int totalBoxesTouched;

    private bool isPracticeMode = false;
    private string requiredStanceForPractice = "";

    private string currentStance = "Default";
    private ArnisStyle currentArnisStyle;

    [Header("Manager Settings")]
    public bool useSparManager = false;
    public bool useTutorialManager = false;

    [Header("Hand Dominance Mirroring")]
    [Tooltip("Check this if the right hand is dominant (will mirror positions)")]
    public bool isRightHandDominant = false;
    [Tooltip("Empty GameObject to use as the mirror center point (like Blender)")]
    public Transform mirrorCenter;
    [Tooltip("Mirror axis - X for left/right mirroring, Y for up/down, Z for forward/back")]
    public MirrorAxis mirrorAxis = MirrorAxis.X;

    // Store original positions and rotations for mirroring
    private Dictionary<GameObject, Vector3> originalPositions = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, Quaternion> originalRotations = new Dictionary<GameObject, Quaternion>();
    private bool positionsStored = false;

    public enum MirrorAxis
    {
        X, Y, Z
    }

    public delegate void StanceChangedDelegate(string newStance);
    public event StanceChangedDelegate OnStanceChanged;

    // Public properties for debugging
    public AttackSequence CurrentAttackSequence => currentAttackSequence;
    public string CurrentStance => currentStance;
    public int SequenceCounter => sequenceCounter;
    public int TotalBoxesTouched => totalBoxesTouched;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        InitializeManager();
    }

    private void InitializeManager()
    {
        allDetectors = FindObjectsOfType<StanceDetector>();

        // Validate required components
        if (!ValidateSetup())
        {
            Debug.LogError("StanceManager setup validation failed!");
            return;
        }

        // Store original positions and rotations before any mirroring
        StoreOriginalPositions();

        // Initialize all boxes as inactive
        SetAllBoxesActive(false);

        AssignSequencePositions();

        // Apply mirroring if right hand is dominant
        if (isRightHandDominant)
        {
            ApplyMirroring();
        }

        currentStance = "Default";
        Debug.Log("StanceManager initialized successfully");
    }

    private bool ValidateSetup()
    {
        bool isValid = true;

        if (defaultBoxes == null || defaultBoxes.Length == 0)
        {
            Debug.LogWarning("No default boxes assigned to StanceManager");
        }

        if (arnisStyles == null || arnisStyles.Count == 0)
        {
            Debug.LogWarning("No Arnis styles configured in StanceManager");
            return false;
        }

        foreach (var style in arnisStyles)
        {
            if (string.IsNullOrEmpty(style.styleName))
            {
                Debug.LogError("Found ArnisStyle with empty name");
                isValid = false;
            }

            if (style.stanceBoxes == null || style.stanceBoxes.Length == 0)
            {
                Debug.LogWarning($"No stance boxes assigned for style: {style.styleName}");
            }

            foreach (var sequence in style.sequences)
            {
                if (string.IsNullOrEmpty(sequence.sequenceName))
                {
                    Debug.LogError($"Found sequence with empty name in style: {style.styleName}");
                    isValid = false;
                }

                if (sequence.sequenceBoxes == null || sequence.sequenceBoxes.Length == 0)
                {
                    Debug.LogWarning($"No sequence boxes assigned for sequence: {sequence.sequenceName}");
                }
            }
        }

        return isValid;
    }

    private void SetAllBoxesActive(bool active)
    {
        // Set default boxes
        if (defaultBoxes != null)
        {
            foreach (var box in defaultBoxes)
            {
                if (box != null) box.SetActive(active);
            }
        }

        // Set stance boxes
        foreach (var style in arnisStyles)
        {
            if (style.stanceBoxes != null)
            {
                foreach (var box in style.stanceBoxes)
                {
                    if (box != null) box.SetActive(active);
                }
            }

            // Set sequence boxes
            foreach (var sequence in style.sequences)
            {
                if (sequence.sequenceBoxes != null)
                {
                    foreach (var box in sequence.sequenceBoxes)
                    {
                        if (box != null) box.SetActive(active);
                    }
                }

                if (sequence.startBoxLeft != null) sequence.startBoxLeft.SetActive(active);
                if (sequence.startBoxRight != null) sequence.startBoxRight.SetActive(active);
                if (sequence.endBoxLeft != null) sequence.endBoxLeft.SetActive(active);
                if (sequence.endBoxRight != null) sequence.endBoxRight.SetActive(active);
            }
        }
    }

    private void StoreOriginalPositions()
    {
        if (positionsStored) return;

        originalPositions.Clear();
        originalRotations.Clear();

        // Store default boxes positions and rotations
        if (defaultBoxes != null)
        {
            foreach (var box in defaultBoxes)
            {
                if (box != null)
                {
                    originalPositions[box] = box.transform.position;
                    originalRotations[box] = box.transform.rotation;
                }
            }
        }

        // Store stance boxes positions and rotations
        foreach (var style in arnisStyles)
        {
            if (style.stanceBoxes != null)
            {
                foreach (var box in style.stanceBoxes)
                {
                    if (box != null)
                    {
                        originalPositions[box] = box.transform.position;
                        originalRotations[box] = box.transform.rotation;
                    }
                }
            }

            // Store sequence boxes positions and rotations
            foreach (var sequence in style.sequences)
            {
                if (sequence.sequenceBoxes != null)
                {
                    foreach (var box in sequence.sequenceBoxes)
                    {
                        if (box != null)
                        {
                            originalPositions[box] = box.transform.position;
                            originalRotations[box] = box.transform.rotation;
                        }
                    }
                }

                if (sequence.startBoxLeft != null)
                {
                    originalPositions[sequence.startBoxLeft] = sequence.startBoxLeft.transform.position;
                    originalRotations[sequence.startBoxLeft] = sequence.startBoxLeft.transform.rotation;
                }
                if (sequence.startBoxRight != null)
                {
                    originalPositions[sequence.startBoxRight] = sequence.startBoxRight.transform.position;
                    originalRotations[sequence.startBoxRight] = sequence.startBoxRight.transform.rotation;
                }
                if (sequence.endBoxLeft != null)
                {
                    originalPositions[sequence.endBoxLeft] = sequence.endBoxLeft.transform.position;
                    originalRotations[sequence.endBoxLeft] = sequence.endBoxLeft.transform.rotation;
                }
                if (sequence.endBoxRight != null)
                {
                    originalPositions[sequence.endBoxRight] = sequence.endBoxRight.transform.position;
                    originalRotations[sequence.endBoxRight] = sequence.endBoxRight.transform.rotation;
                }
            }
        }

        positionsStored = true;
        Debug.Log($"Stored {originalPositions.Count} original positions and rotations for mirroring");
    }

    private void ApplyMirroring()
    {
        if (mirrorCenter == null)
        {
            Debug.LogWarning("Mirror center is not assigned! Please assign an empty GameObject as mirror center.");
            return;
        }

        Vector3 mirrorCenterPos = mirrorCenter.position;
        Quaternion mirrorCenterRot = mirrorCenter.rotation;
        int mirroredCount = 0;

        // Mirror all stored positions and rotations
        foreach (var kvp in originalPositions)
        {
            if (kvp.Key != null)
            {
                kvp.Key.transform.position = GetMirroredPosition(kvp.Value, mirrorCenterPos);
                
                if (originalRotations.ContainsKey(kvp.Key))
                {
                    kvp.Key.transform.rotation = GetMirroredRotation(originalRotations[kvp.Key], mirrorCenterRot);
                }
                
                mirroredCount++;
            }
        }

        Debug.Log($"Applied mirroring to {mirroredCount} objects (position and rotation)");
    }

    private Vector3 GetMirroredPosition(Vector3 originalPos, Vector3 mirrorCenterPos)
    {
        Vector3 mirroredPos = originalPos;
        
        switch (mirrorAxis)
        {
            case MirrorAxis.X:
                mirroredPos.x = mirrorCenterPos.x - (originalPos.x - mirrorCenterPos.x);
                break;
            case MirrorAxis.Y:
                mirroredPos.y = mirrorCenterPos.y - (originalPos.y - mirrorCenterPos.y);
                break;
            case MirrorAxis.Z:
                mirroredPos.z = mirrorCenterPos.z - (originalPos.z - mirrorCenterPos.z);
                break;
        }

        return mirroredPos;
    }

    private Quaternion GetMirroredRotation(Quaternion originalRotation, Quaternion mirrorCenterRotation)
    {
        // Convert quaternion to euler angles for easier mirroring
        Vector3 eulerAngles = originalRotation.eulerAngles;
        Vector3 mirroredEuler = eulerAngles;

        // Mirror rotation based on the selected axis
        switch (mirrorAxis)
        {
            case MirrorAxis.X:
                // Mirror Y and Z rotations when mirroring across X axis
                mirroredEuler.y = -eulerAngles.y;
                mirroredEuler.z = -eulerAngles.z;
                break;
            case MirrorAxis.Y:
                // Mirror X and Z rotations when mirroring across Y axis
                mirroredEuler.x = -eulerAngles.x;
                mirroredEuler.z = -eulerAngles.z;
                break;
            case MirrorAxis.Z:
                // Mirror X and Y rotations when mirroring across Z axis
                mirroredEuler.x = -eulerAngles.x;
                mirroredEuler.y = -eulerAngles.y;
                break;
        }

        return Quaternion.Euler(mirroredEuler);
    }

    private void RestoreOriginalPositions()
    {
        int restoredCount = 0;
        foreach (var kvp in originalPositions)
        {
            if (kvp.Key != null)
            {
                kvp.Key.transform.position = kvp.Value;
                
                if (originalRotations.ContainsKey(kvp.Key))
                {
                    kvp.Key.transform.rotation = originalRotations[kvp.Key];
                }
                
                restoredCount++;
            }
        }
        Debug.Log($"Restored {restoredCount} original positions and rotations");
    }

    // Public method to toggle hand dominance at runtime
    public void SetHandDominance(bool rightHandDominant)
    {
        if (isRightHandDominant == rightHandDominant) return;

        isRightHandDominant = rightHandDominant;

        if (isRightHandDominant)
        {
            ApplyMirroring();
        }
        else
        {
            RestoreOriginalPositions();
        }

        Debug.Log($"Hand dominance set to: {(rightHandDominant ? "Right" : "Left")}");
    }

    // Editor helper method to refresh mirroring
    [ContextMenu("Refresh Mirroring")]
    private void RefreshMirroring()
    {
        if (!Application.isPlaying) return;

        if (isRightHandDominant)
        {
            RestoreOriginalPositions();
            ApplyMirroring();
        }
        else
        {
            RestoreOriginalPositions();
        }
    }

    private void AssignSequencePositions()
    {
        foreach (var style in arnisStyles)
        {
            foreach (var sequence in style.sequences)
            {
                if (sequence.sequenceBoxes != null)
                {
                    for (int i = 0; i < sequence.sequenceBoxes.Length; i++)
                    {
                        if (sequence.sequenceBoxes[i] != null)
                        {
                            StanceDetector detector = sequence.sequenceBoxes[i].GetComponent<StanceDetector>();
                            if (detector != null)
                            {
                                detector.isPartOfSequence = true;
                                detector.sequencePosition = i;
                            }
                        }
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
        if (!isGameActive) return;

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
                Debug.Log("Attack sequence timed out");
                ResetSequence();
                SetStance("Default");
            }
        }
        else if (currentStance != "Default")
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                Debug.Log("Stance timed out, returning to Default");
                SetStance("Default");
            }
        }
    }

    public void SetGameActive(bool active)
    {
        isGameActive = active;
        Debug.Log($"Game active state set to: {active}");
    }

    public void ActivateDefaultStance()
    {
        if (!isGameActive) return;

        if (defaultBoxes != null)
        {
            foreach (var box in defaultBoxes)
            {
                if (box != null) box.SetActive(true);
            }
        }
    }

    public void EnterStance(string stanceName, bool practiceMode = false)
    {
        if (!isGameActive) return;

        if (string.IsNullOrEmpty(stanceName))
        {
            Debug.LogWarning("Attempted to enter stance with null or empty name");
            return;
        }

        isPracticeMode = practiceMode;
        requiredStanceForPractice = stanceName;

        if (currentStance == "Default" && practiceMode)
        {
            ActivateBoxesForPracticeMode("Default");
            return;
        }

        if (stanceName != currentStance && currentStance == "Default")
        {
            bool validStance = IsValidStance(stanceName);

            if (validStance)
            {
                SetStance(stanceName);
                NotifyStanceEntered(stanceName);
            }
            else
            {
                NotifyStanceEntered("Incorrect");
            }
        }
        else
        {
            NotifyStanceEntered("Incorrect");
        }
    }

    private bool IsValidStance(string stanceName)
    {
        foreach (var style in arnisStyles)
        {
            if (style.styleName == stanceName)
            {
                return true;
            }
        }
        return false;
    }

    private void NotifyStanceEntered(string stanceName)
    {
        if (useSparManager && SparManager.Instance != null)
        {
            // SparManager notification logic here
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

    private void SetStance(string newStance)
    {
        timer = stanceTimeout;
        currentArnisStyle = null;
        
        Debug.Log($"Setting stance from {currentStance} to {newStance}");
        
        OnStanceChanged?.Invoke(newStance);

        // Reset all detectors
        if (allDetectors != null)
        {
            foreach (var detector in allDetectors)
            {
                if (detector != null)
                {
                    detector.ResetStance();
                }
            }
        }

        // Deactivate all boxes
        SetAllBoxesActive(false);

        if (newStance == "Default")
        {
            if (isPracticeMode)
            {
                ActivateBoxesForPracticeMode(newStance);
            }
        }
        else
        {
            // Find and activate the specific style
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
                        if (style.stanceBoxes != null)
                        {
                            foreach (var box in style.stanceBoxes)
                            {
                                if (box != null) box.SetActive(true);
                            }
                        }
                    }
                    break;
                }
            }
        }

        ResetSequenceState();
        currentStance = newStance;
    }

    private void ResetSequenceState()
    {
        currentAttackSequence = null;
        sequenceCounter = 0;
        totalBoxesTouched = 0;
    }

    public void ClearAllStances()
    {
        Debug.Log("Clearing all stances and sequence boxes");

        // Reset all detectors
        if (allDetectors != null)
        {
            foreach (var detector in allDetectors)
            {
                if (detector != null)
                {
                    detector.ResetStance();
                    detector.ForceResetTriggerState();
                }
            }
        }

        // Deactivate all boxes
        SetAllBoxesActive(false);

        // Reset sequence completion states
        if (currentAttackSequence != null && currentAttackSequence.sequenceBoxes != null)
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
                }
            }
        }

        // Reset state
        ResetSequenceState();
        currentStance = "Default";
        currentArnisStyle = null;

        // Clear event listeners
        OnStanceChanged = null;
    }

    private void ActivateBoxesForPracticeMode(string newStance)
    {
        if (!isGameActive) return;

        if (newStance == "Default")
        {
            // Deactivate default boxes
            if (defaultBoxes != null)
            {
                foreach (var box in defaultBoxes)
                {
                    if (box != null) box.SetActive(false);
                }
            }

            // Find sequences for the required practice stance
            List<AttackSequence> targetSequences = new List<AttackSequence>();

            foreach (var style in arnisStyles)
            {
                if (style.styleName == requiredStanceForPractice)
                {
                    targetSequences = style.sequences;
                    break;
                }
            }

            // Activate start boxes for target sequences
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
            // Activate stance boxes for the specified style
            foreach (var style in arnisStyles)
            {
                if (style.styleName == newStance && style.stanceBoxes != null)
                {
                    foreach (var box in style.stanceBoxes)
                    {
                        if (box != null) box.SetActive(true);
                    }
                    break;
                }
            }
        }
    }

    private void CheckForSequenceStart()
    {
        if (currentArnisStyle?.sequences == null) return;

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

            if (leftDetector != null && rightDetector != null)
            {
                return leftDetector.IsLeftHandInStance() && rightDetector.IsRightHandInStance();
            }
        }
        return false;
    }

    private void StartAttackSequence(AttackSequence sequence)
    {
        if (sequence == null)
        {
            Debug.LogError("Attempted to start null attack sequence");
            return;
        }

        Debug.Log($"Starting attack sequence: {sequence.sequenceName}");
        
        currentAttackSequence = sequence;
        timer = stanceTimeout;
        totalBoxesTouched = 0;

        // Reset sequence box states
        if (sequence.sequenceBoxes != null)
        {
            foreach (var box in sequence.sequenceBoxes)
            {
                if (box != null)
                {
                    var detector = box.GetComponent<StanceDetector>();
                    if (detector != null)
                    {
                        detector.IsCompleted = false;
                    }
                }
            }
        }

        // Deactivate all boxes first
        SetAllBoxesActive(false);

        // Activate sequence boxes
        if (sequence.sequenceBoxes != null)
        {
            foreach (var box in sequence.sequenceBoxes)
            {
                if (box != null) box.SetActive(true);
            }
        }

        // Activate end boxes
        if (sequence.endBoxLeft != null) sequence.endBoxLeft.SetActive(true);
        if (sequence.endBoxRight != null) sequence.endBoxRight.SetActive(true);

        UpdateSequenceColors();
    }

    private void CheckAttackSequence()
    {
        if (currentAttackSequence?.sequenceBoxes == null) return;

        // Check sequence boxes
        for (int i = 0; i < currentAttackSequence.sequenceBoxes.Length; i++)
        {
            var box = currentAttackSequence.sequenceBoxes[i];
            if (box == null) continue;

            var detector = box.GetComponent<StanceDetector>();
            if (detector == null) continue;

            if ((detector.IsLeftHandInStance() || detector.IsRightHandInStance()) && !detector.IsCompleted)
            {
                detector.IsCompleted = true;
                sequenceCounter++;
                totalBoxesTouched++;
                Debug.Log($"Box {box.name} completed. Total completed: {sequenceCounter}");
            }
        }

        // Check end condition
        if (currentAttackSequence.endBoxLeft != null && currentAttackSequence.endBoxRight != null)
        {
            var leftEndDetector = currentAttackSequence.endBoxLeft.GetComponent<StanceDetector>();
            var rightEndDetector = currentAttackSequence.endBoxRight.GetComponent<StanceDetector>();

            if (leftEndDetector != null && rightEndDetector != null &&
                leftEndDetector.IsLeftHandInStance() && rightEndDetector.IsRightHandInStance())
            {
                Debug.Log($"{currentStance}.{currentAttackSequence.sequenceName} completed. Boxes triggered: {totalBoxesTouched} out of {currentAttackSequence.sequenceBoxes.Length}");

                NotifyObjectiveCompletion();
                ResetSequence();
                SetStance("Default");
            }
        }
    }

    private void ResetSequence()
    {
        if (currentAttackSequence?.sequenceBoxes != null)
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
                }
            }
        }

        ResetSequenceState();
        Debug.Log("Attack sequence reset");
    }

    public void NotifyObjectiveCompletion()
    {
        int touchedBoxes = totalBoxesTouched;
        int sequenceBoxCount = currentAttackSequence?.sequenceBoxes?.Length ?? 0;

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
        if (availableStances == null || availableStances.Count == 0)
        {
            Debug.LogWarning("No available stances provided for Phase 2");
            return;
        }

        Debug.Log($"Activating Phase 2 stances: {string.Join(", ", availableStances)}");

        // Deactivate all boxes
        SetAllBoxesActive(false);

        // Activate start boxes for available stances
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
        if (sequence?.sequenceBoxes == null) return;
        
        int totalBoxes = sequence.sequenceBoxes.Length;
        
        for (int i = 0; i < totalBoxes; i++)
        {
            if (sequence.sequenceBoxes[i] != null)
            {
                StanceDetector detector = sequence.sequenceBoxes[i].GetComponent<StanceDetector>();
                if (detector != null)
                {
                    detector.UpdateColorForSequence(totalBoxes);
                }
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

    // Debug methods
    [ContextMenu("Log Current State")]
    private void LogCurrentState()
    {
        Debug.Log($"Current State - Stance: {currentStance}, Sequence: {currentAttackSequence?.sequenceName ?? "None"}, " +
                  $"Counter: {sequenceCounter}, Touched: {totalBoxesTouched}, Timer: {timer:F2}");
    }

    private void OnDestroy()
    {
        // Clean up events
        OnStanceChanged = null;
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