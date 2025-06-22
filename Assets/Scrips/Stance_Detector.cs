using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StanceDetector : MonoBehaviour
{
    [Header("Stance Configuration")]
    public string stanceName;
    public GameObject[] stanceBoxes;

    [Header("Gizmos")]
    [SerializeField] private bool showDetectionGizmos = false;
    [SerializeField] private bool showOnlyWhenSelected = false;
    [SerializeField] private Color gizmosColor = Color.yellow;
    [SerializeField] private float gizmosArrowLength = 0.3f;

    [Header("Baton & Detection Settings")]
    public Transform leftBatonTip;
    public Transform rightBatonTip;
    public Transform leftHand;
    public Transform rightHand;

    [Header("One-Handed Mode")]
    [SerializeField] private bool isOneHanded = false;

    [SerializeField] private float angleThreshold = 30f;
    [SerializeField] private float positionThreshold = 0.2f;

    [Header("Visual Representation")]
    [SerializeField] private GameObject visualPrefab;
    private GameObject visualInstance;
    [SerializeField] private Vector3 visualRotationOffset = Vector3.zero;

    [Header("Visual Mirroring Settings")]
    [SerializeField] private bool enableVisualMirroring = true;
    [SerializeField] private MirrorAxis mirrorAxis = MirrorAxis.X;
    [SerializeField] private bool invertMirrorAxis = false;
    public enum MirrorAxis
    {
        X,
        Y,
        Z
    }

    [Header("Sequence Visual Behavior")]
    [SerializeField] private bool disappearOnTouch = true;
    [SerializeField] private float disappearDelay = 0.0f;
    [SerializeField] private bool useDisappearAnimation = false;
    [SerializeField] private float disappearAnimationDuration = 0.5f;

    [Header("Color Settings")]
    [SerializeField] private Color leftBatonColor = new Color(1.0f, 0.647f, 0.0f);
    [SerializeField] private Color rightBatonColor = new Color(0.0f, 0.812f, 1.0f);
    [SerializeField] private Color leftBatonDarkColor = new Color(0.6f, 0.3f, 0.0f);
    [SerializeField] private Color rightBatonDarkColor = new Color(0.0f, 0.3f, 0.5f);
    [SerializeField] private Color successColor = Color.green;
    [SerializeField] private Color failureColor = Color.red;

    [HideInInspector] public bool isPartOfSequence = false;
    [HideInInspector] public int sequencePosition = 0;

    private bool leftHandInStance = false;
    private bool rightHandInStance = false;
    public bool IsCompleted { get; set; } = false;

    private bool hasVisualDisappeared = false;

    private List<Collider> collidersInTrigger = new List<Collider>();

    public bool IsLeftHandInStance() => leftHandInStance;
    public bool IsRightHandInStance() => rightHandInStance;

    private Color originalColor;
    private Renderer visualRenderer;
    private MaterialPropertyBlock propertyBlock;
    private Renderer originalRenderer;
    private Material originalMaterial;
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    private bool wasActiveLastFrame = false;
    private Coroutine disappearCoroutine = null;

    private Transform GetBatonTipForHand(bool isLeftHand)
    {
        if (StanceManager.Instance != null && StanceManager.Instance.isRightHandDominant)
        {
            return isLeftHand ? leftBatonTip : rightBatonTip;
        }
        else
        {
            return isLeftHand ? leftBatonTip : rightBatonTip;
        }
    }

    private Transform GetHandForHand(bool isLeftHand)
    {
        if (StanceManager.Instance != null && StanceManager.Instance.isRightHandDominant)
        {
            return isLeftHand ? leftHand : rightHand;
        }
        else
        {
            return isLeftHand ? leftHand : rightHand;
        }
    }

    private Transform GetActiveTransformForHand(bool isLeftHand)
    {
        if (isOneHanded)
        {
            bool isDominantHand = (StanceManager.Instance != null && StanceManager.Instance.isRightHandDominant) ? !isLeftHand : isLeftHand;

            if (isDominantHand)
            {
                return GetBatonTipForHand(isLeftHand);
            }
            else
            {
                return GetHandForHand(isLeftHand);
            }
        }
        else
        {
            return GetBatonTipForHand(isLeftHand);
        }
    }

    private void Start()
    {
        propertyBlock = new MaterialPropertyBlock();
        SetupInitialReferences();

        if (gameObject.activeInHierarchy)
        {
            SetupVisualRepresentation();
            SetupInitialColors();
        }

        wasActiveLastFrame = gameObject.activeInHierarchy;
    }

    private void Update()
    {
        bool isCurrentlyActive = gameObject.activeInHierarchy;

        if (isCurrentlyActive && !wasActiveLastFrame)
        {
            OnObjectActivated();
        }
        else if (!isCurrentlyActive && wasActiveLastFrame)
        {
            OnObjectDeactivated();
        }

        wasActiveLastFrame = isCurrentlyActive;
    }

    public GameObject GetVisualInstance()
    {
        return visualInstance;
    }
    private void OnObjectActivated()
    {
        Debug.Log($"StanceDetector {gameObject.name} activated - creating visual clone");

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        hasVisualDisappeared = false;

        SetupInitialReferences();
        SetupVisualRepresentation();
        SetupInitialColors();
    }

    private void OnObjectDeactivated()
    {
        Debug.Log($"StanceDetector {gameObject.name} deactivated - destroying visual clone");

        if (disappearCoroutine != null)
        {
            StopCoroutine(disappearCoroutine);
            disappearCoroutine = null;
        }

        DestroyVisualClone();
        ForceResetTriggerState();
    }

    private void SetupInitialReferences()
    {
        if (originalRenderer == null)
        {
            originalRenderer = GetComponent<Renderer>();
            if (originalRenderer != null)
            {
                originalMaterial = originalRenderer.material;
            }
        }
    }

    private void OnEnable()
    {
        if (visualPrefab != null && visualInstance == null && gameObject.activeInHierarchy)
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            hasVisualDisappeared = false;

            SetupInitialReferences();
            SetupVisualRepresentation();
            SetupInitialColors();
        }
    }

    private void OnDisable()
    {
        if (disappearCoroutine != null)
        {
            StopCoroutine(disappearCoroutine);
            disappearCoroutine = null;
        }

        ForceResetTriggerState();
        DestroyVisualClone();
    }

    private void SetupVisualRepresentation()
    {
        if (!gameObject.activeInHierarchy || hasVisualDisappeared)
        {
            return;
        }

        if (visualInstance != null)
        {
            DestroyVisualClone();
        }

        if (visualPrefab != null)
        {
            Quaternion finalRotation = transform.rotation * Quaternion.Euler(visualRotationOffset);
            visualInstance = Instantiate(visualPrefab, transform.position, finalRotation, null);
            visualRenderer = visualInstance.GetComponent<Renderer>();

            if (visualRenderer == null)
            {
                visualRenderer = visualInstance.GetComponentInChildren<Renderer>();
            }

            if (visualRenderer != null)
            {
                Material baseMaterial = originalMaterial != null ? originalMaterial : visualRenderer.material;
                originalColor = baseMaterial.color;

                if (originalMaterial != null)
                {
                    visualRenderer.material = originalMaterial;
                }
            }

            ApplyVisualMirroring();

            Debug.Log($"Created visual clone for {gameObject.name}");
        }
        else
        {
            visualRenderer = originalRenderer;

            if (visualRenderer != null && originalMaterial != null)
            {
                originalColor = originalMaterial.color;
            }
        }
    }

    private void ApplyVisualMirroring()
    {
        if (visualInstance == null || StanceManager.Instance == null || !enableVisualMirroring) return;

        if (StanceManager.Instance.isRightHandDominant)
        {
            Vector3 scale = visualInstance.transform.localScale;

            switch (mirrorAxis)
            {
                case MirrorAxis.X:
                    scale.x = invertMirrorAxis ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
                    break;
                case MirrorAxis.Y:
                    scale.y = invertMirrorAxis ? Mathf.Abs(scale.y) : -Mathf.Abs(scale.y);
                    break;
                case MirrorAxis.Z:
                    scale.z = invertMirrorAxis ? Mathf.Abs(scale.z) : -Mathf.Abs(scale.z);
                    break;
            }

            visualInstance.transform.localScale = scale;
        }
        else
        {
            Vector3 scale = visualInstance.transform.localScale;
            scale.x = Mathf.Abs(scale.x);
            scale.y = Mathf.Abs(scale.y);
            scale.z = Mathf.Abs(scale.z);
            visualInstance.transform.localScale = scale;
        }
    }

    private void SetupInitialColors()
    {
        if (visualRenderer == null || !gameObject.activeInHierarchy || hasVisualDisappeared) return;

        if (isPartOfSequence)
        {
            AttackSequence sequence = GetAttackSequence();
            if (sequence != null)
            {
                UpdateSequenceColor(sequence.sequenceBoxes.Length);
            }
        }
        else
        {
            SetDefaultColor();
        }
    }

    private void SetDefaultColor()
    {
        if (visualRenderer == null || propertyBlock == null || !gameObject.activeInHierarchy || hasVisualDisappeared) return;

        Color targetColor;

        bool shouldSwapColors = StanceManager.Instance != null && StanceManager.Instance.isRightHandDominant;

        if (CompareTag("Left Baton") || CompareTag("Left Hand"))
        {
            targetColor = shouldSwapColors ? rightBatonColor : leftBatonColor;
        }
        else if (CompareTag("Right Baton") || CompareTag("Right Hand"))
        {
            targetColor = shouldSwapColors ? leftBatonColor : rightBatonColor;
        }
        else
        {
            targetColor = originalColor;
        }

        propertyBlock.SetColor(ColorProperty, targetColor);
        visualRenderer.SetPropertyBlock(propertyBlock);
    }

    private void UpdateSequenceColor(int totalBoxesInSequence)
    {
        if (visualRenderer == null || propertyBlock == null || totalBoxesInSequence <= 1 || !gameObject.activeInHierarchy || hasVisualDisappeared) return;

        if (sequencePosition == 0 || sequencePosition == totalBoxesInSequence - 1)
        {
            SetDefaultColor();
            return;
        }

        float t = (float)(sequencePosition - 1) / (totalBoxesInSequence - 3);

        bool shouldSwapColors = StanceManager.Instance != null && StanceManager.Instance.isRightHandDominant;

        Color targetColor;
        if (CompareTag("Left Baton") || CompareTag("Left Hand"))
        {
            if (shouldSwapColors)
            {
                targetColor = Color.Lerp(rightBatonColor, rightBatonDarkColor, t);
            }
            else
            {
                targetColor = Color.Lerp(leftBatonColor, leftBatonDarkColor, t);
            }
        }
        else if (CompareTag("Right Baton") || CompareTag("Right Hand"))
        {
            if (shouldSwapColors)
            {
                targetColor = Color.Lerp(leftBatonColor, leftBatonDarkColor, t);
            }
            else
            {
                targetColor = Color.Lerp(rightBatonColor, rightBatonDarkColor, t);
            }
        }
        else
        {
            targetColor = originalColor;
        }

        propertyBlock.SetColor(ColorProperty, targetColor);
        visualRenderer.SetPropertyBlock(propertyBlock);
    }

    public void UpdateColorForSequence(int totalBoxesInSequence)
    {
        if (visualRenderer == null || propertyBlock == null || !gameObject.activeInHierarchy || hasVisualDisappeared) return;

        if (isPartOfSequence && totalBoxesInSequence > 1)
        {
            UpdateSequenceColor(totalBoxesInSequence);
        }
        else
        {
            SetDefaultColor();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Left Baton") || other.CompareTag("Right Baton") ||
            other.CompareTag("Left Hand") || other.CompareTag("Right Hand"))
        {
            if (!collidersInTrigger.Contains(other))
            {
                collidersInTrigger.Add(other);
            }

            CheckOrientation(other);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Left Baton") || other.CompareTag("Right Baton") ||
            other.CompareTag("Left Hand") || other.CompareTag("Right Hand"))
        {
            CheckOrientation(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Left Baton") || other.CompareTag("Left Hand"))
        {
            leftHandInStance = false;

            if (collidersInTrigger.Contains(other))
            {
                collidersInTrigger.Remove(other);
            }
        }
        else if (other.CompareTag("Right Baton") || other.CompareTag("Right Hand"))
        {
            rightHandInStance = false;

            if (collidersInTrigger.Contains(other))
            {
                collidersInTrigger.Remove(other);
            }
        }

        ResetColor();
        CheckStance();
    }

    public void ForceResetTriggerState()
    {
        foreach (var collider in new List<Collider>(collidersInTrigger))
        {
            if (collider != null)
            {
                if (collider.CompareTag("Left Baton") || collider.CompareTag("Left Hand"))
                {
                    leftHandInStance = false;
                }
                else if (collider.CompareTag("Right Baton") || collider.CompareTag("Right Hand"))
                {
                    rightHandInStance = false;
                }
            }
        }

        collidersInTrigger.Clear();

        ResetColor();
    }

    public void DestroyVisualClone()
    {
        if (visualInstance != null)
        {
            Debug.Log($"Destroying visual clone for {gameObject.name}");

            if (Application.isPlaying)
            {
                Destroy(visualInstance);
            }
            else
            {
                DestroyImmediate(visualInstance);
            }
            visualInstance = null;
            visualRenderer = null;
        }
    }

    private void HandleSequenceTouch()
    {
        if (!isPartOfSequence || !disappearOnTouch || hasVisualDisappeared) return;

        Debug.Log($"Visual disappearing for sequence box: {gameObject.name}");

        if (disappearCoroutine != null)
        {
            StopCoroutine(disappearCoroutine);
        }

        disappearCoroutine = StartCoroutine(DisappearVisualCoroutine());
    }

    private IEnumerator DisappearVisualCoroutine()
    {
        if (disappearDelay > 0)
        {
            yield return new WaitForSeconds(disappearDelay);
        }

        if (visualInstance != null)
        {
            if (useDisappearAnimation && disappearAnimationDuration > 0)
            {
                Vector3 originalScale = visualInstance.transform.localScale;
                float elapsedTime = 0f;

                while (elapsedTime < disappearAnimationDuration)
                {
                    if (visualInstance == null) yield break;

                    float t = elapsedTime / disappearAnimationDuration;
                    float scale = Mathf.Lerp(1f, 0f, t);
                    visualInstance.transform.localScale = originalScale * scale;

                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
            }

            hasVisualDisappeared = true;
            DestroyVisualClone();
        }

        disappearCoroutine = null;
    }

    public void TriggerVisualDisappear()
    {
        HandleSequenceTouch();
    }

    public void RestoreVisual()
    {
        if (hasVisualDisappeared)
        {
            hasVisualDisappeared = false;

            if (disappearCoroutine != null)
            {
                StopCoroutine(disappearCoroutine);
                disappearCoroutine = null;
            }

            if (gameObject.activeInHierarchy)
            {
                SetupVisualRepresentation();
                SetupInitialColors();
            }
        }
    }

    private void CheckOrientation(Collider other)
    {
        if (!gameObject.activeInHierarchy) return;

        bool isLeftHand = other.CompareTag("Left Baton") || other.CompareTag("Left Hand");
        Transform currentTransform = GetActiveTransformForHand(isLeftHand);

        if (currentTransform == null) return;

        Vector3 transformDirection = (currentTransform.position - transform.position).normalized;
        Vector3 worldCorrectDirection = transform.forward;
        float dot = Vector3.Dot(worldCorrectDirection.normalized, transformDirection);
        float distance = Vector3.Distance(currentTransform.position, transform.position);
        bool inStance = (dot >= Mathf.Cos(angleThreshold * Mathf.Deg2Rad)) && (distance < positionThreshold);

        if (isLeftHand)
        {
            leftHandInStance = inStance;
        }
        else
        {
            rightHandInStance = inStance;
        }

        CheckStance();

        if (inStance && isPartOfSequence)
        {
            HandleSequenceTouch();
        }

        if (visualRenderer != null && propertyBlock != null && !hasVisualDisappeared)
        {
            Color targetColor = inStance ? successColor : failureColor;
            propertyBlock.SetColor(ColorProperty, targetColor);
            visualRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    private AttackSequence GetAttackSequence()
    {
        StanceManager stanceManager = StanceManager.Instance;
        if (stanceManager != null)
        {
            foreach (var style in stanceManager.arnisStyles)
            {
                foreach (var sequence in style.sequences)
                {
                    for (int i = 0; i < sequence.sequenceBoxes.Length; i++)
                    {
                        if (sequence.sequenceBoxes[i] == gameObject)
                        {
                            return sequence;
                        }
                    }
                }
            }
        }
        return null;
    }

    private void CheckStance()
    {
        if (!gameObject.activeInHierarchy) return;

        StanceDetector[] allDetectors = FindObjectsOfType<StanceDetector>();

        bool allBoxesTriggered = true;

        foreach (StanceDetector detector in allDetectors)
        {
            if (detector.stanceName == this.stanceName && detector.gameObject.activeInHierarchy)
            {
                if (!detector.leftHandInStance && !detector.rightHandInStance)
                {
                    allBoxesTriggered = false;
                    break;
                }
            }
        }

        if (allBoxesTriggered)
        {
            StanceManager.Instance.EnterStance(stanceName);
        }
    }

    public void ResetStance()
    {
        leftHandInStance = false;
        rightHandInStance = false;
        ResetColor();
        IsCompleted = false;

        RestoreVisual();
    }

    private void ResetColor()
    {
        if (visualRenderer == null || propertyBlock == null || !gameObject.activeInHierarchy || hasVisualDisappeared) return;

        if (isPartOfSequence)
        {
            AttackSequence sequence = GetAttackSequence();
            if (sequence != null)
            {
                UpdateSequenceColor(sequence.sequenceBoxes.Length);
            }
        }
        else
        {
            SetDefaultColor();
        }

        if (StanceManager.Instance != null && isPartOfSequence)
        {
            StanceManager.Instance.UpdateSequenceColors();
        }
    }

    public void SetVisualPrefab(GameObject newPrefab, Vector3 rotationOffset = default)
    {
        DestroyVisualClone();

        visualPrefab = newPrefab;
        visualRotationOffset = rotationOffset;

        hasVisualDisappeared = false;

        if (gameObject.activeInHierarchy)
        {
            SetupVisualRepresentation();
            SetupInitialColors();
        }
    }

    public void SetVisualRotationOffset(Vector3 rotationOffset)
    {
        visualRotationOffset = rotationOffset;
        if (visualInstance != null)
        {
            visualInstance.transform.rotation = transform.rotation * Quaternion.Euler(visualRotationOffset);
        }
    }

    public void SetVisualMirroringEnabled(bool enabled)
    {
        enableVisualMirroring = enabled;
        if (visualInstance != null)
        {
            ApplyVisualMirroring();
        }
    }

    public void SetMirrorAxis(MirrorAxis axis)
    {
        mirrorAxis = axis;
        if (visualInstance != null)
        {
            ApplyVisualMirroring();
        }
    }

    public void SetInvertMirrorAxis(bool invert)
    {
        invertMirrorAxis = invert;
        if (visualInstance != null)
        {
            ApplyVisualMirroring();
        }
    }

    public void RefreshVisualMirroring()
    {
        if (visualInstance != null)
        {
            ApplyVisualMirroring();
        }
    }

    public void SetOneHandedMode(bool oneHanded)
    {
        isOneHanded = oneHanded;
    }

    public bool IsOneHandedMode()
    {
        return isOneHanded;
    }

    public void SetDisappearOnTouch(bool disappear)
    {
        disappearOnTouch = disappear;
    }

    public void SetDisappearDelay(float delay)
    {
        disappearDelay = delay;
    }

    public void SetUseDisappearAnimation(bool useAnimation)
    {
        useDisappearAnimation = useAnimation;
    }

    public void SetDisappearAnimationDuration(float duration)
    {
        disappearAnimationDuration = duration;
    }

    public bool HasVisualDisappeared()
    {
        return hasVisualDisappeared;
    }

    private void OnDestroy()
    {
        if (disappearCoroutine != null)
        {
            StopCoroutine(disappearCoroutine);
        }
        DestroyVisualClone();
    }

    public bool HasVisualClone()
    {
        return visualInstance != null && !hasVisualDisappeared;
    }

    public void RecreateVisualClone()
    {
        if (visualPrefab != null && gameObject.activeInHierarchy)
        {
            DestroyVisualClone();
            hasVisualDisappeared = false;
            SetupVisualRepresentation();
            SetupInitialColors();
        }
    }
    private void DrawDetectionGizmos()
    {

        Gizmos.color = gizmosColor;


        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + (transform.forward * gizmosArrowLength);


        Gizmos.DrawLine(startPos, endPos);


        Vector3 arrowHead1 = endPos - (transform.forward * 0.05f) + (transform.right * 0.025f);
        Vector3 arrowHead2 = endPos - (transform.forward * 0.05f) - (transform.right * 0.025f);
        Vector3 arrowHead3 = endPos - (transform.forward * 0.05f) + (transform.up * 0.025f);
        Vector3 arrowHead4 = endPos - (transform.forward * 0.05f) - (transform.up * 0.025f);

        Gizmos.DrawLine(endPos, arrowHead1);
        Gizmos.DrawLine(endPos, arrowHead2);
        Gizmos.DrawLine(endPos, arrowHead3);
        Gizmos.DrawLine(endPos, arrowHead4);


        Gizmos.color = new Color(gizmosColor.r, gizmosColor.g, gizmosColor.b, 0.3f);
        Gizmos.DrawSphere(transform.position, positionThreshold);


        Gizmos.color = gizmosColor;
        float angleRad = angleThreshold * Mathf.Deg2Rad;


        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        Vector3 up = transform.up;


        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            Vector3 coneDirection = forward;
            Vector3 offset = (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * Mathf.Tan(angleRad);
            coneDirection = (forward + offset).normalized;

            Vector3 coneEnd = startPos + coneDirection * gizmosArrowLength;
            Gizmos.DrawLine(startPos, coneEnd);


            if (i > 0)
            {
                float prevAngle = (i - 1) * 45f * Mathf.Deg2Rad;
                Vector3 prevOffset = (right * Mathf.Cos(prevAngle) + up * Mathf.Sin(prevAngle)) * Mathf.Tan(angleRad);
                Vector3 prevConeDirection = (forward + prevOffset).normalized;
                Vector3 prevConeEnd = startPos + prevConeDirection * gizmosArrowLength;
                Gizmos.DrawLine(coneEnd, prevConeEnd);
            }
        }


        float firstAngle = 0f;
        Vector3 firstOffset = (right * Mathf.Cos(firstAngle) + up * Mathf.Sin(firstAngle)) * Mathf.Tan(angleRad);
        Vector3 firstConeDirection = (forward + firstOffset).normalized;
        Vector3 firstConeEnd = startPos + firstConeDirection * gizmosArrowLength;

        float lastAngle = 7 * 45f * Mathf.Deg2Rad;
        Vector3 lastOffset = (right * Mathf.Cos(lastAngle) + up * Mathf.Sin(lastAngle)) * Mathf.Tan(angleRad);
        Vector3 lastConeDirection = (forward + lastOffset).normalized;
        Vector3 lastConeEnd = startPos + lastConeDirection * gizmosArrowLength;

        Gizmos.DrawLine(firstConeEnd, lastConeEnd);
    }
    private void OnDrawGizmos()
    {
        if (!showDetectionGizmos || showOnlyWhenSelected || Application.isPlaying) return;
        DrawDetectionGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDetectionGizmos || !showOnlyWhenSelected || Application.isPlaying) return;
        DrawDetectionGizmos();
    }
    public void SetShowDetectionGizmos(bool show)
    {
        showDetectionGizmos = show;
    }

    public void SetGizmosColor(Color color)
    {
        gizmosColor = color;
    }

    public void SetGizmosArrowLength(float length)
    {
        gizmosArrowLength = length;
    }
}