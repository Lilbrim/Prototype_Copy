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
    
    private Dictionary<GameObject, GameObject> mirroredBoxes = new Dictionary<GameObject, GameObject>();
    public List<ArnisStyle> mirroredArnisStyles = new List<ArnisStyle>();
    private GameObject[] mirroredDefaultBoxes;
    private GameObject[] mirroredIntroBoxes;
    private bool mirroredDataInitialized = false;
    
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

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeMirroredObjectsParent();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeMirroredObjectsParent()
    {
        GameObject parentObj = GameObject.Find(MIRRORED_PARENT_NAME);
        if (parentObj == null)
        {
            parentObj = new GameObject(MIRRORED_PARENT_NAME);
            parentObj.transform.SetParent(transform);
        }
        mirroredObjectsParent = parentObj.transform;
    }
    
    public GameObject[] GetIntroStanceBoxes()
    {
        if (isRightHandDominant && mirroredIntroBoxes != null)
        {
            return mirroredIntroBoxes;
        }
        return introStanceBoxes;
    }

    private void Start()
    {
        gameObject.SetActive(true);
        InitializeMirroringSystem();

        allDetectors = FindObjectsOfType<StanceDetector>();

        foreach (var box in GetActiveDefaultBoxes()) box.SetActive(false);

        foreach (var style in GetActiveArnisStyles())
        {
            foreach (var box in style.stanceBoxes) box.SetActive(false);
        }

        AssignSequencePositions();
        currentStance = "Default";
    }

    private void InitializeMirroringSystem()
    {
        if (isRightHandDominant && !mirroredDataInitialized)
        {
            CreateMirroredStances();
            mirroredDataInitialized = true;
        }
    }

    private void CreateMirroredStances()
    {
        if (mirrorPlane == null)
        {
            SetupDefaultMirrorPlane();
        }

        if (mirroredDataInitialized)
        {
            UpdateMirroredPositions();
            return;
        }

        CreateMirroredDefaultBoxes();
        CreateMirroredArnisStyles();
    }

    private void SetupDefaultMirrorPlane()
    {
        Debug.LogWarning("Mirror plane not set! Using world center as mirror plane.");
        GameObject tempPlane = new GameObject("TempMirrorPlane");
        tempPlane.transform.position = Vector3.zero;
        tempPlane.transform.SetParent(transform);
        mirrorPlane = tempPlane.transform;
    }

    private void CreateMirroredDefaultBoxes()
    {
        if (mirroredDefaultBoxes != null) return;

        // Create mirrored default boxes (existing code)
        mirroredDefaultBoxes = new GameObject[defaultBoxes.Length];
        
        for (int i = 0; i < defaultBoxes.Length; i++)
        {
            if (defaultBoxes[i] != null)
            {
                GameObject mirrored = CreateOptimizedMirroredBox(defaultBoxes[i], $"Default_{i}");
                mirroredDefaultBoxes[i] = mirrored;
                mirroredBoxes[defaultBoxes[i]] = mirrored;
            }
        }

        // NEW: Create mirrored intro boxes
        CreateMirroredIntroBoxes();
    }

    private void CreateMirroredArnisStyles()
    {
        if (mirroredArnisStyles.Count > 0) return;

        foreach (var style in arnisStyles)
        {
            ArnisStyle mirroredStyle = CreateMirroredStyle(style);
            mirroredArnisStyles.Add(mirroredStyle);
        }
    }

    private ArnisStyle CreateMirroredStyle(ArnisStyle originalStyle)
    {
        ArnisStyle mirroredStyle = new ArnisStyle
        {
            styleName = originalStyle.styleName,
            stanceBoxes = new GameObject[originalStyle.stanceBoxes.Length],
            sequences = new List<AttackSequence>()
        };

        for (int i = 0; i < originalStyle.stanceBoxes.Length; i++)
        {
            if (originalStyle.stanceBoxes[i] != null)
            {
                GameObject mirrored = CreateOptimizedMirroredBox(
                    originalStyle.stanceBoxes[i], 
                    $"{originalStyle.styleName}_Stance_{i}"
                );
                mirroredStyle.stanceBoxes[i] = mirrored;
                mirroredBoxes[originalStyle.stanceBoxes[i]] = mirrored;
            }
        }

        foreach (var sequence in originalStyle.sequences)
        {
            AttackSequence mirroredSequence = CreateMirroredSequence(sequence, originalStyle.styleName);
            mirroredStyle.sequences.Add(mirroredSequence);
        }

        return mirroredStyle;
    }

    private AttackSequence CreateMirroredSequence(AttackSequence originalSequence, string styleName)
    {
        AttackSequence mirroredSequence = new AttackSequence
        {
            sequenceName = originalSequence.sequenceName,
            timeLimit = originalSequence.timeLimit,
            sequenceBoxes = new GameObject[originalSequence.sequenceBoxes.Length]
        };

        for (int i = 0; i < originalSequence.sequenceBoxes.Length; i++)
        {
            if (originalSequence.sequenceBoxes[i] != null)
            {
                GameObject mirrored = CreateOptimizedMirroredBox(
                    originalSequence.sequenceBoxes[i],
                    $"{styleName}_{originalSequence.sequenceName}_Seq_{i}"
                );
                mirroredSequence.sequenceBoxes[i] = mirrored;
                mirroredBoxes[originalSequence.sequenceBoxes[i]] = mirrored;
            }
        }

        mirroredSequence.startBoxLeft = CreateMirroredSpecialBox(originalSequence.startBoxLeft, $"{styleName}_{originalSequence.sequenceName}_StartL");
        mirroredSequence.startBoxRight = CreateMirroredSpecialBox(originalSequence.startBoxRight, $"{styleName}_{originalSequence.sequenceName}_StartR");
        mirroredSequence.endBoxLeft = CreateMirroredSpecialBox(originalSequence.endBoxLeft, $"{styleName}_{originalSequence.sequenceName}_EndL");
        mirroredSequence.endBoxRight = CreateMirroredSpecialBox(originalSequence.endBoxRight, $"{styleName}_{originalSequence.sequenceName}_EndR");

        return mirroredSequence;
    }

    private GameObject CreateMirroredSpecialBox(GameObject original, string namePrefix)
    {
        if (original == null) return null;
        
        GameObject mirrored = CreateOptimizedMirroredBox(original, namePrefix);
        mirroredBoxes[original] = mirrored;
        return mirrored;
    }

    private GameObject CreateOptimizedMirroredBox(GameObject original, string namePrefix)
    {
        GameObject mirrored = InstantiateOptimized(original);
        mirrored.name = $"{namePrefix}_Mirrored";
        mirrored.transform.SetParent(mirroredObjectsParent);

        MirrorTransform(original.transform, mirrored.transform);

        mirrored.SetActive(original.activeSelf);

        return mirrored;
    }
    
    private void CreateMirroredIntroBoxes()
    {
        if (introStanceBoxes == null || introStanceBoxes.Length == 0) return;
        if (mirroredIntroBoxes != null) return; // Already created

        mirroredIntroBoxes = new GameObject[introStanceBoxes.Length];
        
        for (int i = 0; i < introStanceBoxes.Length; i++)
        {
            if (introStanceBoxes[i] != null)
            {
                GameObject mirrored = CreateOptimizedMirroredBox(introStanceBoxes[i], $"Intro_{i}");
                mirroredIntroBoxes[i] = mirrored;
                mirroredBoxes[introStanceBoxes[i]] = mirrored;
            }
        }
    }

    private GameObject InstantiateOptimized(GameObject original)
    {
        GameObject instance = Instantiate(original, mirroredObjectsParent);


        return instance;
    }

    private void UpdateMirroredPositions()
    {
        if (!mirroredDataInitialized) return;

        UpdateMirroredBoxPositions(defaultBoxes, mirroredDefaultBoxes);
        
        UpdateMirroredBoxPositions(introStanceBoxes, mirroredIntroBoxes);

        for (int styleIndex = 0; styleIndex < arnisStyles.Count && styleIndex < mirroredArnisStyles.Count; styleIndex++)
        {
            var originalStyle = arnisStyles[styleIndex];
            var mirroredStyle = mirroredArnisStyles[styleIndex];

            UpdateMirroredBoxPositions(originalStyle.stanceBoxes, mirroredStyle.stanceBoxes);

            for (int seqIndex = 0; seqIndex < originalStyle.sequences.Count && seqIndex < mirroredStyle.sequences.Count; seqIndex++)
            {
                var originalSeq = originalStyle.sequences[seqIndex];
                var mirroredSeq = mirroredStyle.sequences[seqIndex];

                UpdateMirroredBoxPositions(originalSeq.sequenceBoxes, mirroredSeq.sequenceBoxes);
                
                UpdateSingleMirroredPosition(originalSeq.startBoxLeft, mirroredSeq.startBoxLeft);
                UpdateSingleMirroredPosition(originalSeq.startBoxRight, mirroredSeq.startBoxRight);
                UpdateSingleMirroredPosition(originalSeq.endBoxLeft, mirroredSeq.endBoxLeft);
                UpdateSingleMirroredPosition(originalSeq.endBoxRight, mirroredSeq.endBoxRight);
            }
        }
    }

    private void UpdateMirroredBoxPositions(GameObject[] original, GameObject[] mirrored)
    {
        if (original == null || mirrored == null) return;

        int minLength = Mathf.Min(original.Length, mirrored.Length);
        for (int i = 0; i < minLength; i++)
        {
            UpdateSingleMirroredPosition(original[i], mirrored[i]);
        }
    }

    private void UpdateSingleMirroredPosition(GameObject original, GameObject mirrored)
    {
        if (original != null && mirrored != null)
        {
            MirrorTransform(original.transform, mirrored.transform);
        }
    }

    private void MirrorTransform(Transform original, Transform mirrored)
    {
        Vector3 mirrorPosition = mirrorPlane.position;
        Vector3 worldMirrorNormal = mirrorPlane.TransformDirection(mirrorNormal).normalized;

        Vector3 originalPos = original.position;
        Vector3 toOriginal = originalPos - mirrorPosition;
        float distanceToPlane = Vector3.Dot(toOriginal, worldMirrorNormal);
        Vector3 mirroredPos = originalPos - 2 * distanceToPlane * worldMirrorNormal;
        mirrored.position = mirroredPos;

        Vector3 mirroredForward = Vector3.Reflect(original.forward, worldMirrorNormal);
        Vector3 mirroredUp = Vector3.Reflect(original.up, worldMirrorNormal);
        mirrored.rotation = Quaternion.LookRotation(mirroredForward, mirroredUp);

        Vector3 originalScale = original.localScale;
        Vector3 mirroredScale = originalScale;
        
        if (Mathf.Abs(worldMirrorNormal.x) > 0.5f)
            mirroredScale.x *= -1;
        else if (Mathf.Abs(worldMirrorNormal.y) > 0.5f)
            mirroredScale.y *= -1;
        else if (Mathf.Abs(worldMirrorNormal.z) > 0.5f)
            mirroredScale.z *= -1;
            
        mirrored.localScale = mirroredScale;
    }

    private void ClearMirroredStances()
    {
        if (mirroredObjectsParent != null)
        {
            foreach (Transform child in mirroredObjectsParent)
            {
                child.gameObject.SetActive(false);
            }
        }

        mirroredBoxes.Clear();
        mirroredArnisStyles.Clear();
        mirroredDefaultBoxes = null;
        mirroredIntroBoxes = null; 
        mirroredDataInitialized = false;
    }

    private void DestroyMirroredStances()
    {
        if (mirroredObjectsParent != null)
        {
            for (int i = mirroredObjectsParent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(mirroredObjectsParent.GetChild(i).gameObject);
            }
        }

        mirroredBoxes.Clear();
        mirroredArnisStyles.Clear();
        mirroredDefaultBoxes = null;
        mirroredIntroBoxes = null;
        mirroredDataInitialized = false;
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
        return isRightHandDominant && mirroredDefaultBoxes != null ? mirroredDefaultBoxes : defaultBoxes;
    }

    private List<ArnisStyle> GetActiveArnisStyles()
    {
        return isRightHandDominant && mirroredArnisStyles.Count > 0 ? mirroredArnisStyles : arnisStyles;
    }

    public void SetRightHandDominant(bool rightHandDominant)
    {
        if (isRightHandDominant != rightHandDominant)
        {
            isRightHandDominant = rightHandDominant;

            if (isRightHandDominant)
            {
                if (!mirroredDataInitialized)
                {
                    CreateMirroredStances();
                    mirroredDataInitialized = true;
                }
                else
                {
                    ActivateMirroredObjects(true);
                    UpdateMirroredPositions();
                }
            }
            else
            {
                ActivateMirroredObjects(false);
            }

            allDetectors = FindObjectsOfType<StanceDetector>();
            AssignSequencePositions();
            RefreshAllVisualMirroring();
        }
    }

    private void ActivateMirroredObjects(bool activate)
    {
        if (mirroredObjectsParent != null)
        {
            foreach (Transform child in mirroredObjectsParent)
            {
                child.gameObject.SetActive(activate && child.gameObject.activeSelf);
            }
        }
    }

    public void BatchUpdateMirroredPositions()
    {
        if (isRightHandDominant && mirroredDataInitialized)
        {
            StartCoroutine(BatchUpdateCoroutine());
        }
    }

    private IEnumerator BatchUpdateCoroutine()
    {
        UpdateMirroredBoxPositions(defaultBoxes, mirroredDefaultBoxes);
        yield return null;

        UpdateMirroredBoxPositions(introStanceBoxes, mirroredIntroBoxes);
        yield return null;

        for (int i = 0; i < arnisStyles.Count && i < mirroredArnisStyles.Count; i++)
        {
            UpdateMirroredBoxPositions(arnisStyles[i].stanceBoxes, mirroredArnisStyles[i].stanceBoxes);
            yield return null;
        }
    }

    private void OnDestroy()
    {
        DestroyMirroredStances();
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

        foreach (var box in GetActiveDefaultBoxes()) box.SetActive(false);

        foreach (var style in GetActiveArnisStyles())
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