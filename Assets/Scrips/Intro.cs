using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using TMPro;

public class IntroManager : MonoBehaviour
{
    [Header("Intro Completion Settings")]
    [SerializeField] private bool forceIntroComplete = false;
    [SerializeField] private bool resetIntroOnStart = false;
    
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
    public StanceManager stanceManager;
    public LevelSelector levelSelector;
    
    [Header("Effect Settings")]
    public float fogDisappearSpeed = 0.5f;

    [Header("Dominant Hand Detection")]
    [SerializeField] private float dominantHandDetectionDelay = 0.5f;

    private const string INTRO_COMPLETE_KEY = "IntroManagerComplete";
    
    private bool heightCompleted = false;
    private bool heightInstructionShown = false;
    private bool recenterCompleted = false;
    private bool recenterInstructionShown = false;
    private bool batonCompleted = false;
    private bool batonInstructionShown = false;
    private bool boxCompleted = false;
    private bool boxInstructionShown = false;
    
    private bool dominantHandDetected = false;
    private bool isRightHandDominant = false;
    
    private enum IntroState
    {
        HandSelection,
        HeightInstruction,
        RecenterInstruction,
        BatonInstruction,
        BoxInstruction,
        FogClear,
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
        instructionScreens.onBatonInstructionComplete.AddListener(OnBatonInstructionComplete);
        instructionScreens.onBoxInstructionComplete.AddListener(OnBoxInstructionComplete);
    }

    private void Start()
    {
        
        if (resetIntroOnStart)
        {
            PlayerPrefs.SetInt(INTRO_COMPLETE_KEY, 0);
            PlayerPrefs.Save();
            Debug.Log("Intro completion reset via inspector setting");
        }
        
        if (forceIntroComplete)
        {
            PlayerPrefs.SetInt(INTRO_COMPLETE_KEY, 1);
            PlayerPrefs.Save();
            Debug.Log("Intro completion forced via inspector setting");
        }
        
        
        bool introCompleted = PlayerPrefs.GetInt(INTRO_COMPLETE_KEY, 0) == 1;
        
        if (introCompleted)
        {
            Debug.Log("Intro already completed, skipping to level selector");
            SkipIntroToLevelSelector();
        }
        else
        {
            Debug.Log("Starting intro sequence");
            InitializeScene();
            SetupHandSelectionButtons();
        }
    }
    

    private void SkipIntroToLevelSelector()
    {
        
        if (handSelectionCanvas != null)
            handSelectionCanvas.gameObject.SetActive(false);
            
        if (stanceInstructionText != null)
            stanceInstructionText.gameObject.SetActive(false);
            
        if (stanceInstructionImage != null)
            stanceInstructionImage.gameObject.SetActive(false);
            
        if (instructionScreens != null && instructionScreens.instructionCanvas != null)
            instructionScreens.instructionCanvas.gameObject.SetActive(false);
        
        
        RenderSettings.fog = false;
        RenderSettings.fogDensity = 0;
        
        
        isRightHandDominant = PlayerPrefs.GetInt("DominantHandRight", 1) == 1;
        dominantHandDetected = true;
        
        
        if (stanceManager != null)
        {
            stanceManager.gameObject.SetActive(true);
            stanceManager.SetRightHandDominant(isRightHandDominant);
            stanceManager.SetGameActive(true);
            Debug.Log($"StanceManager setup with saved dominant hand: {(isRightHandDominant ? "Right" : "Left")}");
        }
        
        
        if (levelSelector != null)
        {
            levelSelector.enabled = true;
            levelSelector.gameObject.SetActive(true);
            
            
            levelSelector.StartStoryModeWithTutorialInGym();
            
            Debug.Log("Level selector activated in story mode");
        }
        
        
        currentState = IntroState.Complete;
        this.enabled = false;
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
            
            
            PlayerPrefs.SetInt("DominantHandRight", 0);
            PlayerPrefs.Save();
            
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
            
            
            PlayerPrefs.SetInt("DominantHandRight", 1);
            PlayerPrefs.Save();
            
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
        }
        
        if (handSelectionCanvas != null)
            handSelectionCanvas.gameObject.SetActive(false);
            
        StartIntroSequence();
    }

    private void InitializeScene()
    {
        heightCompleted = false;
        recenterCompleted = false;
        batonCompleted = false;
        boxCompleted = false;
        heightInstructionShown = false;
        recenterInstructionShown = false;
        batonInstructionShown = false;
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
        
        if (levelSelector != null) 
        {
            levelSelector.gameObject.SetActive(false);
            levelSelector.enabled = false;
        }

        if (handSelectionCanvas != null)
            handSelectionCanvas.gameObject.SetActive(true);
            
        if (stanceInstructionText != null)
            stanceInstructionText.gameObject.SetActive(false);
            
        if (stanceInstructionImage != null)
            stanceInstructionImage.gameObject.SetActive(false);

        ShowHandSelectionInstruction();
    }


    private void Update()
    {
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
                    
                    
                    PlayerPrefs.SetInt("DominantHandRight", 1);
                    PlayerPrefs.Save();
                    
                    if (stanceManager != null)
                    {
                        if (!stanceManager.gameObject.activeInHierarchy)
                        {
                            stanceManager.gameObject.SetActive(true);
                            Debug.Log("Activated StanceManager during skip");
                        }
                        stanceManager.SetRightHandDominant(isRightHandDominant);
                        stanceManager.SetGameActive(false);
                    }
                }
                
                currentState = IntroState.HeightInstruction;
                ShowHeightInstruction();
                break;
                
            case IntroState.HeightInstruction:
                if (instructionScreens != null && instructionScreens.instructionCanvas != null)
                    instructionScreens.instructionCanvas.gameObject.SetActive(false);
                
                OnHeightInstructionComplete();
                break;
                
            case IntroState.RecenterInstruction:
                if (instructionScreens != null && instructionScreens.instructionCanvas != null)
                    instructionScreens.instructionCanvas.gameObject.SetActive(false);
                
                OnRecenterInstructionComplete();
                break;
                
            case IntroState.BatonInstruction:
                if (instructionScreens != null && instructionScreens.instructionCanvas != null)
                    instructionScreens.instructionCanvas.gameObject.SetActive(false);
                
                OnBatonInstructionComplete();
                break;
                
            case IntroState.BoxInstruction:
                if (instructionScreens != null && instructionScreens.instructionCanvas != null)
                    instructionScreens.instructionCanvas.gameObject.SetActive(false);
                
                OnBoxInstructionComplete();
                break;
                
            case IntroState.FogClear:
                StopAllCoroutines();
                
                RenderSettings.fog = false;
                RenderSettings.fogDensity = 0;
                
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
        PlayerPrefs.SetInt(INTRO_COMPLETE_KEY, 1);
        PlayerPrefs.Save();
        Debug.Log("Intro completed and saved to PlayerPrefs");
        
        if (stanceManager != null)
        {
            stanceManager.gameObject.SetActive(true);
            stanceManager.SetGameActive(true); 
            Debug.Log("StanceManager activated in CompleteIntro()");
        }
            
        if (levelSelector != null)
        {
            levelSelector.enabled = true; 
            levelSelector.gameObject.SetActive(true);
            
            
            levelSelector.StartStoryModeWithTutorialInGym();
            
            Debug.Log("Story mode started with tutorial in gym");
        }
        
        
        currentState = IntroState.Complete;
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
    
    [ContextMenu("Reset Intro Completion")]
    public void ResetIntroCompletion()
    {
        PlayerPrefs.SetInt(INTRO_COMPLETE_KEY, 0);
        PlayerPrefs.Save();
        Debug.Log("Intro completion status reset - intro will play next time");
    }
    
    [ContextMenu("Force Complete Intro")]
    public void ForceCompleteIntro()
    {
        PlayerPrefs.SetInt(INTRO_COMPLETE_KEY, 1);
        PlayerPrefs.Save();
        Debug.Log("Intro marked as completed - intro will be skipped next time");
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

    private void OnRecenterInstructionComplete()
    {
        recenterInstructionShown = true;
        recenterCompleted = true;
        currentState = IntroState.BatonInstruction;
        ShowBatonInstruction();
    }

    private void ShowBatonInstruction()
    {
        instructionScreens.ShowBatonInstruction();
    }

    private void OnBatonInstructionComplete()
    {
        batonInstructionShown = true;
        batonCompleted = true;
        currentState = IntroState.BoxInstruction;
        ShowBoxInstruction();
    }

    private void ShowBoxInstruction()
    {
        instructionScreens.ShowBoxInstruction();
    }

    private void OnBoxInstructionComplete()
    {
        boxInstructionShown = true;
        boxCompleted = true;
        currentState = IntroState.FogClear;
        StartCoroutine(ClearFog());
    }

    private IEnumerator ClearFog()
    {
        while (RenderSettings.fogDensity > 0.01f)
        {
            RenderSettings.fogDensity = Mathf.Max(RenderSettings.fogDensity - fogDisappearSpeed * Time.deltaTime, 0);
            yield return null;
        }
        RenderSettings.fog = false;

        yield return new WaitForSeconds(0.5f);

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
            instructionScreens.onBatonInstructionComplete.RemoveListener(OnBatonInstructionComplete);
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
    
    public bool IsIntroCompleted()
    {
        return PlayerPrefs.GetInt(INTRO_COMPLETE_KEY, 0) == 1;
    }
}