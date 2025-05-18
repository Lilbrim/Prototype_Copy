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

    [Header("Color Settings")]
    [SerializeField] private Color leftBatonColor = new Color(1.0f, 0.647f, 0.0f); // Orange FFA500
    [SerializeField] private Color rightBatonColor = new Color(0.0f, 0.812f, 1.0f); // Cyan 00CFFF
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
    private Renderer boxRenderer;

    private void Start()
    {
        boxRenderer = GetComponent<Renderer>();

        if (boxRenderer != null)
        {
            originalColor = boxRenderer.material.color;
            
            if (isPartOfSequence)
            {
                AttackSequence sequence = GetAttackSequence();
                if (sequence != null)
                {
                    float t = (float)sequencePosition / (sequence.sequenceBoxes.Length - 1);
                    
                    if (CompareTag("Left Baton"))
                    {
                        Color darkOrange = new Color(0.8f, 0.4f, 0.0f); // Darker orange
                        boxRenderer.material.color = Color.Lerp(leftBatonColor, darkOrange, t);
                    }
                    else if (CompareTag("Right Baton"))
                    {
                        Color darkCyan = new Color(0.0f, 0.4f, 0.6f); // Darker cyan
                        boxRenderer.material.color = Color.Lerp(rightBatonColor, darkCyan, t);
                    }
                }
            }
            else
            {
                if (CompareTag("Left Baton"))
                {
                    boxRenderer.material.color = leftBatonColor;
                }
                else if (CompareTag("Right Baton"))
                {
                    boxRenderer.material.color = rightBatonColor;
                }
            }
        }
    }

    public void UpdateColorForSequence(int totalBoxesInSequence)
    {
        if (boxRenderer == null) return;
        
        if (isPartOfSequence && totalBoxesInSequence > 1)
        {
            float t = (float)sequencePosition / (totalBoxesInSequence - 1);
            
            if (CompareTag("Left Baton"))
            {
                Color darkOrange = new Color(0.8f, 0.4f, 0.0f); // Darker orange
                boxRenderer.material.color = Color.Lerp(leftBatonColor, darkOrange, t);
            }
            else if (CompareTag("Right Baton"))
            {
                Color darkCyan = new Color(0.0f, 0.4f, 0.6f); // Darker cyan
                boxRenderer.material.color = Color.Lerp(rightBatonColor, darkCyan, t);
            }
        }
        else
        {
            // For non-sequence boxes, use default colors
            if (CompareTag("Left Baton"))
            {
                boxRenderer.material.color = leftBatonColor;
            }
            else if (CompareTag("Right Baton"))
            {
                boxRenderer.material.color = rightBatonColor;
            }
            else
            {
                boxRenderer.material.color = originalColor;
            }
        }
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

    private void OnDisable()
    {
        ForceResetTriggerState();
    }

    private void CheckOrientation(Collider other)
    {
        if (boxRenderer == null || batonTip == null)
            return;

        Vector3 batonDirection = (batonTip.position - transform.position).normalized;
        Vector3 worldCorrectDirection = transform.forward;

        float dot = Vector3.Dot(worldCorrectDirection.normalized, batonDirection);
        float distance = Vector3.Distance(batonTip.position, transform.position);

        bool inStance = (dot >= Mathf.Cos(angleThreshold * Mathf.Deg2Rad)) && (distance < positionThreshold);

        if (inStance)
        {
            boxRenderer.material.color = successColor;
            if (other.CompareTag("Left Baton")) leftHandInStance = true;
            if (other.CompareTag("Right Baton")) rightHandInStance = true;
        }
        else
        {
            boxRenderer.material.color = failureColor;
            if (other.CompareTag("Left Baton")) leftHandInStance = false;
            if (other.CompareTag("Right Baton")) rightHandInStance = false;
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
        StanceDetector[] allDetectors = FindObjectsOfType<StanceDetector>();

        bool allBoxesTriggered = true;

        foreach (StanceDetector detector in allDetectors)
        {
            if (detector.stanceName == this.stanceName)
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
        if (boxRenderer != null)
        {
            if (CompareTag("Left Baton"))
            {
                boxRenderer.material.color = leftBatonColor;
            }
            else if (CompareTag("Right Baton"))
            {
                boxRenderer.material.color = rightBatonColor;
            }
            else
            {
                boxRenderer.material.color = originalColor;
            }
            
            if (StanceManager.Instance != null && isPartOfSequence)
            {
                StanceManager.Instance.UpdateSequenceColors();
            }
        }
    }
}
