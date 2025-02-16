using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Holder : MonoBehaviour
{
    public XRSocketInteractor weaponSocket; 
    private XRGrabInteractable grabInteractable;
    private float timeSinceDropped = 0f;
    private bool isHeld = false;

    void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnDropped);
    }

    void Update()
    {
        if (!isHeld)
        {
            timeSinceDropped += Time.deltaTime;

            if (timeSinceDropped >= 5f)
            {
                ReturnToHolder();
            }
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isHeld = true;
        timeSinceDropped = 0f; 
    }

    private void OnDropped(SelectExitEventArgs args)
    {
        isHeld = false;
    }

    private void ReturnToHolder()
    {
        if (weaponSocket != null)
        {
            weaponSocket.StartManualInteraction(grabInteractable);
        }
        else
        {
            Debug.LogError("Weapon Socket is not assigned!");
        }
    }
}