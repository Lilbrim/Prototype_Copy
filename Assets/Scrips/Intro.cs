using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using TMPro;

public class IntroManager : MonoBehaviour
{
    [Header("Instruction Screens")]
    [SerializeField] private InstructionScreens instructionScreens;
    
    [Header("Stance UI")]
    public TextMeshProUGUI stanceInstructionText;
    public Image stanceInstructionImage;
    public string stanceInstructionMessage = "Stand in ready position";
    public Sprite stanceInstructionSprite;

    [Header("Hand Selection UI")]
    public Canvas handSelectionCanvas;
    public Button leftHandButton;
    public Button rightHandButton;
    public TextMeshProUGUI handSelectionText;

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
    private bool heightCompleted = false;
    private bool stanceCompleted = false;
    private bool heightInstructionShown = false;
    private bool boxInstructionShown = false;
    
    private bool dominantHandDetected = false;
    private bool isRightHandDominant = false;
    private bool recenterCompleted = false;
    private bool recenterInstructionShown = false;
    
    private enum IntroState
    {
        HandSelection,
        HeightInstruction,
        RecenterInstruction,
        RoomRotation,
        BoxInstruction,
        StancePhase,
        Complete
    }
    private IntroState currentState = IntroState.HandSelection;

    private void Awake()
    {
        if (instructionScreens == null)
        {
            Debug.LogError("InstructionScreens reference is missing");
            return;
        }
        
        instructionScreens.onHeightInstructionComplete.AddListener(OnHeightInstructionComplete);
        instructionScreens.onRecenterInstructionComplete.AddListener(OnRecenterInstructionComplete);
        instructionScreens.onBoxInstructionComplete.AddListener(OnBoxInstructionComplete);
    }

    private void Start()
    {
        InitializeScene();
        SetupHandSelectionButtons();
    }

    private void SetupHandSelectionButtons()
    {
        if (leftHandButton != null)
        {
            leftHandButton.onClick.AddListener(OnLeftHandSelected);
        }
        
        if (rightHandButton != null)
        {
            rightHandButton.onClick.AddListener(OnRightHandSelected);
        }
    }
    
    private void OnLeftHandSelected()
    {
        if (!dominantHandDetected && currentState == IntroState.HandSelection)
        {
            isRightHandDominant = false;
            dominantHandDetected = true;
            Debug.Log("Left hand selected as dominant");
            
            SetupDominantHand();
        }
    }

    private void OnRightHandSelected()
    {
        if (!dominantHandDetected && currentState == IntroState.HandSelection)
        {
            isRightHandDominant = true;
            dominantHandDetected = true;
            Debug.Log("Right hand selected as dominant");
            
            SetupDominantHand();
        }
    }

    private void SetupDominantHand()
    {
        if (stanceManager != null)
        {
            if (!stanceManager.gameObject.activeInHierarchy)
            {
                stanceManager.gameObject.SetActive(true);
                Debug.Log("Activated StanceManager for dominant hand setup");
            }
            
            stanceManager.SetRightHandDominant(isRightHandDominant);
            stanceManager.SetGameActive(false); 
            Debug.Log($"Set StanceManager right hand dominant to: {isRightHandDominant}");
            
            InitializeStanceDetection();
        }
        
        
        if (handSelectionCanvas != null)
            handSelectionCanvas.gameObject.SetActive(false);
            
        StartIntroSequence();
    }

    private void InitializeScene()
    {
        heightCompleted = false;
        recenterCompleted = false;
        stanceCompleted = false;
        heightInstructionShown = false;
        recenterInstructionShown = false;
        boxInstructionShown = false;
        
        dominantHandDetected = false;
        isRightHandDominant = false;
        
        currentState = IntroState.HandSelection;
        
        RenderSettings.fog = true;
        RenderSettings.fogColor = Color.black;
        RenderSettings.fogDensity = 0.42f;

        if (stanceManager != null) 
        {
            stanceManager.gameObject.SetActive(true);
            stanceManager.SetGameActive(false);
            Debug.Log("StanceManager kept active during intro initialization");
        }
        
        if (TutorialLevelManager != null) 
        {
            TutorialLevelManager.gameObject.SetActive(false);
            TutorialLevelManager.enabled = false;
        }

        
        if (handSelectionCanvas != null)
            handSelectionCanvas.gameObject.SetActive(true);
            
        if (stanceInstructionText != null)
            stanceInstructionText.gameObject.SetActive(false);
            
        if (stanceInstructionImage != null)
            stanceInstructionImage.gameObject.SetActive(false);
            
        HideAllStanceBoxes();

        ShowHandSelectionInstruction();
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
        if (!stanceCompleted && stanceInstructionText != null && stanceInstructionText.gameObject.activeSelf)
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
            case IntroState.HandSelection:
                if (handSelectionCanvas != null)
                    handSelectionCanvas.gameObject.SetActive(false);
                
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
                        stanceManager.SetGameActive(false);
                        InitializeStanceDetection();
                    }
                }
                
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
            stanceManager.SetGameActive(true); 
            Debug.Log("StanceManager activated in CompleteIntro()");
        }
            
        if (TutorialLevelManager != null)
        {
            TutorialLevelManager.enabled = true; 
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

    private void ShowHandSelectionInstruction()
    {
        if (handSelectionText != null)
            handSelectionText.text = "Select your dominant hand:";
            
        if (handSelectionCanvas != null)
            handSelectionCanvas.gameObject.SetActive(true);
    }
    
    private void StartIntroSequence()
    {
        if (handSelectionCanvas != null)
            handSelectionCanvas.gameObject.SetActive(false);
            
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

    private void OnDestroy()
    {
        RenderSettings.fog = false;
        
        if (instructionScreens != null)
        {
            instructionScreens.onHeightInstructionComplete.RemoveListener(OnHeightInstructionComplete);
            instructionScreens.onRecenterInstructionComplete.RemoveListener(OnRecenterInstructionComplete);
            instructionScreens.onBoxInstructionComplete.RemoveListener(OnBoxInstructionComplete);
        }
        
        
        if (leftHandButton != null)
        {
            leftHandButton.onClick.RemoveListener(OnLeftHandSelected);
        }
        
        if (rightHandButton != null)
        {
            rightHandButton.onClick.RemoveListener(OnRightHandSelected);
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