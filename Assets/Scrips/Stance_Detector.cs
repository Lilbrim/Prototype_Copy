using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StanceDetector : MonoBehaviour
{
    public string stanceName;
    public GameObject[] stanceBoxes; 
    private bool leftHandInStance = false;
    private bool rightHandInStance = false;

    public bool IsLeftHandInStance() => leftHandInStance;
    public bool IsRightHandInStance() => rightHandInStance;

    private Material originalMaterial;
    private Material greenMaterial; 
    private Renderer boxRenderer; 

    private void Start()
    {
        boxRenderer = GetComponent<Renderer>();

        if (boxRenderer != null)
        {
            originalMaterial = boxRenderer.material;
        }

        greenMaterial = Resources.Load<Material>("GreenMaterial");

        if (greenMaterial == null)
        {
            Debug.LogError("GreenMaterial not found in Resources folder!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Left Baton"))
        {
            leftHandInStance = true;
        }
        else if (other.CompareTag("Right Baton"))
        {
            rightHandInStance = true;
        }

        if (boxRenderer != null && greenMaterial != null)
        {
            boxRenderer.material = greenMaterial;
        }

        CheckStance();
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
    }
}