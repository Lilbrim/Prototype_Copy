using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;

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
        float originalYRotation = origin.eulerAngles.y;

        Vector3 headOffset = head.position - origin.position;
        origin.position = target.position - headOffset;


        origin.rotation = Quaternion.Euler(0, originalYRotation, 0);

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
