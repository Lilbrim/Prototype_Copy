using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StanceDetector : MonoBehaviour
{
    [Header("Stance Configuration")]
    public string stanceName;
    public GameObject[] stanceBoxes;

    [Header("Baton & Detection Settings")]
    public Transform batonTip;
    [SerializeField] private float angleThreshold = 30f;
    [SerializeField] private float positionThreshold = 0.2f;

    [Header("Visual Representation")]
    [SerializeField] private GameObject visualPrefab; 
    private GameObject visualInstance; 
    [SerializeField] private Vector3 visualRotationOffset = Vector3.zero; 

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

    private List<Collider> collidersInTrigger = new List<Collider>();

    public bool IsLeftHandInStance() => leftHandInStance;
    public bool IsRightHandInStance() => rightHandInStance;

    private Color originalColor;
    private Renderer visualRenderer;
    private MaterialPropertyBlock propertyBlock; 
    private Renderer originalRenderer; 
    private Material originalMaterial; 
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    // Track if we were active in the previous frame to detect activation/deactivation
    private bool wasActiveLastFrame = false;

    private void Start()
    {
        propertyBlock = new MaterialPropertyBlock();
        SetupInitialReferences();
        
        // Only create visual representation if the object starts active
        if (gameObject.activeInHierarchy)
        {
            SetupVisualRepresentation();
            SetupInitialColors();
        }
        
        wasActiveLastFrame = gameObject.activeInHierarchy;
    }

    private void Update()
    {
        // Check if activation state changed
        bool isCurrentlyActive = gameObject.activeInHierarchy;
        
        if (isCurrentlyActive && !wasActiveLastFrame)
        {
            // Object was just activated
            OnObjectActivated();
        }
        else if (!isCurrentlyActive && wasActiveLastFrame)
        {
            // Object was just deactivated
            OnObjectDeactivated();
        }
        
        wasActiveLastFrame = isCurrentlyActive;
    }

    private void OnObjectActivated()
    {
        Debug.Log($"StanceDetector {gameObject.name} activated - creating visual clone");
        
        // Ensure we have the necessary references
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }
        
        SetupInitialReferences();
        SetupVisualRepresentation();
        SetupInitialColors();
    }

    private void OnObjectDeactivated()
    {
        Debug.Log($"StanceDetector {gameObject.name} deactivated - destroying visual clone");
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
        // When object is enabled, create visual representation if it doesn't exist
        if (visualPrefab != null && visualInstance == null && gameObject.activeInHierarchy)
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }
            SetupInitialReferences();
            SetupVisualRepresentation();
            SetupInitialColors();
        }
    }

    private void OnDisable()
    {
        // When object is disabled, clean up
        ForceResetTriggerState();
        DestroyVisualClone();
    }

    private void SetupVisualRepresentation()
    {
        // Don't create visual representation if object is not active
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        // Clean up existing visual instance first
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

            // Apply mirroring if right hand dominant
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
        if (visualInstance == null || StanceManager.Instance == null) return;

        if (StanceManager.Instance.isRightHandDominant)
        {
            // Mirror by flipping X scale
            Vector3 scale = visualInstance.transform.localScale;
            scale.x = -Mathf.Abs(scale.x); // Make X scale negative
            visualInstance.transform.localScale = scale;
        }
        else
        {
            // Restore original scale
            Vector3 scale = visualInstance.transform.localScale;
            scale.x = Mathf.Abs(scale.x); // Make X scale positive
            visualInstance.transform.localScale = scale;
        }
    }

    private void SetupInitialColors()
    {
        if (visualRenderer == null || !gameObject.activeInHierarchy) return;

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
        if (visualRenderer == null || propertyBlock == null || !gameObject.activeInHierarchy) return;

        Color targetColor;
        if (CompareTag("Left Baton"))
        {
            targetColor = leftBatonColor;
        }
        else if (CompareTag("Right Baton"))
        {
            targetColor = rightBatonColor;
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
        if (visualRenderer == null || propertyBlock == null || !gameObject.activeInHierarchy) return;
        
        if (isPartOfSequence && totalBoxesInSequence > 1)
        {
            UpdateSequenceColor(totalBoxesInSequence);
        }
        else
        {
            SetDefaultColor();
        }
    }

    private void UpdateSequenceColor(int totalBoxesInSequence)
    {
        if (visualRenderer == null || propertyBlock == null || totalBoxesInSequence <= 1 || !gameObject.activeInHierarchy) return;

        if (sequencePosition == 0 || sequencePosition == totalBoxesInSequence - 1)
        {
            SetDefaultColor();
            return;
        }

        float t = (float)(sequencePosition - 1) / (totalBoxesInSequence - 3);
        
        Color targetColor;
        if (CompareTag("Left Baton"))
        {
            targetColor = Color.Lerp(leftBatonColor, leftBatonDarkColor, t);
        }
        else if (CompareTag("Right Baton"))
        {
            targetColor = Color.Lerp(rightBatonColor, rightBatonDarkColor, t);
        }
        else
        {
            targetColor = originalColor;
        }

        propertyBlock.SetColor(ColorProperty, targetColor);
        visualRenderer.SetPropertyBlock(propertyBlock);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Left Baton") || other.CompareTag("Right Baton"))
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
        if (other.CompareTag("Left Baton") || other.CompareTag("Right Baton"))
        {
            CheckOrientation(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Left Baton"))
        {
            leftHandInStance = false;
            
            if (collidersInTrigger.Contains(other))
            {
                collidersInTrigger.Remove(other);
            }
        }
        else if (other.CompareTag("Right Baton"))
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
                if (collider.CompareTag("Left Baton"))
                {
                    leftHandInStance = false;
                }
                else if (collider.CompareTag("Right Baton"))
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

    private void CheckOrientation(Collider other)
    {
        if (visualRenderer == null || propertyBlock == null || batonTip == null || !gameObject.activeInHierarchy)
            return;

        Vector3 batonDirection = (batonTip.position - transform.position).normalized;
        Vector3 worldCorrectDirection = transform.forward;

        float dot = Vector3.Dot(worldCorrectDirection.normalized, batonDirection);
        float distance = Vector3.Distance(batonTip.position, transform.position);

        bool inStance = (dot >= Mathf.Cos(angleThreshold * Mathf.Deg2Rad)) && (distance < positionThreshold);

        Color targetColor = inStance ? successColor : failureColor;
        propertyBlock.SetColor(ColorProperty, targetColor);
        visualRenderer.SetPropertyBlock(propertyBlock);

        if (other.CompareTag("Left Baton"))
        {
            leftHandInStance = inStance;
        }
        else if (other.CompareTag("Right Baton"))
        {
            rightHandInStance = inStance;
        }

        CheckStance();
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
    }

    private void ResetColor()
    {
        if (visualRenderer == null || propertyBlock == null || !gameObject.activeInHierarchy) return;

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
        
        // Only setup visual representation if object is currently active
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

    private void OnDestroy()
    {
        DestroyVisualClone();
    }
    
    public bool HasVisualClone()
    {
        return visualInstance != null;
    }

    public void RecreateVisualClone()
    {
        if (visualPrefab != null && gameObject.activeInHierarchy)
        {
            DestroyVisualClone();
            SetupVisualRepresentation();
            SetupInitialColors();
        }
    }
}