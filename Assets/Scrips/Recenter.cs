using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Recenter : MonoBehaviour
{
    public Transform head;
    public Transform origin;
    public Transform target;

    private Pause pauseScript;

    private void Start()
    {
        pauseScript = FindObjectOfType<Pause>();    
        StartCoroutine(RecenterAfterDelay(0.1f));
    }

    private IEnumerator RecenterAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        recenter();
    }

    public void recenter()
    {
        if (head == null || origin == null || target == null)
        {
            Debug.LogWarning("Recenter: Missing required transform references");
            return;
        }
        
        Vector3 targetForward = target.forward;
        targetForward.y = 0;
        targetForward.Normalize();
        
        Vector3 headForward = head.forward;
        headForward.y = 0;
        headForward.Normalize();
        
        float angleToRotate = Vector3.SignedAngle(headForward, targetForward, Vector3.up);
        origin.Rotate(0, angleToRotate, 0);

        Vector3 headOffset = head.position - origin.position;
        origin.position = target.position - headOffset;

        if (pauseScript != null && Time.timeScale == 0)
        {
            pauseScript.Resume();
        }
        else
        {
            Time.timeScale = 1;
        }
    }
}