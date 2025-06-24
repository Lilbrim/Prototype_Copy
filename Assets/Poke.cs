using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class Poke : MonoBehaviour
{
    [Header("Left Hand")]
    [SerializeField] private InputActionProperty leftTriggerAction;
    [SerializeField] private XRBaseInteractor[] leftHandPokeInteractors;

    [Header("Right Hand")]
    [SerializeField] private InputActionProperty rightTriggerAction;
    [SerializeField] private XRBaseInteractor[] rightHandPokeInteractors;

    private void OnEnable()
    {
        leftTriggerAction.action.Enable();
        rightTriggerAction.action.Enable();
    }

    private void OnDisable()
    {
        leftTriggerAction.action.Disable();
        rightTriggerAction.action.Disable();
    }

    private void Update()
    {
        
        bool leftTriggerPressed = leftTriggerAction.action.ReadValue<float>() > 0.1f;
        bool rightTriggerPressed = rightTriggerAction.action.ReadValue<float>() > 0.1f;

        
        foreach (var interactor in leftHandPokeInteractors)
        {
            interactor.gameObject.SetActive(leftTriggerPressed);
        }

        
        foreach (var interactor in rightHandPokeInteractors)
        {
            interactor.gameObject.SetActive(rightTriggerPressed);
        }
    }
}
