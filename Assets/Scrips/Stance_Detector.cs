using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StanceDetector : MonoBehaviour
{
    public string stanceName;
    public GameObject[] stanceBoxes;

    public Transform batonTip;
    public Vector3 correctDirection = Vector3.forward;
    public float angleThreshold = 30f;

    private bool leftHandInStance = false;
    private bool rightHandInStance = false;
      public bool IsCompleted { get; set; } = false;

    public bool IsLeftHandInStance() => leftHandInStance;
    public bool IsRightHandInStance() => rightHandInStance;

    private Material originalMaterial;
    private Material greenMaterial;
    private Material redMaterial;
    private Renderer boxRenderer;

    private void Start()
    {
        boxRenderer = GetComponent<Renderer>();

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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Left Baton") || other.CompareTag("Right Baton"))
        {
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
        }
        else if (other.CompareTag("Right Baton"))
        {
            rightHandInStance = false;
        }

        if (boxRenderer != null && originalMaterial != null)
        {
            boxRenderer.material = originalMaterial;
        }

        CheckStance(); 
    }

    private void CheckOrientation(Collider other)
    {
        if (boxRenderer == null || greenMaterial == null || redMaterial == null || batonTip == null)
            return;

        Vector3 batonDirection = (batonTip.position - transform.position).normalized;

        Vector3 worldCorrectDirection = transform.forward;

        float angle = Vector3.Angle(worldCorrectDirection, batonDirection);

        if (angle <= angleThreshold)
        {
            boxRenderer.material = greenMaterial; 
            if (other.CompareTag("Left Baton"))
            {
                leftHandInStance = true;
            }
            else if (other.CompareTag("Right Baton"))
            {
                rightHandInStance = true;
            }
        }
        else
        {
            boxRenderer.material = redMaterial; 
            if (other.CompareTag("Left Baton"))
            {
                leftHandInStance = false;
            }
            else if (other.CompareTag("Right Baton"))
            {
                rightHandInStance = false;
            }
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

        if (boxRenderer != null && originalMaterial != null)
        {
            boxRenderer.material = originalMaterial;
        }
         IsCompleted = false; 
    }
}