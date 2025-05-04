using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using TMPro;

public class IntroManager : MonoBehaviour
{
    [Header("Arnis Requirements")]
    [SerializeField] private bool requireBothArnis = true;
    
    [Header("Initial UI")]
    public Canvas batonInstructionCanvas;
    public TextMeshProUGUI batonInstructionText;
    public Image batonInstructionImage;
    
    [Header("Stance UI")]
    public TextMeshProUGUI stanceInstructionText;
    public Image stanceInstructionImage;
    public string stanceInstructionMessage = "Stand in ready position";
    public Sprite stanceInstructionSprite;
    
    [Header("XR References")]
    public XRSocketInteractor leftBatonSocket;
    public XRSocketInteractor rightBatonSocket;

    [Header("Scene References")]
    public Transform roomTransform;
    public GameObject[] stanceBoxes;
    public StanceManager stanceManager;
    public LevelManager levelManager;
    
    [Header("Effect Settings")]
    public float roomRotationSpeed = 10f;
    public float fogDisappearSpeed = 0.5f;
    public float stanceHoldTime = 3f;

    private StanceDetector[] stanceDetectors;
    private bool[] isBoxHeld;
    private float[] holdTimers;
    private bool batonsRemoved = false;
    private bool stanceCompleted = false;

    private void Start()
    {
        InitializeScene();
    }

    private void InitializeScene()
    {
        batonsRemoved = false;
        stanceCompleted = false;
        
        RenderSettings.fog = true;
        RenderSettings.fogColor = Color.black;
        RenderSettings.fogDensity = 0.42f;

        if (stanceManager != null) stanceManager.gameObject.SetActive(false);
        if (levelManager != null) levelManager.gameObject.SetActive(false);

        InitializeStanceDetection();

        stanceInstructionText.gameObject.SetActive(false);
        stanceInstructionImage.gameObject.SetActive(false);
        foreach (var box in stanceBoxes)
        {
            box.SetActive(false);
        }

        ShowBatonInstruction();
    }

    private void InitializeStanceDetection()
    {
        stanceDetectors = new StanceDetector[stanceBoxes.Length];
        isBoxHeld = new bool[stanceBoxes.Length];
        holdTimers = new float[stanceBoxes.Length];

        for (int i = 0; i < stanceBoxes.Length; i++)
        {
            stanceDetectors[i] = stanceBoxes[i].GetComponent<StanceDetector>();
            isBoxHeld[i] = false;
            holdTimers[i] = 0f;
        }
    }

private void Update()
{
    if (!batonsRemoved)
    {
        CheckBatonRemoval();
    }
    else if (!stanceCompleted && stanceInstructionText.gameObject.activeSelf)
    {
        CheckStanceHold();
    }

    if (Input.GetKeyDown(KeyCode.S))
    {
        SkipIntro();
    }
}

    private void SkipIntro()
    {
        Debug.Log("Skipping intro...");
        
        StopAllCoroutines();

        RenderSettings.fog = false;

        batonInstructionCanvas.gameObject.SetActive(false);

        if (!batonsRemoved)
        {
            batonsRemoved = true;
        }

        if (!stanceCompleted)
        {
            stanceCompleted = true;

            foreach (var box in stanceBoxes)
            {
                box.SetActive(false);
            }
            stanceInstructionText.gameObject.SetActive(false);
            stanceInstructionImage.gameObject.SetActive(false);
        }

        stanceManager.gameObject.SetActive(true);
        levelManager.gameObject.SetActive(true);
        levelManager.StartLevel();

        this.enabled = false;
    }

    private void CheckBatonRemoval()
    {
        bool shouldProceed = requireBothArnis 
            ? (!leftBatonSocket.hasSelection && !rightBatonSocket.hasSelection)
            : (!leftBatonSocket.hasSelection || !rightBatonSocket.hasSelection);

        if (shouldProceed)
        {
            batonsRemoved = true;
            StartIntroSequence();
        }
    }

    private void StartIntroSequence()
    {
        batonInstructionCanvas.gameObject.SetActive(false);
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

        yield return new WaitForSeconds(0.5f);
        
        StartStancePhase();
    }

    private void StartStancePhase()
    {
        stanceInstructionText.text = stanceInstructionMessage;
        stanceInstructionImage.sprite = stanceInstructionSprite;
        stanceInstructionText.gameObject.SetActive(true);
        stanceInstructionImage.gameObject.SetActive(true);

        foreach (var box in stanceBoxes)
        {
            box.SetActive(true);
        }
    }

    private void CheckStanceHold()
    {
        bool allBoxesHeld = true;

        for (int i = 0; i < stanceDetectors.Length; i++)
        {
            if (stanceDetectors[i].IsLeftHandInStance() || stanceDetectors[i].IsRightHandInStance())
            {
                if (!isBoxHeld[i])
                {
                    isBoxHeld[i] = true;
                    holdTimers[i] = 0f;
                }

                holdTimers[i] += Time.deltaTime;

                if (holdTimers[i] < stanceHoldTime)
                {
                    allBoxesHeld = false;
                }
            }
            else
            {
                isBoxHeld[i] = false;
                holdTimers[i] = 0f;
                allBoxesHeld = false;
            }
        }

        if (allBoxesHeld && !stanceCompleted)
        {
            StartCoroutine(CompleteStancePhase());
        }
    }

    private IEnumerator CompleteStancePhase()
    {
        stanceCompleted = true;

        foreach (var box in stanceBoxes)
        {
            box.SetActive(false);
        }
        stanceInstructionText.gameObject.SetActive(false);
        stanceInstructionImage.gameObject.SetActive(false);

        yield return new WaitForSeconds(1f);

        stanceManager.gameObject.SetActive(true);
        levelManager.gameObject.SetActive(true);
        levelManager.StartLevel();

        this.enabled = false;
    }

    private void ShowBatonInstruction()
    {
        batonInstructionText.text = "Grab Batons using trigger";
        batonInstructionImage.gameObject.SetActive(true);
        batonInstructionCanvas.gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        RenderSettings.fog = false;
    }
}