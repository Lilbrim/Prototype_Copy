using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HandAnimate : MonoBehaviour
{
    public InputActionProperty GripAnim;
    public InputActionProperty TriggerAnim;
    public Animator HandAnim;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float gripValue = GripAnim.action.ReadValue<float>();
        HandAnim.SetFloat("Grip", gripValue);
        float triggerValue = TriggerAnim.action.ReadValue<float>();
        HandAnim.SetFloat("Trigger", triggerValue);
    }
}
