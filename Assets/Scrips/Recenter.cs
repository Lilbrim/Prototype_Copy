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
    }

    public void recenter()
    {
        if (head == null || origin == null || target == null)
        {
            Debug.LogWarning("Recenter: Missing required transform references");
            return;
        }

        Vector3 headOffset = head.position - origin.position;
        
        origin.position = target.position - headOffset;

        Vector3 targetForward = target.forward;
        targetForward.y = 0;
        targetForward.Normalize();
        
        Vector3 headForward = head.forward;
        headForward.y = 0;
        headForward.Normalize();
        
        float angleToRotate = Vector3.SignedAngle(headForward, targetForward, Vector3.up);
        
        origin.Rotate(0, angleToRotate, 0);

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