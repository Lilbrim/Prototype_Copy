using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class activate_ray : MonoBehaviour
{
    public GameObject leftRay;
    public GameObject rightRay;

    public InputActionProperty leftActivate;
    public InputActionProperty rightActivate;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        leftRay.SetActive(leftActivate.action.ReadValue<float>()>0.1f);
        rightRay.SetActive(rightActivate.action.ReadValue<float>()>0.1f);
    }
}
