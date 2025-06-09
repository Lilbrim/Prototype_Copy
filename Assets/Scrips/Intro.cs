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

    [Header("Dominant Hand Detection")]
    [SerializeField] private float dominantHandDetectionDelay = 0.5f;

    private StanceDetector[] stanceDetectors;
    private bool[] isBoxHeld;
    private float[] holdTimers;
    private bool batonsRemoved = false;
    private bool heightCompleted = false;
    private bool stanceCompleted = false;
    private bool batonInstructionShown = false;
    private bool heightInstructionShown = false;
    private bool boxInstructionShown = false;
    
    private bool leftBatonGrabbed = false;
    private bool rightBatonGrabbed = false;
    private bool dominantHandDetected = false;
    private float firstGrabTime = 0f;
    private bool isRightHandDominant = false;
    private bool recenterCompleted = false;
    private bool recenterInstructionShown = false;
    private enum IntroState
    {
        BatonInstruction,
        GrabBaton,
        HeightInstruction,
        RecenterInstruction,
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
        instructionScreens.onRecenterInstructionComplete.AddListener(OnRecenterInstructionComplete);
        instructionScreens.onBoxInstructionComplete.AddListener(OnBoxInstructionComplete);
    }


    private void Start()
    {
        InitializeScene();
        SetupBatonSocketListeners();
    }

    private void SetupBatonSocketListeners()
    {
        if (leftBatonSocket != null)
        {
            leftBatonSocket.selectExited.AddListener(OnLeftBatonRemoved);
        }
        
        if (rightBatonSocket != null)
        {
            rightBatonSocket.selectExited.AddListener(OnRightBatonRemoved);
        }
    }

    private void OnLeftBatonRemoved(SelectExitEventArgs args)
    {
        if (!dominantHandDetected && currentState == IntroState.GrabBaton)
        {
            leftBatonGrabbed = true;
            
            if (!rightBatonGrabbed)
            {
                firstGrabTime = Time.time;
                StartCoroutine(CheckDominantHand());
            }
            else
            {
                DetermineDominantHand();
            }
        }
    }

    private void OnRightBatonRemoved(SelectExitEventArgs args)
    {
        if (!dominantHandDetected && currentState == IntroState.GrabBaton)
        {
            rightBatonGrabbed = true;
            
            if (!leftBatonGrabbed)
            {
                firstGrabTime = Time.time;
                StartCoroutine(CheckDominantHand());
            }
            else
            {
                DetermineDominantHand();
            }
        }
    }

    private IEnumerator CheckDominantHand()
    {
        yield return new WaitForSeconds(dominantHandDetectionDelay);
        
        if (!dominantHandDetected)
        {
            DetermineDominantHand();
        }
    }

    private void DetermineDominantHand()
    {
        if (dominantHandDetected) return;
        
        dominantHandDetected = true;
        
        if (leftBatonGrabbed && rightBatonGrabbed)
        {
        }
        else if (leftBatonGrabbed && !rightBatonGrabbed)
        {
            isRightHandDominant = false;
            Debug.Log("Left hand dominant detected (grabbed left baton first)");
        }
        else if (rightBatonGrabbed && !leftBatonGrabbed)
        {
            isRightHandDominant = true;
            Debug.Log("Right hand dominant detected (grabbed right baton first)");
        }
        
        if (stanceManager != null)
        {
            if (!stanceManager.gameObject.activeInHierarchy)
            {
                stanceManager.gameObject.SetActive(true);
                Debug.Log("Activated StanceManager for dominant hand setup");
            }
            
            stanceManager.SetRightHandDominant(isRightHandDominant);
            // Keep game inactive during intro to prevent objective triggering
            stanceManager.SetGameActive(false);
            Debug.Log($"Set StanceManager right hand dominant to: {isRightHandDominant}");
            
            InitializeStanceDetection();
            
        }
        
        UpdateGrabBatonUI();
    }

    private void UpdateGrabBatonUI()
    {
        if (grabBatonText != null)
        {
            string dominantHandText = isRightHandDominant ? "right" : "left";
            string nonDominantText = isRightHandDominant ? "left" : "right";
            
            if (requireBothArnis)
            {
                grabBatonText.text = $"Grab remaining baton with {nonDominantText} hand";
            }
            else
            {
                grabBatonText.text = $"Dominant hand: {dominantHandText}. Continue with tutorial.";
            }
        }
    }

    private void InitializeScene()
    {
        batonsRemoved = false;
        heightCompleted = false;
        recenterCompleted = false;
        stanceCompleted = false;
        batonInstructionShown = false;
        heightInstructionShown = false;
        recenterInstructionShown = false;
        boxInstructionShown = false;
        
        leftBatonGrabbed = false;
        rightBatonGrabbed = false;
        dominantHandDetected = false;
        isRightHandDominant = false;
        firstGrabTime = 0f;
        
        currentState = IntroState.BatonInstruction;
        
        RenderSettings.fog = true;
        RenderSettings.fogColor = Color.black;
        RenderSettings.fogDensity = 0.42f;

        if (stanceManager != null) 
        {
            stanceManager.gameObject.SetActive(true);
            // Tell StanceManager we're in intro mode to prevent triggering objectives
            stanceManager.SetGameActive(false);
            Debug.Log("StanceManager kept active during intro initialization");
        }
        
        if (TutorialLevelManager != null) 
        {
            TutorialLevelManager.gameObject.SetActive(false);
            TutorialLevelManager.enabled = false; // Fully disable it
        }

        
        if (grabBatonCanvas != null)
            grabBatonCanvas.gameObject.SetActive(false);
            
        if (stanceInstructionText != null)
            stanceInstructionText.gameObject.SetActive(false);
            
        if (stanceInstructionImage != null)
            stanceInstructionImage.gameObject.SetActive(false);
            
        HideAllStanceBoxes();

        ShowBatonWelcomeInstruction();
    }
    private void HideAllStanceBoxes()
    {
        GameObject[] activeBoxes = GetActiveStanceBoxes();
        foreach (var box in activeBoxes)
        {
            if (box != null)
                box.SetActive(false);
        }
    }

    private GameObject[] GetActiveStanceBoxes()
    {
        if (stanceManager != null)
        {
            bool wasActive = stanceManager.gameObject.activeInHierarchy;
            if (!wasActive)
            {
                stanceManager.gameObject.SetActive(true);
                Debug.Log("Temporarily activated StanceManager to get intro boxes");
            }
            
            GameObject[] managerBoxes = stanceManager.GetIntroStanceBoxes();
            if (managerBoxes != null && managerBoxes.Length > 0)
            {
                Debug.Log($"Using StanceManager intro boxes: {managerBoxes.Length} boxes, Right hand dominant: {isRightHandDominant}");
                return managerBoxes;
            }
        }
        
        Debug.Log($"Using local stance boxes: {stanceBoxes?.Length ?? 0} boxes");
        return stanceBoxes ?? new GameObject[0];
    }

    private void InitializeStanceDetection()
    {
        GameObject[] activeBoxes = GetActiveStanceBoxes();
        
        if (activeBoxes.Length == 0)
        {
            Debug.LogWarning("No stance boxes found for initialization!");
            return;
        }
        
        stanceDetectors = new StanceDetector[activeBoxes.Length];
        isBoxHeld = new bool[activeBoxes.Length];
        holdTimers = new float[activeBoxes.Length];

        for (int i = 0; i < activeBoxes.Length; i++)
        {
            if (activeBoxes[i] != null)
            {
                stanceDetectors[i] = activeBoxes[i].GetComponent<StanceDetector>();
                isBoxHeld[i] = false;
                holdTimers[i] = 0f;
                
                if (stanceDetectors[i] == null)
                {
                    Debug.LogWarning($"StanceDetector not found on box: {activeBoxes[i].name}");
                }
            }
        }
        
        Debug.Log($"Initialized stance detection with {activeBoxes.Length} boxes");
    }

    private void Update()
    {
        if (!batonsRemoved && batonInstructionShown)
        {
            CheckBatonRemoval();
        }
        else if (!stanceCompleted && stanceInstructionText != null && stanceInstructionText.gameObject.activeSelf)
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
                
                if (!dominantHandDetected)
                {
                    isRightHandDominant = true;
                    dominantHandDetected = true;
                    if (stanceManager != null)
                    {
                        if (!stanceManager.gameObject.activeInHierarchy)
                        {
                            stanceManager.gameObject.SetActive(true);
                            Debug.Log("Activated StanceManager during skip");
                        }
                        stanceManager.SetRightHandDominant(isRightHandDominant);
                        stanceManager.SetGameActive(false); // Keep in intro mode
                        InitializeStanceDetection();
                    }
                }
                
                batonsRemoved = true;
                currentState = IntroState.HeightInstruction;
                ShowHeightInstruction();
                break;
                
            case IntroState.HeightInstruction:
                if (instructionScreens != null && instructionScreens.instructionCanvas != null)
                    instructionScreens.instructionCanvas.gameObject.SetActive(false);
                
                OnHeightInstructionComplete();
                currentState = IntroState.RecenterInstruction;
                break;
                
            case IntroState.RecenterInstruction:
                if (instructionScreens != null && instructionScreens.instructionCanvas != null)
                    instructionScreens.instructionCanvas.gameObject.SetActive(false);
                
                OnRecenterInstructionComplete();
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
                
                HideAllStanceBoxes();
                
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
        if (stanceManager != null)
        {
            stanceManager.gameObject.SetActive(true);
            stanceManager.SetGameActive(true); // Now activate game mode
            Debug.Log("StanceManager activated in CompleteIntro()");
        }
            
        if (TutorialLevelManager != null)
        {
            TutorialLevelManager.enabled = true; // Re-enable the component
            TutorialLevelManager.gameObject.SetActive(true);
            TutorialLevelManager.StartLevel();
        }
        
        this.enabled = false;
    }

    [ContextMenu("Force Activate StanceManager")]
    public void ForceActivateStanceManager()
    {
        if (stanceManager != null)
        {
            stanceManager.gameObject.SetActive(true);
            Debug.Log("Force activated StanceManager");
        }
        else
        {
            Debug.LogError("StanceManager reference is null!");
        }
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
        currentState = IntroState.RecenterInstruction;
        ShowRecenterInstruction();
    }

    private void ShowRecenterInstruction()
    {
        instructionScreens.ShowRecenterInstruction();
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
    
    private void OnRecenterInstructionComplete()
    {
        recenterInstructionShown = true;
        recenterCompleted = true;
        currentState = IntroState.RoomRotation;
        StartCoroutine(RotateRoomAndClearFog());
    }

    private void StartStancePhase()
    {
        if (stanceDetectors == null || stanceDetectors.Length == 0)
        {
            InitializeStanceDetection();
        }

        // Ensure StanceManager is in intro mode, not game mode
        if (stanceManager != null)
        {
            stanceManager.SetGameActive(false);
        }

        if (stanceInstructionText != null)
            stanceInstructionText.text = stanceInstructionMessage;

        if (stanceInstructionImage != null)
            stanceInstructionImage.sprite = stanceInstructionSprite;

        if (stanceInstructionText != null)
            stanceInstructionText.gameObject.SetActive(true);

        if (stanceInstructionImage != null)
            stanceInstructionImage.gameObject.SetActive(true);

        GameObject[] activeBoxes = GetActiveStanceBoxes();
        foreach (var box in activeBoxes)
        {
            if (box != null)
                box.SetActive(true);
        }

        Debug.Log($"Started stance phase with {activeBoxes.Length} boxes");
    }

    private void CheckStanceHold()
    {
        if (stanceDetectors == null || stanceDetectors.Length == 0)
        {
            Debug.LogWarning("Stance detectors not initialized!");
            return;
        }
        
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

        HideAllStanceBoxes();
        
        if (stanceInstructionText != null)
            stanceInstructionText.gameObject.SetActive(false);
            
        if (stanceInstructionImage != null)
            stanceInstructionImage.gameObject.SetActive(false);

        yield return new WaitForSeconds(1f);

        currentState = IntroState.Complete;
        CompleteIntro();
    }

    private void ShowGrabBatonInstruction()
    {
        if (grabBatonText != null)
            grabBatonText.text = "Grab Baton Depending on your dominant hand.";
            
        if (grabBatonImage != null)
            grabBatonImage.gameObject.SetActive(true);
            
        if (grabBatonCanvas != null)
            grabBatonCanvas.gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        RenderSettings.fog = false;
        
        if (instructionScreens != null)
        {
            instructionScreens.onBatonInstructionComplete.RemoveListener(OnBatonInstructionComplete);
            instructionScreens.onHeightInstructionComplete.RemoveListener(OnHeightInstructionComplete);
            instructionScreens.onRecenterInstructionComplete.RemoveListener(OnRecenterInstructionComplete);
            instructionScreens.onBoxInstructionComplete.RemoveListener(OnBoxInstructionComplete);
        }
        
        if (leftBatonSocket != null)
        {
            leftBatonSocket.selectExited.RemoveListener(OnLeftBatonRemoved);
        }
        
        if (rightBatonSocket != null)
        {
            rightBatonSocket.selectExited.RemoveListener(OnRightBatonRemoved);
        }
    }
    
    public bool GetIsRightHandDominant()
    {
        return isRightHandDominant;
    }

    public bool IsDominantHandDetected()
    {
        return dominantHandDetected;
    }
}