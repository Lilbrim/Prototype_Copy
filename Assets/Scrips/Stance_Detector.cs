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

    private bool leftHandInStance = false;
    private bool rightHandInStance = false;
    public bool IsCompleted { get; set; } = false;
    
    // Add these variables to track visibility and interactability
    private bool isVisible = true;
    private bool isInteractable = true;
    private Collider boxCollider;

    public bool IsLeftHandInStance() => leftHandInStance;
    public bool IsRightHandInStance() => rightHandInStance;

    private Material originalMaterial;
    private Material greenMaterial;
    private Material redMaterial;
    private Renderer boxRenderer;

    private void Start()
    {
        boxRenderer = GetComponent<Renderer>();
        boxCollider = GetComponent<Collider>();

        if (boxRenderer != null)
        {
            originalMaterial = boxRenderer.material;
        }

        greenMaterial = Resources.Load<Material>("GreenMaterial");
        redMaterial = Resources.Load<Material>("RedMaterial");

        if (greenMaterial == null || redMaterial == null)
        {
            Debug.LogError("Materials not found in Resources folder!");
        }
    }

    // Add methods to control visibility and interactability
    public void SetVisibility(bool visible)
    {
        isVisible = visible;
        if (boxRenderer != null)
        {
            boxRenderer.enabled = visible;
        }
    }

    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;
        if (boxCollider != null)
        {
            boxCollider.enabled = interactable;
        }
    }

    // Combine both operations for convenience
    public void SetVisibleAndInteractable(bool state)
    {
        SetVisibility(state);
        SetInteractable(state);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isInteractable) return;
        
        if (other.CompareTag("Left Baton") || other.CompareTag("Right Baton"))
        {
            CheckOrientation(other);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!isInteractable) return;
        
        if (other.CompareTag("Left Baton") || other.CompareTag("Right Baton"))
        {
            CheckOrientation(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isInteractable) return;
        
        if (other.CompareTag("Left Baton"))
        {
            leftHandInStance = false;
        }
        else if (other.CompareTag("Right Baton"))
        {
            rightHandInStance = false;
        }

        ResetMaterial();
        CheckStance();
    }

    private void CheckOrientation(Collider other)
    {
        if (boxRenderer == null || greenMaterial == null || redMaterial == null || batonTip == null)
            return;

        Vector3 batonDirection = (batonTip.position - transform.position).normalized;
        Vector3 worldCorrectDirection = transform.forward;

        float dot = Vector3.Dot(worldCorrectDirection.normalized, batonDirection);
        float distance = Vector3.Distance(batonTip.position, transform.position);

        bool inStance = (dot >= Mathf.Cos(angleThreshold * Mathf.Deg2Rad)) && (distance < positionThreshold);

        if (inStance)
        {
            boxRenderer.material = greenMaterial;
            if (other.CompareTag("Left Baton")) leftHandInStance = true;
            if (other.CompareTag("Right Baton")) rightHandInStance = true;
        }
        else
        {
            boxRenderer.material = redMaterial;
            if (other.CompareTag("Left Baton")) leftHandInStance = false;
            if (other.CompareTag("Right Baton")) rightHandInStance = false;
        }

        CheckStance();
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
        ResetMaterial();
        IsCompleted = false;
        
        // Make sure the box is visible and interactable by default when reset
        SetVisibleAndInteractable(true);
    }

    private void ResetMaterial()
    {
        if (boxRenderer != null && originalMaterial != null)
        {
            boxRenderer.material = originalMaterial;
        }
    }
}