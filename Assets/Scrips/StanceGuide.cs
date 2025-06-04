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

    [System.Serializable]
    public class BatonConfig
    {
        [Header("Baton Identity")]
        public string batonName = "Baton";
        public GameObject batonPrefab;
        public bool showBaton = true;
        public LineRenderer batonTrail;
        
        [Header("Path Configuration")]
        public BatonKeyframe[] batonPath;
        public bool loop = false;
        public bool autoPlay = true;
        public float constantSpeed = 5f;
        
        [Header("Hand Assignment")]
        public bool isLeftHand = true;
        
        [System.NonSerialized] public float totalPathDistance = 0f;
        [System.NonSerialized] public float[] segmentDistances;
        [System.NonSerialized] public float[] cumulativeDistances;
        [System.NonSerialized] public float currentDistance = 0f;
        [System.NonSerialized] public int currentIndex = 0;
        [System.NonSerialized] public bool isPlaying = false;
        [System.NonSerialized] public GameObject batonInstance;
        [System.NonSerialized] public float effectiveSpeed = 5f; 
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
    }
    
    void Start()
    {
        if (StanceManager.Instance != null)
        {
            StanceManager.Instance.OnStanceChanged += OnStanceChanged;
        }
        
        for (int i = 0; i < batonConfigs.Length; i++)
        {
            InitializeBaton(i);
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
    
    void CheckForSequenceChanges()
    {
        if (StanceManager.Instance == null) return;
        
        AttackSequence newSequence = StanceManager.Instance.currentAttackSequence;
        bool isSequenceActive = newSequence != null;
        
        if (isSequenceActive && !wasSequenceActive)
        {
            currentSequence = newSequence;
            GeneratePathsFromCurrentSequence();
            
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
            currentSequence = null;
        }
        else if (isSequenceActive && currentSequence != newSequence)
        {
            currentSequence = newSequence;
            GeneratePathsFromCurrentSequence();
            ResetAllBatons();
            
            if (startOnSequenceBegin)
            {
                StartAllBatons();
            }
        }
        
        wasSequenceActive = isSequenceActive;
    }
    

    void GeneratePathsFromCurrentSequence()
    {
        if (currentSequence == null || currentSequence.sequenceBoxes == null || currentSequence.sequenceBoxes.Length == 0)
        {
            return;
        }
        
        List<Transform> leftHandPath = new List<Transform>();
        List<Transform> rightHandPath = new List<Transform>();
        
        if (currentSequence.startBoxLeft != null)
            leftHandPath.Add(currentSequence.startBoxLeft.transform);
        if (currentSequence.startBoxRight != null)
            rightHandPath.Add(currentSequence.startBoxRight.transform);
        
        foreach (var box in currentSequence.sequenceBoxes)
        {
            if (box != null)
            {
                if (box.CompareTag("Left Baton") || box.CompareTag("Left Hand"))
                {
                    leftHandPath.Add(box.transform);
                }
                else if (box.CompareTag("Right Baton") || box.CompareTag("Right Hand"))
                {
                    rightHandPath.Add(box.transform);
                }
                else
                {
                    leftHandPath.Add(box.transform);
                    rightHandPath.Add(box.transform);
                }
            }
        }
        
        if (currentSequence.endBoxLeft != null)
            leftHandPath.Add(currentSequence.endBoxLeft.transform);
        if (currentSequence.endBoxRight != null)
            rightHandPath.Add(currentSequence.endBoxRight.transform);
        
        GeneratePathForBaton(0, leftHandPath.ToArray());  
        GeneratePathForBaton(1, rightHandPath.ToArray());
        
        if (synchronizeBatons)
        {
            CalculateSynchronizedSpeeds();
        }
        
        
        string leftBoxNames = string.Join(", ", leftHandPath.Select(t => t.name));
        string rightBoxNames = string.Join(", ", rightHandPath.Select(t => t.name));
    }

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
        
        CalculatePathDistances(batonIndex);
        
        if (config.batonPrefab != null && config.batonInstance == null)
        {
            CreateBatonInstance(batonIndex);
        }
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
        }
    }
    
    void UpdateBaton(int batonIndex)
    {
        if (batonIndex >= batonConfigs.Length) return;
        
        var config = batonConfigs[batonIndex];
        
        if (!config.isPlaying || config.batonPath == null || config.batonPath.Length < 2) return;
        
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
        
        if (config.batonPath == null || config.batonPath.Length < 2) return;
        
        config.segmentDistances = new float[config.batonPath.Length - 1];
        config.cumulativeDistances = new float[config.batonPath.Length];
        config.totalPathDistance = 0f;
        config.cumulativeDistances[0] = 0f;
        
        for (int i = 0; i < config.batonPath.Length - 1; i++)
        {
            config.segmentDistances[i] = Vector3.Distance(config.batonPath[i].position, config.batonPath[i + 1].position);
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
        
        if (config.batonTrail == null)
        {
            config.batonTrail = config.batonInstance.GetComponent<LineRenderer>() ?? 
                              config.batonInstance.GetComponentInChildren<LineRenderer>();
        }
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
        
        if (config.currentDistance >= config.totalPathDistance)
        {
            if (config.loop)
            {
                config.currentDistance = 0f;
                config.currentIndex = 0;
                return;
            }
            else
            {
                if (config.batonInstance != null)
                {
                    config.batonInstance.transform.position = config.batonPath[config.batonPath.Length - 1].position;
                    config.batonInstance.transform.rotation = config.batonPath[config.batonPath.Length - 1].rotation;
                }
                return;
            }
        }
        
        while (config.currentIndex < config.batonPath.Length - 1 && 
               config.currentDistance >= config.cumulativeDistances[config.currentIndex + 1])
        {
            config.currentIndex++;
        }
        
        if (config.currentIndex >= config.batonPath.Length - 1)
            config.currentIndex = config.batonPath.Length - 2;
        
        float segmentStartDistance = config.cumulativeDistances[config.currentIndex];
        float segmentEndDistance = config.cumulativeDistances[config.currentIndex + 1];
        float segmentProgress = Mathf.Clamp01((config.currentDistance - segmentStartDistance) / (segmentEndDistance - segmentStartDistance));
        
        BatonKeyframe current = config.batonPath[config.currentIndex];
        BatonKeyframe next = config.batonPath[config.currentIndex + 1];
        
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
        
        Vector3 batonTip = config.batonInstance.transform.position + config.batonInstance.transform.forward * 1f;
        
        if (config.batonTrail.positionCount == 0)
        {
            config.batonTrail.positionCount = 1;
            config.batonTrail.SetPosition(0, batonTip);
        }
        else
        {
            config.batonTrail.positionCount++;
            config.batonTrail.SetPosition(config.batonTrail.positionCount - 1, batonTip);
        }
        
        if (config.batonTrail.positionCount > 100)
        {
            Vector3[] positions = new Vector3[config.batonTrail.positionCount];
            config.batonTrail.GetPositions(positions);
            
            Vector3[] newPositions = new Vector3[99];
            System.Array.Copy(positions, 1, newPositions, 0, 99);
            
            config.batonTrail.positionCount = 99;
            config.batonTrail.SetPositions(newPositions);
        }
    }
    
    public void StartBaton(int batonIndex)
    {
        if (batonIndex >= batonConfigs.Length) return;
        
        var config = batonConfigs[batonIndex];
        
        if (config.batonInstance == null && config.batonPrefab != null)
            CreateBatonInstance(batonIndex);
        
        config.isPlaying = true;
        config.currentDistance = 0f;
        config.currentIndex = 0;
        
        if (config.batonInstance != null && config.batonPath != null && config.batonPath.Length > 0)
        {
            config.batonInstance.transform.position = config.batonPath[0].position;
            config.batonInstance.transform.rotation = config.batonPath[0].rotation;
        }
        
        if (config.batonTrail != null)
            config.batonTrail.positionCount = 0;
    }
    
    public void StopBaton(int batonIndex)
    {
        if (batonIndex >= batonConfigs.Length) return;
        
        batonConfigs[batonIndex].isPlaying = false;
    }
    
    public void ResetBaton(int batonIndex)
    {
        if (batonIndex >= batonConfigs.Length) return;
        
        var config = batonConfigs[batonIndex];
        
        config.currentDistance = 0f;
        config.currentIndex = 0;
        
        if (config.batonInstance != null && config.batonPath != null && config.batonPath.Length > 0)
        {
            config.batonInstance.transform.position = config.batonPath[0].position;
            config.batonInstance.transform.rotation = config.batonPath[0].rotation;
        }
        
        if (config.batonTrail != null)
            config.batonTrail.positionCount = 0;
    }
    
    public void StartAllBatons()
    {
        if (synchronizeBatons)
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
        return config.totalPathDistance / speedToUse;
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
        Debug.Log($"Synchronization {(synchronizeBatons ? "enabled" : "disabled")}");
    }
    
    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        Color[] colors = { Color.yellow, Color.cyan };
        
        for (int batonIndex = 0; batonIndex < batonConfigs.Length; batonIndex++)
        {
            var config = batonConfigs[batonIndex];
            
            if (config.batonPath == null || config.batonPath.Length < 2) continue;
            
            Color batonColor = colors[batonIndex % colors.Length];
            
            Gizmos.color = batonColor;
            for (int i = 0; i < config.batonPath.Length - 1; i++)
            {
                Gizmos.DrawLine(config.batonPath[i].position, config.batonPath[i + 1].position);
            }
            
            Gizmos.color = Color.Lerp(batonColor, Color.red, 0.5f);
            for (int i = 0; i < config.batonPath.Length; i++)
            {
                Gizmos.DrawWireSphere(config.batonPath[i].position, 0.1f);
            }
            
            if (synchronizeBatons && Application.isPlaying)
            {
                Vector3 labelPos = config.batonPath.Length > 0 ? config.batonPath[0].position + Vector3.up * 0.5f : Vector3.zero;
#if UNITY_EDITOR
                UnityEditor.Handles.Label(labelPos, $"{config.batonName}\nSpeed: {config.effectiveSpeed:F1}");
#endif
            }
        }
    }
}