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

    public void recenter()
    {
        XROrigin xrOrigin=GetComponent<XROrigin>();
        xrOrigin.MoveCameraToWorldLocation(target.position);
        xrOrigin.MatchOriginUpCameraForward(target.up, target.forward);
    }
}
