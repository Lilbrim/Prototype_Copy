using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class StanceGuide : MonoBehaviour
{
    [System.Serializable]
    public struct BatonKeyframe
    {
        public Vector3 position;
        public Quaternion rotation;

        [SerializeField] private Transform sourceTransform;
        public Transform SourceTransform
        {
            get => sourceTransform;
            set => sourceTransform = value;
        }

        public BatonKeyframe(Vector3 pos, Quaternion rot)
        {
            position = pos;
            rotation = rot;
            sourceTransform = null;
        }

        public BatonKeyframe(Transform source)
        {
            sourceTransform = source;
            position = source != null ? source.position : Vector3.zero;
            rotation = source != null ? source.rotation : Quaternion.identity;
        }
    }

    private bool IsOneHandedSequence(AttackSequence sequence, out bool isLeftHandOnly, out bool isRightHandOnly)
    {
        isLeftHandOnly = false;
        isRightHandOnly = false;

        if (sequence == null || sequence.sequenceBoxes == null || sequence.sequenceBoxes.Length == 0)
            return false;

        bool hasLeftHandBoxes = false;
        bool hasRightHandBoxes = false;
        bool hasBothHandBoxes = false;


        if (sequence.startBoxLeft != null) hasLeftHandBoxes = true;
        if (sequence.startBoxRight != null) hasRightHandBoxes = true;


        foreach (var box in sequence.sequenceBoxes)
        {
            if (box != null)
            {
                if (box.CompareTag("Left Baton") || box.CompareTag("Left Hand"))
                {
                    hasLeftHandBoxes = true;
                }
                else if (box.CompareTag("Right Baton") || box.CompareTag("Right Hand"))
                {
                    hasRightHandBoxes = true;
                }
                else
                {

                    hasBothHandBoxes = true;
                }
            }
        }


        if (sequence.endBoxLeft != null) hasLeftHandBoxes = true;
        if (sequence.endBoxRight != null) hasRightHandBoxes = true;


        if (hasBothHandBoxes) return false;


        if (hasLeftHandBoxes && !hasRightHandBoxes)
        {
            isLeftHandOnly = true;
            return true;
        }
        else if (hasRightHandBoxes && !hasLeftHandBoxes)
        {
            isRightHandOnly = true;
            return true;
        }

        return false;
    }

    [System.Serializable]
    public class BatonConfig
    {
        [Header("Baton Identity")]
        public string batonName = "Baton";
        public GameObject batonPrefab;
        public bool showBaton = true;

        [Header("Path Configuration")]
        public BatonKeyframe[] batonPath;
        public bool loop = false;

        [Header("Loop Delay Settings")]
        [Tooltip("Delay in seconds before starting the next loop iteration")]
        public float loopDelay = 1f;
        [Tooltip("Should the baton be hidden during the loop delay?")]
        public bool hideDuringDelay = false;

        [Header("Curve Settings")]
        [Tooltip("Enable smooth curved paths between waypoints")]
        public bool useSmoothCurve = true;
        [Tooltip("Number of interpolated points between each pair of waypoints")]
        [Range(5, 50)]
        public int curveResolution = 20;
        [Tooltip("Curve smoothness factor (0 = sharp corners, 1 = very smooth)")]
        [Range(0f, 1f)]
        public float curveSmoothness = 0.3f;
        [System.NonSerialized] public BatonKeyframe[] smoothedPath;
        public bool autoPlay = true;
        public float constantSpeed = 5f;

        [Header("Hand Assignment")]
        public bool isLeftHand = true;
         [Header("Trail Settings")]
        [Tooltip("Enable trail effect for this baton")]
        public bool enableTrail = true;
        [Tooltip("Trail time duration")]
        [Range(0.1f, 10f)]
        public float trailTime = 2f;
        [Tooltip("Trail width")]
        [Range(0.01f, 1f)]
        public float trailWidth = 0.05f;
        [Tooltip("Trail material")]
        public Material trailMaterial;
        [Tooltip("Trail color gradient")]

        [System.NonSerialized] public TrailRenderer batonTrail;
        public Gradient trailColorGradient = new Gradient();

        [System.NonSerialized] public float totalPathDistance = 0f;
        [System.NonSerialized] public float[] segmentDistances;
        [System.NonSerialized] public float[] cumulativeDistances;
        [System.NonSerialized] public float currentDistance = 0f;
        [System.NonSerialized] public int currentIndex = 0;
        [System.NonSerialized] public bool isPlaying = false;
        [System.NonSerialized] public GameObject batonInstance;
        [System.NonSerialized] public float effectiveSpeed = 5f;


        [System.NonSerialized] public bool isWaitingForLoop = false;
        [System.NonSerialized] public float loopDelayTimer = 0f;
        [System.NonSerialized] public bool wasHiddenForDelay = false;
    }

    public BatonConfig leftHandBaton = new BatonConfig();
    public BatonConfig rightHandBaton = new BatonConfig();

    public bool synchronizeBatons = false;
    public float synchronizedDuration = 3f;
    public bool useSlowerBatonDuration = true;

    [Header("Auto-Detection Settings")]
    public bool autoDetectSequences = true;
    public bool startOnSequenceBegin = true;
    public bool stopOnSequenceEnd = true;
    public bool hideBatonsWhenNoSequence = true;

    [Header("Debug Settings")]
    public bool showGizmos = true;

    private BatonConfig[] batonConfigs;
    private AttackSequence currentSequence;
    private bool wasSequenceActive = false;

    void Awake()
    {
        leftHandBaton.batonName = "Left Hand Baton";
        leftHandBaton.isLeftHand = true;

        rightHandBaton.batonName = "Right Hand Baton";
        rightHandBaton.isLeftHand = false;

        batonConfigs = new BatonConfig[] { leftHandBaton, rightHandBaton };

        autoDetectSequences = true;
    }

    void Start()
{
    if (StanceManager.Instance != null)
    {
        StanceManager.Instance.OnStanceChanged += OnStanceChanged;
        
        
        currentSequence = StanceManager.Instance.currentAttackSequence;
        wasSequenceActive = IsSequenceValid(currentSequence);
    }

    for (int i = 0; i < batonConfigs.Length; i++)
    {
        InitializeBaton(i);
    }

    
    if (!IsSequenceValid(currentSequence))
    {
        HideAllBatons();
    }
    
    else if (IsSequenceValid(currentSequence))
    {
        GeneratePathsFromCurrentSequence();
        if (startOnSequenceBegin)
        {
            StartAllBatons();
        }
    }
}

    void Update()
    {
        if (autoDetectSequences)
        {
            CheckForSequenceChanges();
        }

        for (int i = 0; i < batonConfigs.Length; i++)
        {
            UpdateBaton(i);
        }
    }

    private bool IsSequenceValid(AttackSequence sequence)
{
    return sequence != null && 
           sequence.sequenceBoxes != null && 
           sequence.sequenceBoxes.Length > 0;
}

    void CheckForSequenceChanges()
{
    if (StanceManager.Instance == null) return;

    AttackSequence newSequence = StanceManager.Instance.currentAttackSequence;
    bool isSequenceActive = IsSequenceValid(newSequence);

    if (isSequenceActive && !wasSequenceActive)
    {
        currentSequence = newSequence;
        GeneratePathsFromCurrentSequence();

        if (hideBatonsWhenNoSequence)
        {
            ShowAllBatons();
        }

        if (startOnSequenceBegin)
        {
            StartAllBatons();
        }
    }
    else if (!isSequenceActive && wasSequenceActive)
    {
        
        if (stopOnSequenceEnd)
        {
            StopAllBatons();
        }

        
        HideAllBatons();

        currentSequence = null;
    }
    else if (isSequenceActive && currentSequence != newSequence)
    {
        currentSequence = newSequence;
        GeneratePathsFromCurrentSequence();
        ResetAllBatons();

        if (hideBatonsWhenNoSequence)
        {
            ShowAllBatons();
        }

        if (startOnSequenceBegin)
        {
            StartAllBatons();
        }
    }
    
    else if (!isSequenceActive && currentSequence != null)
    {
        if (stopOnSequenceEnd)
        {
            StopAllBatons();
        }

        
        HideAllBatons();

        currentSequence = null;
    }

    wasSequenceActive = isSequenceActive;
}


    public void ShowAllBatons()
    {
        for (int i = 0; i < batonConfigs.Length; i++)
        {
            ShowBaton(i);
        }
    }

    public void HideAllBatons()
    {
        for (int i = 0; i < batonConfigs.Length; i++)
        {
            HideBaton(i);
        }
    }


    void HideBaton(int batonIndex)
    {
        if (batonIndex >= batonConfigs.Length) return;

        var config = batonConfigs[batonIndex];
        if (config.batonInstance != null)
        {
            config.batonInstance.SetActive(false);
        }

        
        if (config.batonTrail != null)
        {
            config.batonTrail.Clear();
        }
    }


    void ShowBaton(int batonIndex)
    {
        if (batonIndex >= batonConfigs.Length) return;

        var config = batonConfigs[batonIndex];


        if (config.batonInstance == null && config.batonPrefab != null)
        {
            CreateBatonInstance(batonIndex);
        }

        if (config.batonInstance != null)
        {
            config.batonInstance.SetActive(true);
        }
    }

    void GeneratePathsFromCurrentSequence()
    {
        if (currentSequence == null || currentSequence.sequenceBoxes == null || currentSequence.sequenceBoxes.Length == 0)
        {
            Debug.Log("No current sequence or sequence boxes");
            return;
        }


        Debug.Log($"=== SEQUENCE ANALYSIS ===");
        Debug.Log($"Start Box Left: {(currentSequence.startBoxLeft != null ? currentSequence.startBoxLeft.name : "NULL")}");
        Debug.Log($"Start Box Right: {(currentSequence.startBoxRight != null ? currentSequence.startBoxRight.name : "NULL")}");
        Debug.Log($"End Box Left: {(currentSequence.endBoxLeft != null ? currentSequence.endBoxLeft.name : "NULL")}");
        Debug.Log($"End Box Right: {(currentSequence.endBoxRight != null ? currentSequence.endBoxRight.name : "NULL")}");
        Debug.Log($"Sequence Boxes Count: {currentSequence.sequenceBoxes.Length}");

        for (int i = 0; i < currentSequence.sequenceBoxes.Length; i++)
        {
            var box = currentSequence.sequenceBoxes[i];
            if (box != null)
            {
                Debug.Log($"  Box {i}: {box.name} - Tag: '{box.tag}'");
            }
            else
            {
                Debug.Log($"  Box {i}: NULL");
            }
        }


        bool isOneHanded = IsOneHandedSequence(currentSequence, out bool isLeftHandOnly, out bool isRightHandOnly);
        Debug.Log($"One-handed: {isOneHanded}, LeftOnly: {isLeftHandOnly}, RightOnly: {isRightHandOnly}");

        List<Transform> leftHandPath = new List<Transform>();
        List<Transform> rightHandPath = new List<Transform>();


        if (currentSequence.startBoxLeft != null)
        {
            leftHandPath.Add(currentSequence.startBoxLeft.transform);
            Debug.Log($"Added LEFT start box: {currentSequence.startBoxLeft.name}");
        }
        if (currentSequence.startBoxRight != null)
        {
            rightHandPath.Add(currentSequence.startBoxRight.transform);
            Debug.Log($"Added RIGHT start box: {currentSequence.startBoxRight.name}");
        }


        foreach (var box in currentSequence.sequenceBoxes)
        {
            if (box != null)
            {
                Debug.Log($"Processing box: {box.name} with tag: '{box.tag}'");

                if (box.CompareTag("Left Baton") || box.CompareTag("Left Hand"))
                {
                    leftHandPath.Add(box.transform);
                    Debug.Log($"Added to LEFT path (tagged): {box.name}");
                }
                else if (box.CompareTag("Right Baton") || box.CompareTag("Right Hand"))
                {
                    rightHandPath.Add(box.transform);
                    Debug.Log($"Added to RIGHT path (tagged): {box.name}");
                }
                else
                {
                    Debug.Log($"Untagged box detected: {box.name}");

                    if (isOneHanded)
                    {
                        if (isLeftHandOnly)
                        {
                            leftHandPath.Add(box.transform);
                            Debug.Log($"Added to LEFT path (one-handed): {box.name}");
                        }
                        else if (isRightHandOnly)
                        {
                            rightHandPath.Add(box.transform);
                            Debug.Log($"Added to RIGHT path (one-handed): {box.name}");
                        }
                        else
                        {
                            leftHandPath.Add(box.transform);
                            rightHandPath.Add(box.transform);
                            Debug.Log($"Added to BOTH paths (fallback): {box.name}");
                        }
                    }
                    else
                    {

                        leftHandPath.Add(box.transform);
                        rightHandPath.Add(box.transform);
                        Debug.Log($"Added to BOTH paths (two-handed): {box.name}");
                    }
                }
            }
        }


        if (currentSequence.endBoxLeft != null)
        {
            leftHandPath.Add(currentSequence.endBoxLeft.transform);
            Debug.Log($"Added LEFT end box: {currentSequence.endBoxLeft.name}");
        }
        if (currentSequence.endBoxRight != null)
        {
            rightHandPath.Add(currentSequence.endBoxRight.transform);
            Debug.Log($"Added RIGHT end box: {currentSequence.endBoxRight.name}");
        }

        Debug.Log($"=== FINAL PATH COUNTS ===");
        Debug.Log($"Left Hand Path: {leftHandPath.Count} waypoints");
        for (int i = 0; i < leftHandPath.Count; i++)
        {
            Debug.Log($"  Left {i}: {leftHandPath[i].name}");
        }
        Debug.Log($"Right Hand Path: {rightHandPath.Count} waypoints");
        for (int i = 0; i < rightHandPath.Count; i++)
        {
            Debug.Log($"  Right {i}: {rightHandPath[i].name}");
        }


        if (leftHandPath.Count > 0)
        {
            GeneratePathForBaton(0, leftHandPath.ToArray());
        }
        else
        {

            leftHandBaton.batonPath = new BatonKeyframe[0];
            leftHandBaton.totalPathDistance = 0f;
            Debug.Log("Cleared left hand baton path");
        }

        if (rightHandPath.Count > 0)
        {
            GeneratePathForBaton(1, rightHandPath.ToArray());
        }
        else
        {

            rightHandBaton.batonPath = new BatonKeyframe[0];
            rightHandBaton.totalPathDistance = 0f;
            Debug.Log("Cleared right hand baton path");
        }


        if (synchronizeBatons && leftHandPath.Count > 0 && rightHandPath.Count > 0)
        {
            CalculateSynchronizedSpeeds();
        }

        Debug.Log($"Generated paths - Left: {leftHandPath.Count} waypoints, Right: {rightHandPath.Count} waypoints");
        if (isOneHanded)
        {
            Debug.Log($"One-handed sequence detected: {(isLeftHandOnly ? "Left Hand Only" : "Right Hand Only")}");
        }
    }

    
    [Header("Curve Settings")]
    [Tooltip("Enable smooth curved paths between waypoints")]
    public bool useSmoothCurve = true;
    [Tooltip("Number of interpolated points between each pair of waypoints")]
    [Range(5, 50)]
    public int curveResolution = 20;
    [Tooltip("Curve smoothness factor (0 = sharp corners, 1 = very smooth)")]
    [Range(0f, 1f)]
    public float curveSmoothness = 0.3f;

    [System.NonSerialized] public BatonKeyframe[] smoothedPath; 

    
    void GeneratePathForBaton(int batonIndex, Transform[] pathTransforms)
    {
        if (batonIndex >= batonConfigs.Length || pathTransforms == null || pathTransforms.Length == 0)
        {
            Debug.LogWarning($"Cannot generate path for baton {batonIndex}");
            return;
        }

        var config = batonConfigs[batonIndex];
        config.batonPath = new BatonKeyframe[pathTransforms.Length];

        for (int i = 0; i < pathTransforms.Length; i++)
        {
            if (pathTransforms[i] != null)
            {
                config.batonPath[i] = new BatonKeyframe(pathTransforms[i]);
            }
        }

        
        if (config.useSmoothCurve && config.batonPath.Length >= 2)
        {
            GenerateSmoothCurve(batonIndex);
        }
        else
        {
            
            config.smoothedPath = config.batonPath;
        }

        CalculatePathDistances(batonIndex);

        if (config.batonPrefab != null && config.batonInstance == null &&
            (!hideBatonsWhenNoSequence || currentSequence != null))
        {
            CreateBatonInstance(batonIndex);
        }
    }

    
    void GenerateSmoothCurve(int batonIndex)
    {
        if (batonIndex >= batonConfigs.Length) return;

        var config = batonConfigs[batonIndex];
        if (config.batonPath == null || config.batonPath.Length < 2) return;

        List<BatonKeyframe> smoothedPoints = new List<BatonKeyframe>();

        for (int i = 0; i < config.batonPath.Length - 1; i++)
        {
            Vector3 p0 = i > 0 ? config.batonPath[i - 1].position : config.batonPath[i].position;
            Vector3 p1 = config.batonPath[i].position;
            Vector3 p2 = config.batonPath[i + 1].position;
            Vector3 p3 = i < config.batonPath.Length - 2 ? config.batonPath[i + 2].position : config.batonPath[i + 1].position;

            Quaternion q1 = config.batonPath[i].rotation;
            Quaternion q2 = config.batonPath[i + 1].rotation;

            
            if (i == 0)
            {
                smoothedPoints.Add(config.batonPath[i]);
            }

            
            for (int j = 1; j <= config.curveResolution; j++)
            {
                float t = (float)j / config.curveResolution;

                Vector3 curvePos = CatmullRomSpline(p0, p1, p2, p3, t, config.curveSmoothness);
                Quaternion curveRot = Quaternion.Slerp(q1, q2, t);

                smoothedPoints.Add(new BatonKeyframe(curvePos, curveRot));
            }
        }

        config.smoothedPath = smoothedPoints.ToArray();
    }

    
    Vector3 CatmullRomSpline(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t, float smoothness)
    {
        
        Vector3 c1 = p1 + smoothness * (p2 - p0) * 0.5f;
        Vector3 c2 = p2 - smoothness * (p3 - p1) * 0.5f;

        
        float u = 1f - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 point = uuu * p1;
        point += 3 * uu * t * c1;
        point += 3 * u * tt * c2;
        point += ttt * p2;

        return point;
    }


    void CalculateSynchronizedSpeeds()
    {
        if (!synchronizeBatons || batonConfigs.Length < 2) return;

        float leftDistance = leftHandBaton.totalPathDistance;
        float rightDistance = rightHandBaton.totalPathDistance;

        if (leftDistance <= 0 || rightDistance <= 0) return;

        float targetDuration;

        if (useSlowerBatonDuration)
        {
            float leftNaturalDuration = leftDistance / leftHandBaton.constantSpeed;
            float rightNaturalDuration = rightDistance / rightHandBaton.constantSpeed;
            targetDuration = Mathf.Max(leftNaturalDuration, rightNaturalDuration);
        }
        else
        {
            targetDuration = synchronizedDuration;
        }

        leftHandBaton.effectiveSpeed = leftDistance / targetDuration;
        rightHandBaton.effectiveSpeed = rightDistance / targetDuration;

        Debug.Log($"Synchronization calculated - Target Duration: {targetDuration:F2}s");
        Debug.Log($"Left: {leftDistance:F2}u @ {leftHandBaton.effectiveSpeed:F2}u/s");
        Debug.Log($"Right: {rightDistance:F2}u @ {rightHandBaton.effectiveSpeed:F2}u/s");
    }

    void OnStanceChanged(string newStance)
    {

    }

    void InitializeBaton(int batonIndex)
    {
        if (batonIndex >= batonConfigs.Length) return;

        var config = batonConfigs[batonIndex];

        if (config.batonPrefab != null)
        {
            CreateBatonInstance(batonIndex);


            if (hideBatonsWhenNoSequence && (currentSequence == null || StanceManager.Instance?.currentAttackSequence == null))
            {
                HideBaton(batonIndex);
            }
        }
    }

    void UpdateBaton(int batonIndex)
{
    if (batonIndex >= batonConfigs.Length) return;

    var config = batonConfigs[batonIndex];

    
    if (autoDetectSequences && !IsSequenceValid(currentSequence))
    {
        if (config.isPlaying)
        {
            StopBaton(batonIndex);
        }
        
        if (config.batonInstance != null && config.batonInstance.activeSelf)
        {
            config.batonInstance.SetActive(false);
        }
        return;
    }

    if (!config.isPlaying || config.batonPath == null || config.batonPath.Length < 2 || config.totalPathDistance <= 0f)
    {
        if (config.batonInstance != null)
        {
            config.batonInstance.SetActive(false);
        }
        return;
    }

    if (config.batonInstance != null && !config.batonInstance.activeSelf && config.showBaton)
    {
        config.batonInstance.SetActive(true);
    }

    if (config.isWaitingForLoop)
    {
        config.loopDelayTimer -= Time.deltaTime;

        if (config.loopDelayTimer <= 0f)
        {
            config.isWaitingForLoop = false;
            config.currentDistance = 0f;
            config.currentIndex = 0;

            if (config.wasHiddenForDelay && config.batonInstance != null)
            {
                config.batonInstance.SetActive(true);
                config.wasHiddenForDelay = false;
            }

            if (config.batonInstance != null)
            {
                config.batonInstance.transform.position = config.batonPath[0].position;
                config.batonInstance.transform.rotation = config.batonPath[0].rotation;
            }

            if (config.batonTrail != null)
            {
                config.batonTrail.Clear();
            }
        }
        return;
    }

    UpdateKeyframesFromSources(batonIndex);
    CalculatePathDistances(batonIndex);

    float speedToUse = synchronizeBatons ? config.effectiveSpeed : config.constantSpeed;
    config.currentDistance += speedToUse * Time.deltaTime;
    UpdateBatonPosition(batonIndex);
}


    void CalculatePathDistances(int batonIndex)
    {
        if (batonIndex >= batonConfigs.Length) return;

        var config = batonConfigs[batonIndex];

        
        BatonKeyframe[] pathToCalculate = config.smoothedPath ?? config.batonPath;

        if (pathToCalculate == null || pathToCalculate.Length < 2) return;

        config.segmentDistances = new float[pathToCalculate.Length - 1];
        config.cumulativeDistances = new float[pathToCalculate.Length];
        config.totalPathDistance = 0f;
        config.cumulativeDistances[0] = 0f;

        for (int i = 0; i < pathToCalculate.Length - 1; i++)
        {
            config.segmentDistances[i] = Vector3.Distance(pathToCalculate[i].position, pathToCalculate[i + 1].position);
            config.totalPathDistance += config.segmentDistances[i];
            config.cumulativeDistances[i + 1] = config.totalPathDistance;
        }
    }


void CreateBatonInstance(int batonIndex)
{
    if (batonIndex >= batonConfigs.Length) return;

    var config = batonConfigs[batonIndex];

    if (config.batonPrefab == null) return;

    if (config.batonInstance != null)
    {
        if (Application.isPlaying)
            Destroy(config.batonInstance);
        else
            DestroyImmediate(config.batonInstance);
    }

    config.batonInstance = Instantiate(config.batonPrefab);
    config.batonInstance.name = config.batonName + " (Baton Guide)";

    if (config.batonPath != null && config.batonPath.Length > 0)
    {
        config.batonInstance.transform.position = config.batonPath[0].position;
        config.batonInstance.transform.rotation = config.batonPath[0].rotation;
    }

    
    InitializeTrail(batonIndex);
}


    void UpdateKeyframesFromSources(int batonIndex)
    {
        if (batonIndex >= batonConfigs.Length) return;

        var config = batonConfigs[batonIndex];

        for (int i = 0; i < config.batonPath.Length; i++)
        {
            if (config.batonPath[i].SourceTransform != null)
            {
                var keyframe = config.batonPath[i];
                keyframe.position = keyframe.SourceTransform.position;
                keyframe.rotation = keyframe.SourceTransform.rotation;
                config.batonPath[i] = keyframe;
            }
        }
    }

    void UpdateBatonPosition(int batonIndex)
    {
        if (batonIndex >= batonConfigs.Length) return;

        var config = batonConfigs[batonIndex];

        
        BatonKeyframe[] pathToUse = config.smoothedPath ?? config.batonPath;

        if (config.currentDistance >= config.totalPathDistance)
        {
            if (config.loop)
            {
                config.isWaitingForLoop = true;
                config.loopDelayTimer = config.loopDelay;

                if (config.hideDuringDelay && config.batonInstance != null)
                {
                    config.batonInstance.SetActive(false);
                    config.wasHiddenForDelay = true;
                }

                return;
            }
            else
            {
                if (config.batonInstance != null)
                {
                    config.batonInstance.transform.position = pathToUse[pathToUse.Length - 1].position;
                    config.batonInstance.transform.rotation = pathToUse[pathToUse.Length - 1].rotation;
                }
                return;
            }
        }

        while (config.currentIndex < pathToUse.Length - 1 &&
               config.currentDistance >= config.cumulativeDistances[config.currentIndex + 1])
        {
            config.currentIndex++;
        }

        if (config.currentIndex >= pathToUse.Length - 1)
            config.currentIndex = pathToUse.Length - 2;

        float segmentStartDistance = config.cumulativeDistances[config.currentIndex];
        float segmentEndDistance = config.cumulativeDistances[config.currentIndex + 1];
        float segmentProgress = Mathf.Clamp01((config.currentDistance - segmentStartDistance) / (segmentEndDistance - segmentStartDistance));

        BatonKeyframe current = pathToUse[config.currentIndex];
        BatonKeyframe next = pathToUse[config.currentIndex + 1];

        Vector3 newPosition = Vector3.Lerp(current.position, next.position, segmentProgress);
        Quaternion newRotation = Quaternion.Slerp(current.rotation, next.rotation, segmentProgress);

        if (config.batonInstance != null && config.showBaton)
        {
            config.batonInstance.transform.position = newPosition;
            config.batonInstance.transform.rotation = newRotation;

            if (config.batonTrail != null)
            {
                UpdateBatonTrail(batonIndex);
            }
        }
    }

void UpdateBatonTrail(int batonIndex)
{
    if (batonIndex >= batonConfigs.Length) return;

    var config = batonConfigs[batonIndex];
    
    if (!config.enableTrail || config.batonTrail == null || config.batonInstance == null)
        return;

    
    config.batonTrail.enabled = true;
}

void InitializeTrail(int batonIndex)
{
    if (batonIndex >= batonConfigs.Length) return;

    var config = batonConfigs[batonIndex];
    
    if (config.batonInstance != null)
    {
        
        config.batonTrail = config.batonInstance.GetComponent<TrailRenderer>();
        
        if (config.batonTrail == null)
        {
            config.batonTrail = config.batonInstance.AddComponent<TrailRenderer>();
        }

        
        config.batonTrail.time = config.trailTime;
        config.batonTrail.startWidth = config.trailWidth;
        config.batonTrail.endWidth = config.trailWidth * 0.1f;
        config.batonTrail.enabled = config.enableTrail;
        
        
        if (config.trailMaterial != null)
        {
            config.batonTrail.material = config.trailMaterial;
        }
        else
        {
            
            Material defaultMaterial = new Material(Shader.Find("Sprites/Default"));
            defaultMaterial.color = config.isLeftHand ? Color.yellow : Color.cyan;
            config.batonTrail.material = defaultMaterial;
        }

        
        if (config.trailColorGradient.colorKeys.Length == 0)
        {
            
            Color trailColor = config.isLeftHand ? Color.yellow : Color.cyan;
            config.trailColorGradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(trailColor, 0.0f), new GradientColorKey(trailColor, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
            );
        }
        config.batonTrail.colorGradient = config.trailColorGradient;
    }
}


    public void StartBaton(int batonIndex)
{
    if (batonIndex >= batonConfigs.Length) return;

    var config = batonConfigs[batonIndex];

    if (config.batonPath == null || config.batonPath.Length < 2 || config.totalPathDistance <= 0f)
    {
        Debug.Log($"Cannot start {config.batonName} - no valid path available");
        return;
    }

    if (config.batonInstance == null && config.batonPrefab != null)
        CreateBatonInstance(batonIndex);

    if (config.batonInstance != null)
    {
        config.batonInstance.SetActive(true);
    }

    config.isPlaying = true;
    config.currentDistance = 0f;
    config.currentIndex = 0;

    config.isWaitingForLoop = false;
    config.loopDelayTimer = 0f;
    config.wasHiddenForDelay = false;

    if (config.batonInstance != null && config.batonPath != null && config.batonPath.Length > 0)
    {
        config.batonInstance.transform.position = config.batonPath[0].position;
        config.batonInstance.transform.rotation = config.batonPath[0].rotation;
    }

    
    ClearTrail(batonIndex);

    Debug.Log($"Started {config.batonName} with {config.batonPath.Length} waypoints");
}

    public void StopBaton(int batonIndex)
    {
        if (batonIndex >= batonConfigs.Length) return;

        var config = batonConfigs[batonIndex];
        config.isPlaying = false;


        config.isWaitingForLoop = false;
        config.loopDelayTimer = 0f;
        config.wasHiddenForDelay = false;


        if (hideBatonsWhenNoSequence && (currentSequence == null || StanceManager.Instance?.currentAttackSequence == null))
        {
            HideBaton(batonIndex);
        }
    }
    
    public void ClearTrail(int batonIndex)
    {
        if (batonIndex >= batonConfigs.Length) return;

        var config = batonConfigs[batonIndex];
        
        if (config.batonTrail != null)
        {
            config.batonTrail.Clear();
        }
    }

    public void ResetBaton(int batonIndex)
    {
        if (batonIndex >= batonConfigs.Length) return;

        var config = batonConfigs[batonIndex];

        config.currentDistance = 0f;
        config.currentIndex = 0;

        config.isWaitingForLoop = false;
        config.loopDelayTimer = 0f;
        
        
        ClearTrail(batonIndex);

        if (config.wasHiddenForDelay && config.batonInstance != null)
        {
            config.batonInstance.SetActive(true);
            config.wasHiddenForDelay = false;
        }

        if (config.batonInstance != null && config.batonPath != null && config.batonPath.Length > 0)
        {
            config.batonInstance.transform.position = config.batonPath[0].position;
            config.batonInstance.transform.rotation = config.batonPath[0].rotation;
        }
    }

    public void StartAllBatons()
    {

        bool leftHasPath = leftHandBaton.batonPath != null && leftHandBaton.batonPath.Length >= 2 && leftHandBaton.totalPathDistance > 0f;
        bool rightHasPath = rightHandBaton.batonPath != null && rightHandBaton.batonPath.Length >= 2 && rightHandBaton.totalPathDistance > 0f;

        if (synchronizeBatons && leftHasPath && rightHasPath)
        {
            CalculateSynchronizedSpeeds();
        }

        for (int i = 0; i < batonConfigs.Length; i++)
        {
            if (batonConfigs[i].autoPlay)
            {
                StartBaton(i);
            }
        }
    }

    public void StopAllBatons()
    {
        for (int i = 0; i < batonConfigs.Length; i++)
        {
            StopBaton(i);
        }
    }

    public void ResetAllBatons()
    {
        for (int i = 0; i < batonConfigs.Length; i++)
        {
            ResetBaton(i);
        }
    }

    public float GetProgressPercentage(int batonIndex)
    {
        if (batonIndex >= batonConfigs.Length) return 0f;

        var config = batonConfigs[batonIndex];
        return config.totalPathDistance > 0 ? config.currentDistance / config.totalPathDistance : 0f;
    }

    public GameObject GetBatonInstance(int batonIndex)
    {
        if (batonIndex >= batonConfigs.Length) return null;

        return batonConfigs[batonIndex].batonInstance;
    }

    public void EnableSynchronization(bool enable)
    {
        synchronizeBatons = enable;
        if (enable)
        {
            CalculateSynchronizedSpeeds();
        }
    }

    public void SetSynchronizedDuration(float duration)
    {
        synchronizedDuration = duration;
        if (synchronizeBatons)
        {
            CalculateSynchronizedSpeeds();
        }
    }

    public float GetEstimatedCompletionTime(int batonIndex)
    {
        if (batonIndex >= batonConfigs.Length) return 0f;

        var config = batonConfigs[batonIndex];
        if (config.totalPathDistance <= 0) return 0f;

        float speedToUse = synchronizeBatons ? config.effectiveSpeed : config.constantSpeed;
        float baseTime = config.totalPathDistance / speedToUse;


        if (config.loop)
        {
            baseTime += config.loopDelay;
        }

        return baseTime;
    }


    public void SetLoopDelay(int batonIndex, float delay)
    {
        if (batonIndex >= batonConfigs.Length) return;
        batonConfigs[batonIndex].loopDelay = delay;
    }

    public void SetHideDuringDelay(int batonIndex, bool hide)
    {
        if (batonIndex >= batonConfigs.Length) return;
        batonConfigs[batonIndex].hideDuringDelay = hide;
    }

    public bool IsWaitingForLoop(int batonIndex)
    {
        if (batonIndex >= batonConfigs.Length) return false;
        return batonConfigs[batonIndex].isWaitingForLoop;
    }

    public float GetRemainingDelayTime(int batonIndex)
    {
        if (batonIndex >= batonConfigs.Length) return 0f;
        var config = batonConfigs[batonIndex];
        return config.isWaitingForLoop ? config.loopDelayTimer : 0f;
    }


    public void SetHideBatonsWhenNoSequence(bool hide)
    {
        hideBatonsWhenNoSequence = hide;


        if (hide && (currentSequence == null || StanceManager.Instance?.currentAttackSequence == null))
        {
            HideAllBatons();
        }
        else if (!hide)
        {
            ShowAllBatons();
        }
    }

    public void StartGuide() => StartBaton(0);
    public void StopGuide() => StopBaton(0);
    public void ResetGuide() => ResetBaton(0);
    public float GetProgressPercentage() => GetProgressPercentage(0);
    public GameObject GetBatonInstance() => GetBatonInstance(0);

    void OnDestroy()
    {
        if (StanceManager.Instance != null)
        {
            StanceManager.Instance.OnStanceChanged -= OnStanceChanged;
        }

        for (int i = 0; i < batonConfigs.Length; i++)
        {
            var config = batonConfigs[i];
            if (config.batonInstance != null)
            {
                if (Application.isPlaying)
                    Destroy(config.batonInstance);
                else
                    DestroyImmediate(config.batonInstance);
            }
        }
    }

    void EditorForceGeneratePaths()
    {
        if (StanceManager.Instance != null && StanceManager.Instance.currentAttackSequence != null)
        {
            currentSequence = StanceManager.Instance.currentAttackSequence;
            GeneratePathsFromCurrentSequence();
        }

    }

    [ContextMenu("Start All Batons")]
    void EditorStartAllBatons()
    {
        StartAllBatons();
    }

    [ContextMenu("Toggle Synchronization")]
    void EditorToggleSync()
    {
        EnableSynchronization(!synchronizeBatons);
    }

    [ContextMenu("Toggle Hide Batons When No Sequence")]
    void EditorToggleHideBatons()
    {
        SetHideBatonsWhenNoSequence(!hideBatonsWhenNoSequence);
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Color[] colors = { Color.yellow, Color.cyan };

        for (int batonIndex = 0; batonIndex < batonConfigs.Length; batonIndex++)
        {
            var config = batonConfigs[batonIndex];

            
            BatonKeyframe[] pathToDraw = config.smoothedPath ?? config.batonPath;

            if (pathToDraw == null || pathToDraw.Length < 2) continue;

            Color batonColor = colors[batonIndex % colors.Length];

            
            Gizmos.color = batonColor;
            for (int i = 0; i < pathToDraw.Length - 1; i++)
            {
                Gizmos.DrawLine(pathToDraw[i].position, pathToDraw[i + 1].position);
            }

            
            if (config.batonPath != null && config.useSmoothCurve)
            {
                Gizmos.color = Color.Lerp(batonColor, Color.white, 0.5f);
                for (int i = 0; i < config.batonPath.Length; i++)
                {
                    Gizmos.DrawWireSphere(config.batonPath[i].position, 0.15f);
                }
            }

            
            Gizmos.color = Color.Lerp(batonColor, Color.red, 0.5f);
            for (int i = 0; i < pathToDraw.Length; i++)
            {
                float sphereSize = config.useSmoothCurve ? 0.05f : 0.1f;
                Gizmos.DrawWireSphere(pathToDraw[i].position, sphereSize);
            }

            if (synchronizeBatons && Application.isPlaying)
            {
                Vector3 labelPos = pathToDraw.Length > 0 ? pathToDraw[0].position + Vector3.up * 0.5f : Vector3.zero;
#if UNITY_EDITOR
                string label = $"{config.batonName}\nSpeed: {config.effectiveSpeed:F1}";
                if (config.useSmoothCurve)
                {
                    label += $"\nSmooth: {config.curveResolution} pts";
                }
                if (config.isWaitingForLoop)
                {
                    label += $"\nLoop Delay: {config.loopDelayTimer:F1}s";
                }
                UnityEditor.Handles.Label(labelPos, label);
#endif
            }
        }
    }
}