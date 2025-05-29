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
    [SerializeField] private Color leftPathColor = Color.blue;
    [SerializeField] private Color rightPathColor = Color.red;

    private Dictionary<AttackSequence, GuideInstance> activeGuides = new Dictionary<AttackSequence, GuideInstance>();
    private Dictionary<AttackSequence, GuideInstance> mirroredGuides = new Dictionary<AttackSequence, GuideInstance>();

    private StanceManager stanceManager;

    private Transform guidesParent;
    private Transform mirroredGuidesParent;

    [System.Serializable]
    public class GuideInstance
    {
        public AttackSequence sequence;
        
        // Separate path points for left and right hands
        public List<Vector3> leftPathPoints = new List<Vector3>();
        public List<Vector3> rightPathPoints = new List<Vector3>();
        
        public List<GameObject> leftHandGuides = new List<GameObject>();
        public List<GameObject> rightHandGuides = new List<GameObject>();
        public List<GameObject> batonGuides = new List<GameObject>();
        
        public bool isActive = false;
        public Coroutine leftAnimationCoroutine;
        public Coroutine rightAnimationCoroutine;

        public void ClearGuides()
        {
            DestroyGuideList(leftHandGuides);
            DestroyGuideList(rightHandGuides);
            DestroyGuideList(batonGuides);
            
            leftPathPoints.Clear();
            rightPathPoints.Clear();

            if (leftAnimationCoroutine != null)
            {
                leftAnimationCoroutine = null;
            }
            if (rightAnimationCoroutine != null)
            {
                rightAnimationCoroutine = null;
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

            // Generate guides after StanceManager is initialized
            StartCoroutine(InitializeGuidesDelayed());
        }

        if (mirrorPlane == null && stanceManager != null)
        {
            mirrorPlane = stanceManager.mirrorPlane;
            mirrorNormal = stanceManager.mirrorNormal;
        }
    }

    private IEnumerator InitializeGuidesDelayed()
    {
        yield return new WaitForEndOfFrame();

        // Validate prefabs
        if (leftHandGuidePrefab == null)
            Debug.LogWarning("StanceGuideSystem: leftHandGuidePrefab is not assigned!");
        if (rightHandGuidePrefab == null)
            Debug.LogWarning("StanceGuideSystem: rightHandGuidePrefab is not assigned!");

        // Generate guides for all sequences
        GenerateGuidesForAllSequences();

        Debug.Log($"StanceGuideSystem: Generated guides for {activeGuides.Count} sequences");
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

        GenerateSeparatePathPoints(sequence, guideInstance);

        CreateGuideObjects(sequence, guideInstance, isMirrored);

        if (isMirrored)
            mirroredGuides[sequence] = guideInstance;
        else
            activeGuides[sequence] = guideInstance;
    }

    private void GenerateSeparatePathPoints(AttackSequence sequence, GuideInstance guideInstance)
    {
        var leftPathPoints = guideInstance.leftPathPoints;
        var rightPathPoints = guideInstance.rightPathPoints;

        // Add start positions
        if (sequence.startBoxLeft != null)
        {
            Vector3 startPos = sequence.startBoxLeft.transform.position;
            startPos.y += pathHeight;
            leftPathPoints.Add(startPos);
        }

        if (sequence.startBoxRight != null)
        {
            Vector3 startPos = sequence.startBoxRight.transform.position;
            startPos.y += pathHeight;
            rightPathPoints.Add(startPos);
        }

        // Add sequence box positions based on their tags/requirements
        foreach (var box in sequence.sequenceBoxes)
        {
            if (box != null)
            {
                Vector3 boxPos = box.transform.position;
                boxPos.y += pathHeight;

                // Determine which hand(s) this box is for
                var detector = box.GetComponent<StanceDetector>();
                bool isLeftHandBox = IsLeftHandBox(box, detector);
                bool isRightHandBox = IsRightHandBox(box, detector);

                // Add to appropriate paths
                if (isLeftHandBox)
                {
                    leftPathPoints.Add(boxPos);
                }
                if (isRightHandBox)
                {
                    rightPathPoints.Add(boxPos);
                }

                // If it's neither specifically left nor right, add to both
                if (!isLeftHandBox && !isRightHandBox)
                {
                    leftPathPoints.Add(boxPos);
                    rightPathPoints.Add(boxPos);
                }
            }
        }

        // Add end positions
        if (sequence.endBoxLeft != null)
        {
            Vector3 endPos = sequence.endBoxLeft.transform.position;
            endPos.y += pathHeight;
            leftPathPoints.Add(endPos);
        }

        if (sequence.endBoxRight != null)
        {
            Vector3 endPos = sequence.endBoxRight.transform.position;
            endPos.y += pathHeight;
            rightPathPoints.Add(endPos);
        }
    }

    private bool IsLeftHandBox(GameObject box, StanceDetector detector)
    {
        // Check by tag first
        if (box.CompareTag("Left Hand") || box.CompareTag("Left Baton"))
            return true;

        // Check by name patterns
        string boxName = box.name.ToLower();
        if (boxName.Contains("left") || boxName.Contains("l_"))
            return true;

        // Add other detection logic as needed
        return false;
    }

    private bool IsRightHandBox(GameObject box, StanceDetector detector)
    {
        // Check by tag first
        if (box.CompareTag("Right Hand") || box.CompareTag("Right Baton"))
            return true;

        // Check by name patterns
        string boxName = box.name.ToLower();
        if (boxName.Contains("right") || boxName.Contains("r_"))
            return true;

        // Add other detection logic as needed
        return false;
    }

    private void CreateGuideObjects(AttackSequence sequence, GuideInstance guideInstance, bool isMirrored)
    {
        Transform parentTransform = isMirrored ? mirroredGuidesParent : guidesParent;

        // Create left hand guides following left path
        if (leftHandGuidePrefab != null && guideInstance.leftPathPoints.Count > 0)
        {
            CreateGuidesForPath(guideInstance.leftPathPoints, leftHandGuidePrefab, 
                               guideInstance.leftHandGuides, $"{sequence.sequenceName}_Left", parentTransform);
        }

        // Create right hand guides following right path
        if (rightHandGuidePrefab != null && guideInstance.rightPathPoints.Count > 0)
        {
            CreateGuidesForPath(guideInstance.rightPathPoints, rightHandGuidePrefab, 
                               guideInstance.rightHandGuides, $"{sequence.sequenceName}_Right", parentTransform);
        }

        // Create baton guides if needed (following both paths or specific logic)
        if (HasBatonRequirement(sequence) && batonGuidePrefab != null)
        {
            CreateBatonGuides(sequence, guideInstance, parentTransform);
        }

        SetGuideVisibility(guideInstance, false);
    }

    private void CreateGuidesForPath(List<Vector3> pathPoints, GameObject prefab, 
                                   List<GameObject> guideList, string namePrefix, Transform parent)
    {
        foreach (var point in pathPoints)
        {
            GameObject guide = Instantiate(prefab, point, Quaternion.identity, parent);
            guide.name = $"{namePrefix}_Guide_{guideList.Count}";
            guideList.Add(guide);
        }
    }

    private void CreateBatonGuides(AttackSequence sequence, GuideInstance guideInstance, Transform parent)
    {
        // For baton guides, you might want to follow both paths or create a specific logic
        // Here's an example that creates baton guides for boxes specifically tagged as baton boxes
        foreach (var box in sequence.sequenceBoxes)
        {
            if (box != null && (box.CompareTag("Left Baton") || box.CompareTag("Right Baton")))
            {
                Vector3 batonPos = box.transform.position;
                batonPos.y += pathHeight;
                
                GameObject guide = Instantiate(batonGuidePrefab, batonPos, Quaternion.identity, parent);
                guide.name = $"{sequence.sequenceName}_Baton_Guide_{guideInstance.batonGuides.Count}";
                guideInstance.batonGuides.Add(guide);
            }
        }
    }

    private bool HasBatonRequirement(AttackSequence sequence)
    {
        foreach (var box in sequence.sequenceBoxes)
        {
            if (box != null && (box.CompareTag("Left Baton") || box.CompareTag("Right Baton")))
            {
                return true;
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
        StopGuideAnimation(guideInstance);

        // Start separate animations for left and right paths
        if (guideInstance.leftPathPoints.Count > 1)
        {
            guideInstance.leftAnimationCoroutine = StartCoroutine(
                AnimateGuidesOnPath(guideInstance.leftHandGuides, guideInstance.leftPathPoints, guideInstance));
        }

        if (guideInstance.rightPathPoints.Count > 1)
        {
            guideInstance.rightAnimationCoroutine = StartCoroutine(
                AnimateGuidesOnPath(guideInstance.rightHandGuides, guideInstance.rightPathPoints, guideInstance));
        }
    }

    private void StopGuideAnimation(GuideInstance guideInstance)
    {
        if (guideInstance.leftAnimationCoroutine != null)
        {
            StopCoroutine(guideInstance.leftAnimationCoroutine);
            guideInstance.leftAnimationCoroutine = null;
        }

        if (guideInstance.rightAnimationCoroutine != null)
        {
            StopCoroutine(guideInstance.rightAnimationCoroutine);
            guideInstance.rightAnimationCoroutine = null;
        }
    }

    private IEnumerator AnimateGuidesOnPath(List<GameObject> guides, List<Vector3> pathPoints, GuideInstance guideInstance)
    {
        while (guideInstance.isActive && loopPath && pathPoints.Count > 1)
        {
            yield return StartCoroutine(AnimateGuidePathSingle(guides, pathPoints, guideInstance));

            if (loopPath && guideInstance.isActive)
            {
                yield return new WaitForSeconds(loopDelay);
            }
        }
    }

    private IEnumerator AnimateGuidePathSingle(List<GameObject> guides, List<Vector3> pathPoints, GuideInstance guideInstance)
    {
        if (pathPoints.Count < 2) yield break;

        float totalDistance = CalculateTotalPathDistance(pathPoints);
        float totalTime = totalDistance / guideSpeed;
        float currentTime = 0f;

        while (currentTime < totalTime && guideInstance.isActive)
        {
            float normalizedTime = currentTime / totalTime;
            float adjustedSpeed = speedCurve.Evaluate(normalizedTime);

            Vector3 currentPosition = GetPositionOnPath(pathPoints, normalizedTime);

            AnimateGuideList(guides, currentPosition, normalizedTime);

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
        if (stanceManager == null || !enableMirroring)
            return stanceManager.arnisStyles;

        return stanceManager.mirroredArnisStyles;
    }

    private AttackSequence GetMirroredSequence(AttackSequence original)
    {
        if (stanceManager == null || !enableMirroring) return original;

        foreach (var mirroredStyle in stanceManager.mirroredArnisStyles)
        {
            foreach (var mirroredSequence in mirroredStyle.sequences)
            {
                if (mirroredSequence.sequenceName == original.sequenceName)
                {
                    return mirroredSequence;
                }
            }
        }

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

        foreach (var kvp in activeGuides)
        {
            var guideInstance = kvp.Value;
            
            // Draw left path
            Gizmos.color = leftPathColor;
            for (int i = 1; i < guideInstance.leftPathPoints.Count; i++)
            {
                Gizmos.DrawLine(guideInstance.leftPathPoints[i - 1], guideInstance.leftPathPoints[i]);
                Gizmos.DrawWireSphere(guideInstance.leftPathPoints[i], 0.05f);
            }

            // Draw right path
            Gizmos.color = rightPathColor;
            for (int i = 1; i < guideInstance.rightPathPoints.Count; i++)
            {
                Gizmos.DrawLine(guideInstance.rightPathPoints[i - 1], guideInstance.rightPathPoints[i]);
                Gizmos.DrawWireSphere(guideInstance.rightPathPoints[i], 0.05f);
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
    
    public void DebugGuideSystem()
    {
        Debug.Log($"=== StanceGuideSystem Debug ===");
        Debug.Log($"StanceManager found: {stanceManager != null}");
        Debug.Log($"Left Hand Prefab: {leftHandGuidePrefab != null}");
        Debug.Log($"Right Hand Prefab: {rightHandGuidePrefab != null}");
        Debug.Log($"Baton Prefab: {batonGuidePrefab != null}");
        Debug.Log($"Active Guides Count: {activeGuides.Count}");
        Debug.Log($"Mirrored Guides Count: {mirroredGuides.Count}");
        Debug.Log($"Enable Mirroring: {enableMirroring}");
        Debug.Log($"Show Only Active Sequence: {showOnlyActiveSequence}");
        
        if (stanceManager != null)
        {
            Debug.Log($"Right Hand Dominant: {stanceManager.isRightHandDominant}");
            Debug.Log($"Arnis Styles Count: {stanceManager.arnisStyles.Count}");
        }

        // Debug path information
        foreach (var kvp in GetActiveGuides())
        {
            var sequence = kvp.Key;
            var guide = kvp.Value;
            Debug.Log($"Sequence: {sequence.sequenceName}");
            Debug.Log($"  Left Path Points: {guide.leftPathPoints.Count}");
            Debug.Log($"  Right Path Points: {guide.rightPathPoints.Count}");
            Debug.Log($"  Left Guides: {guide.leftHandGuides.Count}");
            Debug.Log($"  Right Guides: {guide.rightHandGuides.Count}");
        }
    }
}