using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StanceGuideSystem : MonoBehaviour
{
    [Header("Guide Prefabs")]
    [SerializeField] private GameObject leftHandGuidePrefab;
    [SerializeField] private GameObject rightHandGuidePrefab;
    [SerializeField] private GameObject batonGuidePrefab;
    
    [Header("Path Settings")]
    [SerializeField] private float pathHeight = 0.1f; 
    [SerializeField] private float guideSpeed = 2f;
    [SerializeField] private bool loopPath = true;
    [SerializeField] private float loopDelay = 1f; 
    [SerializeField] private bool showOnlyActiveSequence = true;
    
    [Header("Visual Settings")]
    [SerializeField] private AnimationCurve speedCurve = AnimationCurve.Linear(0, 1, 1, 1);
    [SerializeField] private bool fadeAtEndpoints = true;
    [SerializeField] private float fadeDistance = 0.5f;
    
    [Header("Mirroring Settings")]
    [SerializeField] private bool enableMirroring = true;
    [SerializeField] private Transform mirrorPlane;
    [SerializeField] private Vector3 mirrorNormal = Vector3.right;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = false;
    [SerializeField] private Color pathColor = Color.yellow;
    
    private Dictionary<AttackSequence, GuideInstance> activeGuides = new Dictionary<AttackSequence, GuideInstance>();
    private Dictionary<AttackSequence, GuideInstance> mirroredGuides = new Dictionary<AttackSequence, GuideInstance>();
    
    private StanceManager stanceManager;
    
    private Transform guidesParent;
    private Transform mirroredGuidesParent;
    
    [System.Serializable]
    public class GuideInstance
    {
        public AttackSequence sequence;
        public List<Vector3> pathPoints = new List<Vector3>();
        public List<GameObject> leftHandGuides = new List<GameObject>();
        public List<GameObject> rightHandGuides = new List<GameObject>();
        public List<GameObject> batonGuides = new List<GameObject>();
        public bool isActive = false;
        public Coroutine animationCoroutine;
        
        public void ClearGuides()
        {
            DestroyGuideList(leftHandGuides);
            DestroyGuideList(rightHandGuides);
            DestroyGuideList(batonGuides);
            pathPoints.Clear();
            
            if (animationCoroutine != null)
            {
                animationCoroutine = null;
            }
        }
        
        private void DestroyGuideList(List<GameObject> guides)
        {
            foreach (var guide in guides)
            {
                if (guide != null)
                {
                    if (Application.isPlaying)
                        Destroy(guide);
                    else
                        DestroyImmediate(guide);
                }
            }
            guides.Clear();
        }
    }
    
    private void Awake()
    {
        SetupParentObjects();
    }
    
    private void Start()
    {
        stanceManager = StanceManager.Instance;
        if (stanceManager != null)
        {
            stanceManager.OnStanceChanged += OnStanceChanged;
        }
        
        if (mirrorPlane == null && stanceManager != null)
        {
            mirrorPlane = stanceManager.mirrorPlane;
            mirrorNormal = stanceManager.mirrorNormal;
        }
    }
    
    private void SetupParentObjects()
    {
        GameObject guidesParentObj = GameObject.Find("StanceGuides");
        if (guidesParentObj == null)
        {
            guidesParentObj = new GameObject("StanceGuides");
            guidesParentObj.transform.SetParent(transform);
        }
        guidesParent = guidesParentObj.transform;
        
        GameObject mirroredGuidesParentObj = GameObject.Find("MirroredStanceGuides");
        if (mirroredGuidesParentObj == null)
        {
            mirroredGuidesParentObj = new GameObject("MirroredStanceGuides");
            mirroredGuidesParentObj.transform.SetParent(transform);
        }
        mirroredGuidesParent = mirroredGuidesParentObj.transform;
    }
    
    private void OnStanceChanged(string newStance)
    {
        if (showOnlyActiveSequence)
        {
            HideAllGuides();
            
            if (newStance != "Default")
            {
                ShowGuidesForStance(newStance);
            }
        }
    }
    
    public void GenerateGuidesForAllSequences()
    {
        ClearAllGuides();
        
        if (stanceManager == null) return;
        
        var arnisStyles = GetActiveArnisStyles();
        
        foreach (var style in arnisStyles)
        {
            foreach (var sequence in style.sequences)
            {
                GenerateGuideForSequence(sequence, false);
                
                if (enableMirroring && stanceManager.isRightHandDominant)
                {
                    var mirroredSequence = GetMirroredSequence(sequence);
                    if (mirroredSequence != null)
                    {
                        GenerateGuideForSequence(mirroredSequence, true);
                    }
                }
            }
        }
    }
    
    public void GenerateGuideForSequence(AttackSequence sequence, bool isMirrored = false)
    {
        if (sequence == null || sequence.sequenceBoxes == null || sequence.sequenceBoxes.Length == 0)
            return;
        
        var guideInstance = new GuideInstance();
        guideInstance.sequence = sequence;
        
        GeneratePathPoints(sequence, guideInstance);
        
        CreateGuideObjects(sequence, guideInstance, isMirrored);
        
        if (isMirrored)
            mirroredGuides[sequence] = guideInstance;
        else
            activeGuides[sequence] = guideInstance;
    }
    
    private void GeneratePathPoints(AttackSequence sequence, GuideInstance guideInstance)
    {
        var pathPoints = guideInstance.pathPoints;
        
        if (sequence.startBoxLeft != null && sequence.startBoxRight != null)
        {
            Vector3 startPos = (sequence.startBoxLeft.transform.position + sequence.startBoxRight.transform.position) * 0.5f;
            startPos.y += pathHeight;
            pathPoints.Add(startPos);
        }
        
        foreach (var box in sequence.sequenceBoxes)
        {
            if (box != null)
            {
                Vector3 boxPos = box.transform.position;
                boxPos.y += pathHeight;
                pathPoints.Add(boxPos);
            }
        }
        
        if (sequence.endBoxLeft != null && sequence.endBoxRight != null)
        {
            Vector3 endPos = (sequence.endBoxLeft.transform.position + sequence.endBoxRight.transform.position) * 0.5f;
            endPos.y += pathHeight;
            pathPoints.Add(endPos);
        }
    }
    
    private void CreateGuideObjects(AttackSequence sequence, GuideInstance guideInstance, bool isMirrored)
    {
        Transform parentTransform = isMirrored ? mirroredGuidesParent : guidesParent;
        
        CreateGuidesForHand(sequence, guideInstance, true, parentTransform); // Left hand
        CreateGuidesForHand(sequence, guideInstance, false, parentTransform); // Right hand
        
        if (HasBatonRequirement(sequence))
        {
            CreateBatonGuides(sequence, guideInstance, parentTransform);
        }
        
        SetGuideVisibility(guideInstance, false);
    }
    
    private void CreateGuidesForHand(AttackSequence sequence, GuideInstance guideInstance, bool isLeftHand, Transform parent)
    {
        GameObject prefab = isLeftHand ? leftHandGuidePrefab : rightHandGuidePrefab;
        List<GameObject> guideList = isLeftHand ? guideInstance.leftHandGuides : guideInstance.rightHandGuides;
        
        if (prefab == null) return;
        
        foreach (var point in guideInstance.pathPoints)
        {
            GameObject guide = Instantiate(prefab, point, Quaternion.identity, parent);
            guide.name = $"{sequence.sequenceName}_{(isLeftHand ? "Left" : "Right")}_Guide_{guideList.Count}";
            guideList.Add(guide);
        }
    }
    
    private void CreateBatonGuides(AttackSequence sequence, GuideInstance guideInstance, Transform parent)
    {
        if (batonGuidePrefab == null) return;
        
        foreach (var point in guideInstance.pathPoints)
        {
            GameObject guide = Instantiate(batonGuidePrefab, point, Quaternion.identity, parent);
            guide.name = $"{sequence.sequenceName}_Baton_Guide_{guideInstance.batonGuides.Count}";
            guideInstance.batonGuides.Add(guide);
        }
    }
    
    private bool HasBatonRequirement(AttackSequence sequence)
    {
        foreach (var box in sequence.sequenceBoxes)
        {
            if (box != null)
            {
                var detector = box.GetComponent<StanceDetector>();
                if (detector != null && (box.CompareTag("Left Baton") || box.CompareTag("Right Baton")))
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    public void ShowGuidesForStance(string stanceName)
    {
        if (stanceManager == null) return;
        
        var arnisStyles = GetActiveArnisStyles();
        
        foreach (var style in arnisStyles)
        {
            if (style.styleName == stanceName)
            {
                foreach (var sequence in style.sequences)
                {
                    ShowGuideForSequence(sequence);
                }
                break;
            }
        }
    }
    
    public void ShowGuideForSequence(AttackSequence sequence)
    {
        var guides = GetActiveGuides();
        
        if (guides.ContainsKey(sequence))
        {
            var guideInstance = guides[sequence];
            SetGuideVisibility(guideInstance, true);
            
            if (loopPath)
            {
                StartGuideAnimation(guideInstance);
            }
        }
    }
    
    public void HideGuideForSequence(AttackSequence sequence)
    {
        var guides = GetActiveGuides();
        
        if (guides.ContainsKey(sequence))
        {
            var guideInstance = guides[sequence];
            SetGuideVisibility(guideInstance, false);
            StopGuideAnimation(guideInstance);
        }
    }
    
    public void HideAllGuides()
    {
        var guides = GetActiveGuides();
        
        foreach (var kvp in guides)
        {
            SetGuideVisibility(kvp.Value, false);
            StopGuideAnimation(kvp.Value);
        }
    }
    
    private void SetGuideVisibility(GuideInstance guideInstance, bool visible)
    {
        guideInstance.isActive = visible;
        
        SetListVisibility(guideInstance.leftHandGuides, visible);
        SetListVisibility(guideInstance.rightHandGuides, visible);
        SetListVisibility(guideInstance.batonGuides, visible);
    }
    
    private void SetListVisibility(List<GameObject> guides, bool visible)
    {
        foreach (var guide in guides)
        {
            if (guide != null)
            {
                guide.SetActive(visible);
            }
        }
    }
    
    private void StartGuideAnimation(GuideInstance guideInstance)
    {
        if (guideInstance.animationCoroutine != null)
        {
            StopCoroutine(guideInstance.animationCoroutine);
        }
        
        guideInstance.animationCoroutine = StartCoroutine(AnimateGuides(guideInstance));
    }
    
    private void StopGuideAnimation(GuideInstance guideInstance)
    {
        if (guideInstance.animationCoroutine != null)
        {
            StopCoroutine(guideInstance.animationCoroutine);
            guideInstance.animationCoroutine = null;
        }
    }
    
    private IEnumerator AnimateGuides(GuideInstance guideInstance)
    {
        while (guideInstance.isActive && loopPath)
        {
            yield return StartCoroutine(AnimateGuidePath(guideInstance));
            
            if (loopPath && guideInstance.isActive)
            {
                yield return new WaitForSeconds(loopDelay);
            }
        }
    }
    
    private IEnumerator AnimateGuidePath(GuideInstance guideInstance)
    {
        if (guideInstance.pathPoints.Count < 2) yield break;
        
        float totalDistance = CalculateTotalPathDistance(guideInstance.pathPoints);
        float totalTime = totalDistance / guideSpeed;
        float currentTime = 0f;
        
        while (currentTime < totalTime && guideInstance.isActive)
        {
            float normalizedTime = currentTime / totalTime;
            float adjustedSpeed = speedCurve.Evaluate(normalizedTime);
            
            Vector3 currentPosition = GetPositionOnPath(guideInstance.pathPoints, normalizedTime);
            
            AnimateGuideList(guideInstance.leftHandGuides, currentPosition, normalizedTime);
            AnimateGuideList(guideInstance.rightHandGuides, currentPosition, normalizedTime);
            AnimateGuideList(guideInstance.batonGuides, currentPosition, normalizedTime);
            
            currentTime += Time.deltaTime * adjustedSpeed;
            yield return null;
        }
    }
    
    private void AnimateGuideList(List<GameObject> guides, Vector3 targetPosition, float normalizedTime)
    {
        if (guides.Count == 0) return;
        
        for (int i = 0; i < guides.Count; i++)
        {
            if (guides[i] != null)
            {
                float trailOffset = (float)i / guides.Count * 0.1f; 
                float adjustedTime = Mathf.Clamp01(normalizedTime - trailOffset);
                
                Vector3 trailPosition = Vector3.Lerp(guides[i].transform.position, targetPosition, adjustedTime);
                guides[i].transform.position = trailPosition;
                
                if (fadeAtEndpoints)
                {
                    ApplyFading(guides[i], adjustedTime);
                }
            }
        }
    }
    
    private void ApplyFading(GameObject guide, float normalizedTime)
    {
        Renderer renderer = guide.GetComponent<Renderer>();
        if (renderer != null)
        {
            float alpha = 1f;
            
            if (normalizedTime < fadeDistance)
            {
                alpha = normalizedTime / fadeDistance;
            }
            else if (normalizedTime > 1f - fadeDistance)
            {
                alpha = (1f - normalizedTime) / fadeDistance;
            }
            
            Color color = renderer.material.color;
            color.a = alpha;
            renderer.material.color = color;
        }
    }
    
    private float CalculateTotalPathDistance(List<Vector3> pathPoints)
    {
        float distance = 0f;
        for (int i = 1; i < pathPoints.Count; i++)
        {
            distance += Vector3.Distance(pathPoints[i - 1], pathPoints[i]);
        }
        return distance;
    }
    
    private Vector3 GetPositionOnPath(List<Vector3> pathPoints, float normalizedTime)
    {
        if (pathPoints.Count < 2) return pathPoints[0];
        
        float totalDistance = CalculateTotalPathDistance(pathPoints);
        float targetDistance = totalDistance * normalizedTime;
        float currentDistance = 0f;
        
        for (int i = 1; i < pathPoints.Count; i++)
        {
            float segmentDistance = Vector3.Distance(pathPoints[i - 1], pathPoints[i]);
            
            if (currentDistance + segmentDistance >= targetDistance)
            {
                float segmentProgress = (targetDistance - currentDistance) / segmentDistance;
                return Vector3.Lerp(pathPoints[i - 1], pathPoints[i], segmentProgress);
            }
            
            currentDistance += segmentDistance;
        }
        
        return pathPoints[pathPoints.Count - 1];
    }
    
    private List<StanceManager.ArnisStyle> GetActiveArnisStyles()
    {
        if (stanceManager == null) return new List<StanceManager.ArnisStyle>();
        
        return stanceManager.isRightHandDominant && enableMirroring ? 
               GetMirroredArnisStyles() : stanceManager.arnisStyles;
    }
    
    private List<StanceManager.ArnisStyle> GetMirroredArnisStyles()
    {
        return stanceManager.arnisStyles;
    }
    
    private AttackSequence GetMirroredSequence(AttackSequence original)
    {
        return original; 
    }
    
    private Dictionary<AttackSequence, GuideInstance> GetActiveGuides()
    {
        return (stanceManager != null && stanceManager.isRightHandDominant && enableMirroring) ? 
               mirroredGuides : activeGuides;
    }
    
    public void ClearAllGuides()
    {
        foreach (var kvp in activeGuides)
        {
            kvp.Value.ClearGuides();
        }
        activeGuides.Clear();
        
        foreach (var kvp in mirroredGuides)
        {
            kvp.Value.ClearGuides();
        }
        mirroredGuides.Clear();
    }
    
    public void SetGuideSpeed(float speed)
    {
        guideSpeed = Mathf.Max(0.1f, speed);
    }
    
    public void SetLooping(bool loop)
    {
        loopPath = loop;
        
        if (!loop)
        {
            foreach (var kvp in GetActiveGuides())
            {
                StopGuideAnimation(kvp.Value);
            }
        }
    }
    
    public void SetPathHeight(float height)
    {
        pathHeight = height;
        GenerateGuidesForAllSequences();
    }
    
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        Gizmos.color = pathColor;
        
        foreach (var kvp in activeGuides)
        {
            var pathPoints = kvp.Value.pathPoints;
            for (int i = 1; i < pathPoints.Count; i++)
            {
                Gizmos.DrawLine(pathPoints[i - 1], pathPoints[i]);
                Gizmos.DrawWireSphere(pathPoints[i], 0.05f);
            }
        }
    }
    
    private void OnDestroy()
    {
        ClearAllGuides();
        
        if (stanceManager != null)
        {
            stanceManager.OnStanceChanged -= OnStanceChanged;
        }
    }
}