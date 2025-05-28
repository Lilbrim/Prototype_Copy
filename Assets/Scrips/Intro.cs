using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using TMPro;

public class IntroManager : MonoBehaviour
{
    [Header("Arnis Requirements")]
    [SerializeField] private bool requireBothArnis = true;
    
    [Header("Instruction Screens")]
    [SerializeField] private InstructionScreens instructionScreens;
    
    [Header("Grab Batons UI")]
    public Canvas grabBatonCanvas;
    public TextMeshProUGUI grabBatonText;
    public Image grabBatonImage;
    
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
    public TutorialLevelManager TutorialLevelManager;
    
    [Header("Effect Settings")]
    public float roomRotationSpeed = 10f;
    public float fogDisappearSpeed = 0.5f;
    public float stanceHoldTime = 3f;

    private StanceDetector[] stanceDetectors;
    private bool[] isBoxHeld;
    private float[] holdTimers;
    private bool batonsRemoved = false;
    private bool heightCompleted = false;
    private bool stanceCompleted = false;
    private bool batonInstructionShown = false;
    private bool heightInstructionShown = false;
    private bool boxInstructionShown = false;

    private enum IntroState
    {
        BatonInstruction,
        GrabBaton,
        HeightInstruction,
        RoomRotation,
        BoxInstruction,
        StancePhase,
        Complete
    }
    private IntroState currentState = IntroState.BatonInstruction;

    private void Awake()
    {
        if (instructionScreens == null)
        {
            Debug.LogError("InstructionScreens reference is missing");
            return;
        }
        
        instructionScreens.onBatonInstructionComplete.AddListener(OnBatonInstructionComplete);
        instructionScreens.onHeightInstructionComplete.AddListener(OnHeightInstructionComplete);
        instructionScreens.onBoxInstructionComplete.AddListener(OnBoxInstructionComplete);
    }

    private void Start()
    {
        InitializeScene();
    }

    private void InitializeScene()
    {
        batonsRemoved = false;
        heightCompleted = false;
        stanceCompleted = false;
        batonInstructionShown = false;
        heightInstructionShown = false;
        boxInstructionShown = false;
        currentState = IntroState.BatonInstruction;
        
        RenderSettings.fog = true;
        RenderSettings.fogColor = Color.black;
        RenderSettings.fogDensity = 0.42f;

        if (stanceManager != null) stanceManager.gameObject.SetActive(false);
        if (TutorialLevelManager != null) TutorialLevelManager.gameObject.SetActive(false);

        InitializeStanceDetection();

        if (grabBatonCanvas != null)
            grabBatonCanvas.gameObject.SetActive(false);
            
        if (stanceInstructionText != null)
            stanceInstructionText.gameObject.SetActive(false);
            
        if (stanceInstructionImage != null)
            stanceInstructionImage.gameObject.SetActive(false);
            
        GameObject[] activeBoxes = GetActiveStanceBoxes();
        foreach (var box in activeBoxes)
        {
            box.SetActive(false);
        }

        ShowBatonWelcomeInstruction();
    }

    private GameObject[] GetActiveStanceBoxes()
    {
        if (stanceManager != null)
        {
            GameObject[] managerBoxes = stanceManager.GetIntroStanceBoxes();
            if (managerBoxes != null && managerBoxes.Length > 0)
            {
                return managerBoxes;
            }
        }
        
        return stanceBoxes;
    }

    private void InitializeStanceDetection()
    {
        GameObject[] activeBoxes = GetActiveStanceBoxes();
        
        stanceDetectors = new StanceDetector[activeBoxes.Length];
        isBoxHeld = new bool[activeBoxes.Length];
        holdTimers = new float[activeBoxes.Length];

        for (int i = 0; i < activeBoxes.Length; i++)
        {
            stanceDetectors[i] = activeBoxes[i].GetComponent<StanceDetector>();
            isBoxHeld[i] = false;
            holdTimers[i] = 0f;
        }
    }

    private void Update()
    {
        if (!batonsRemoved && batonInstructionShown)
        {
            CheckBatonRemoval();
        }
        else if (!stanceCompleted && stanceInstructionText.gameObject.activeSelf)
        {
            CheckStanceHold();
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            SkipToNextStep();
        }
    }

    private void SkipToNextStep()
    {
        Debug.Log($"Skipping from state: {currentState}");
        
        switch (currentState)
        {
            case IntroState.BatonInstruction:
                if (instructionScreens != null && instructionScreens.instructionCanvas != null)
                    instructionScreens.instructionCanvas.gameObject.SetActive(false);
                
                OnBatonInstructionComplete();
                currentState = IntroState.GrabBaton;
                break;
                
            case IntroState.GrabBaton:
                if (grabBatonCanvas != null)
                    grabBatonCanvas.gameObject.SetActive(false);
                
                batonsRemoved = true;
                currentState = IntroState.HeightInstruction;
                ShowHeightInstruction();
                break;
                
            case IntroState.HeightInstruction:
                if (instructionScreens != null && instructionScreens.instructionCanvas != null)
                    instructionScreens.instructionCanvas.gameObject.SetActive(false);
                
                OnHeightInstructionComplete();
                currentState = IntroState.RoomRotation;
                break;
                
            case IntroState.RoomRotation:
                StopAllCoroutines();
                
                float targetYRotation = roomTransform.eulerAngles.y - 0;
                roomTransform.rotation = Quaternion.Euler(0, targetYRotation, 0);
                
                RenderSettings.fog = false;
                RenderSettings.fogDensity = 0;
                
                currentState = IntroState.BoxInstruction;
                ShowBoxStanceInstruction();
                break;
                
            case IntroState.BoxInstruction:
                if (instructionScreens != null && instructionScreens.instructionCanvas != null)
                    instructionScreens.instructionCanvas.gameObject.SetActive(false);
                
                OnBoxInstructionComplete();
                currentState = IntroState.StancePhase;
                break;
                
            case IntroState.StancePhase:
                StopAllCoroutines();
                stanceCompleted = true;
                
                GameObject[] activeBoxes = GetActiveStanceBoxes();
                foreach (var box in activeBoxes)
                {
                    box.SetActive(false);
                }
                
                if (stanceInstructionText != null)
                    stanceInstructionText.gameObject.SetActive(false);
                    
                if (stanceInstructionImage != null)
                    stanceInstructionImage.gameObject.SetActive(false);
                
                currentState = IntroState.Complete;
                CompleteIntro();
                break;
                
            case IntroState.Complete:
                Debug.Log("Intro done");
                break;
        }
    }

    private void CompleteIntro()
    {
        stanceManager.gameObject.SetActive(true);
        TutorialLevelManager.gameObject.SetActive(true);
        TutorialLevelManager.StartLevel();
        this.enabled = false;
    }

    private void ShowBatonWelcomeInstruction()
    {
        currentState = IntroState.BatonInstruction;
        instructionScreens.ShowBatonInstruction();
    }
    
    private void OnBatonInstructionComplete()
    {
        batonInstructionShown = true;
        currentState = IntroState.GrabBaton;
        ShowGrabBatonInstruction();
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
        if (grabBatonCanvas != null)
            grabBatonCanvas.gameObject.SetActive(false);
            
        currentState = IntroState.HeightInstruction;
        ShowHeightInstruction();
    }

    private void ShowHeightInstruction()
    {
        instructionScreens.ShowHeightInstruction();
    }
    
    private void OnHeightInstructionComplete()
    {
        heightInstructionShown = true;
        heightCompleted = true;
        currentState = IntroState.RoomRotation;
        StartCoroutine(RotateRoomAndClearFog());
    }

    private IEnumerator RotateRoomAndClearFog()
    {
        float targetYRotation = roomTransform.eulerAngles.y - 0;
        
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
        
        currentState = IntroState.BoxInstruction;
        ShowBoxStanceInstruction();
    }
    
    private void ShowBoxStanceInstruction()
    {
        instructionScreens.ShowBoxInstruction();
    }
    
    private void OnBoxInstructionComplete()
    {
        boxInstructionShown = true;
        currentState = IntroState.StancePhase;
        StartStancePhase();
    }

    private void StartStancePhase()
    {
        stanceInstructionText.text = stanceInstructionMessage;
        stanceInstructionImage.sprite = stanceInstructionSprite;
        stanceInstructionText.gameObject.SetActive(true);
        stanceInstructionImage.gameObject.SetActive(true);

        GameObject[] activeBoxes = GetActiveStanceBoxes();
        foreach (var box in activeBoxes)
        {
            box.SetActive(true);
        }
    }

    private void CheckStanceHold()
    {
        bool allBoxesHeld = true;

        for (int i = 0; i < stanceDetectors.Length; i++)
        {
            if (stanceDetectors[i] != null && (stanceDetectors[i].IsLeftHandInStance() || stanceDetectors[i].IsRightHandInStance()))
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

        GameObject[] activeBoxes = GetActiveStanceBoxes();
        foreach (var box in activeBoxes)
        {
            box.SetActive(false);
        }
        stanceInstructionText.gameObject.SetActive(false);
        stanceInstructionImage.gameObject.SetActive(false);

        yield return new WaitForSeconds(1f);

        currentState = IntroState.Complete;
        CompleteIntro();
    }

    private void ShowGrabBatonInstruction()
    {
        grabBatonText.text = "Grab Batons using trigger";
        grabBatonImage.gameObject.SetActive(true);
        grabBatonCanvas.gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        RenderSettings.fog = false;
        
        if (instructionScreens != null)
        {
            instructionScreens.onBatonInstructionComplete.RemoveListener(OnBatonInstructionComplete);
            instructionScreens.onHeightInstructionComplete.RemoveListener(OnHeightInstructionComplete);
            instructionScreens.onBoxInstructionComplete.RemoveListener(OnBoxInstructionComplete);
        }
    }
}