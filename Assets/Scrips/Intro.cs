using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using TMPro;

public class IntroScript : MonoBehaviour
{
    public Canvas instructionCanvas;
    public TextMeshProUGUI instructionText;
    public Image instructionImage;
    public Image holdBatonImage;
    public XRSocketInteractor leftBatonSocket;
    public XRSocketInteractor rightBatonSocket;

    public StanceDetector[] stanceBoxes;

    public Transform roomTransform;
    public float roomRotationSpeed = 10f;
    public float fogDisappearSpeed = 0.5f;

    private bool batonsRemoved = false; 
    private bool stanceCompleted = false;

    private void Start()
    {
        batonsRemoved = false;
        stanceCompleted = false;

        foreach (var box in stanceBoxes)
        {
            box.ResetStance(); 
        }

        RenderSettings.fog = true;
        RenderSettings.fogColor = Color.black;
        RenderSettings.fogDensity = 0.2f;

        foreach (var box in stanceBoxes)
        {
            box.gameObject.SetActive(false);
        }

        ShowInstruction("Grab Batons using trigger", instructionImage);
    }

    private void Update()
    {
        if (!batonsRemoved && !leftBatonSocket.hasSelection && !rightBatonSocket.hasSelection)
        {
            Debug.Log("Both batons removed from sockets!");
            batonsRemoved = true;
            OnBatonsRemoved();
        }

        if (batonsRemoved && !stanceCompleted && AreAllStanceBoxesCompleted())
        {
            Debug.Log("Stance completed!");
            stanceCompleted = true;
            OnStanceCompleted();
        }
    }

    private void OnBatonsRemoved()
    {
        ShowInstruction("Hold the batons in the correct position", holdBatonImage);
        foreach (var box in stanceBoxes)
        {
            box.gameObject.SetActive(true);
        }
    }

    private void OnStanceCompleted()
    {
        StartCoroutine(RotateRoomAndClearFog());
    }

    private IEnumerator RotateRoomAndClearFog()
    {
        float targetYRotation = roomTransform.eulerAngles.y - 90;
        
        while (Mathf.Abs(Mathf.DeltaAngle(roomTransform.eulerAngles.y, targetYRotation)) > 0.1f)
        {
            roomTransform.rotation = Quaternion.RotateTowards(
                roomTransform.rotation, 
                Quaternion.Euler(0, targetYRotation, 0), 
                roomRotationSpeed * Time.deltaTime
            );
            yield return null;
        }
        
        roomTransform.rotation = Quaternion.Euler(0, targetYRotation, 0);
        while (RenderSettings.fogDensity > 0.01f)
        {
            RenderSettings.fogDensity = Mathf.Max(RenderSettings.fogDensity - fogDisappearSpeed * Time.deltaTime, 0);
            yield return null;
        }
        RenderSettings.fog = false;
        
        instructionCanvas.gameObject.SetActive(false);
        foreach (var box in stanceBoxes)
        {
            box.gameObject.SetActive(false);
        }

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.gameObject.SetActive(true);
            LevelManager.Instance.StartObjective(LevelManager.Instance.objectives[0]);
        }
        else
        {
            Debug.LogError("LevelManager instance not found! Make sure it's in the scene and not destroyed.");
        }

        this.enabled = false;
    }


    private void ShowInstruction(string instruction, Image image)
    {
        instructionText.text = instruction;
        instructionImage.gameObject.SetActive(false); 
        holdBatonImage.gameObject.SetActive(false); 
        image.gameObject.SetActive(true); 
        instructionCanvas.gameObject.SetActive(true);
    }

    private bool AreAllStanceBoxesCompleted()
    {
        foreach (var box in stanceBoxes)
        {
            if (!box.IsCompleted)
            {
                return false;
            }
        }
        return true;
    }
}


