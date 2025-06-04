using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BatonGuide : MonoBehaviour
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
        public bool loop = true;
        public bool autoPlay = true;
        public float constantSpeed = 5f;
        
        [Header("Object References")]
        public Transform[] referenceObjects;
        
        [Header("Runtime Options")]
        public bool useRealTimeData = false;
        public bool autoGenerateFromReferences = true;
        
        [System.NonSerialized] public float totalPathDistance = 0f;
        [System.NonSerialized] public float[] segmentDistances;
        [System.NonSerialized] public float[] cumulativeDistances;
        [System.NonSerialized] public float currentDistance = 0f;
        [System.NonSerialized] public int currentIndex = 0;
        [System.NonSerialized] public bool isPlaying = false;
        [System.NonSerialized] public GameObject batonInstance;
    }
    
    [Header("Multi-Baton Configuration")]
    public BatonConfig[] batonConfigs = new BatonConfig[2];
    
    [Header("Global Settings")]
    public bool startAllBatonsOnPlay = true;
    
    void Start()
    {
        for (int i = 0; i < batonConfigs.Length; i++)
        {
            InitializeBaton(i);
        }
        
        if (startAllBatonsOnPlay)
        {
            StartAllBatons();
        }
    }
    
    void Update()
    {
        for (int i = 0; i < batonConfigs.Length; i++)
        {
            UpdateBaton(i);
        }
    }
    
    void InitializeBaton(int batonIndex)
    {
        if (batonIndex >= batonConfigs.Length) return;
        
        var config = batonConfigs[batonIndex];
        
        if (config.autoGenerateFromReferences && config.referenceObjects != null && config.referenceObjects.Length > 0)
        {
            GeneratePathFromReferences(batonIndex);
        }
        
        CalculatePathDistances(batonIndex);
        
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
        
        if (config.useRealTimeData)
        {
            UpdateKeyframesFromSources(batonIndex);
            CalculatePathDistances(batonIndex);
        }
        
        config.currentDistance += config.constantSpeed * Time.deltaTime;
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
    
    public void GeneratePathFromReferences(int batonIndex)
    {
        if (batonIndex >= batonConfigs.Length) return;
        
        var config = batonConfigs[batonIndex];
        
        if (config.referenceObjects == null || config.referenceObjects.Length == 0)
        {
            Debug.LogWarning($"No reference objects provided for baton {batonIndex}");
            return;
        }
        
        config.batonPath = new BatonKeyframe[config.referenceObjects.Length];
        
        for (int i = 0; i < config.referenceObjects.Length; i++)
        {
            config.batonPath[i] = new BatonKeyframe(config.referenceObjects[i]);
        }
        
        CalculatePathDistances(batonIndex);
        Debug.Log($"Generated path for {config.batonName} with {config.batonPath.Length} keyframes. Total distance: {config.totalPathDistance:F2}");
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
    
    public void StartGuide() => StartBaton(0);
    public void StopGuide() => StopBaton(0);
    public void ResetGuide() => ResetBaton(0);
    public float GetProgressPercentage() => GetProgressPercentage(0);
    public GameObject GetBatonInstance() => GetBatonInstance(0);
    public void GeneratePathFromReferences() => GeneratePathFromReferences(0);
    
    void OnDestroy()
    {
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
    
    [ContextMenu("Generate All Paths from References")]
    void EditorGenerateAllFromReferences()
    {
        for (int i = 0; i < batonConfigs.Length; i++)
        {
            GeneratePathFromReferences(i);
        }
    }
    
    [ContextMenu("Create All Baton Instances")]
    void EditorCreateAllBatonInstances()
    {
        for (int i = 0; i < batonConfigs.Length; i++)
        {
            CreateBatonInstance(i);
        }
    }
    
    [ContextMenu("Start All Batons")]
    void EditorStartAllBatons()
    {
        StartAllBatons();
    }
    
    void OnDrawGizmos()
    {
        Color[] colors = { Color.yellow, Color.cyan, Color.magenta, Color.green };
        
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
        }
    }
}