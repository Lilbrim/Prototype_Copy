using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;

public class InstructionScreens : MonoBehaviour
{
    [Header("Input Action")]
    [SerializeField] private InputActionAsset inputActions;
    private InputAction acceptAction;

    [Header("Main Instruction Canvas")]
    public Canvas instructionCanvas;

    [Header("Baton Instruction Panel")]
    public GameObject batonInstructionPanel;
    public TextMeshProUGUI batonInstructionText;
    public Image batonInstructionImage;

    [Header("Height Instruction Panel")]
    public GameObject heightInstructionPanel;
    public TextMeshProUGUI heightInstructionText;
    public Image heightInstructionImage;
    public Slider heightSlider;
    public TextMeshProUGUI heightValueText;

    [Header("Box Instruction Panel")]
    public GameObject boxInstructionPanel;
    public TextMeshProUGUI boxInstructionText;
    public Image boxInstructionImage;

    [Header("Shared UI Elements")]
    public TextMeshProUGUI continuePrompt;

    [Header("Instruction Content")]
    [SerializeField] private string batonInstructionMessage = "Pick up your baton depending on your preferred hand to begin. This can be changed in the settings";
    [SerializeField] private string heightInstructionMessage = "Adjust the slider to set your height. This can be changed in the settings.";
    [SerializeField] private string boxInstructionMessage = "Stand in the stance boxes when they appear";
    [SerializeField] private string continueButtonPrompt = "Press A to Continue";

    [Header("Height Settings")]
    public Transform playerRig;
    public float minHeight = 0.5f;
    public float maxHeight = 1.5f;
    [SerializeField] private float defaultHeight = 1.0f;
    private const string HEIGHT_PREF_KEY = "PlayerHeight";

    [Header("Recenter Instruction Panel")]
    public GameObject recenterInstructionPanel;
    public TextMeshProUGUI recenterInstructionText;
    public Image recenterInstructionImage;
    public Button recenterButton;

    [Header("Recenter Settings")]
    public Recenter recenterScript;

    [Header("Instruction Content")]
    [SerializeField] private string recenterInstructionMessage = "Press the button below to recenter your position and orientation.";
    [SerializeField] private string recenterButtonText = "Recenter Position";
    public UnityEvent onRecenterInstructionComplete;

    public UnityEvent onBatonInstructionComplete;
    public UnityEvent onHeightInstructionComplete;
    public UnityEvent onBoxInstructionComplete;

    private InstructionType currentInstructionType = InstructionType.None;

    private enum InstructionType
    {
        None,
        Baton,
        Height,
        Recenter,
        Box
    }

    private void Awake()
    {
        if (inputActions != null)
        {
            acceptAction = inputActions.FindAction("Accept");
        }

        // Hide all panels initially
        HideAllPanels();

        if (instructionCanvas != null)
            instructionCanvas.gameObject.SetActive(false);
    }

    private void Start()
    {
        inputActions.Enable();
        InitializeHeightSlider();
    }

    private void InitializeHeightSlider()
    {
        if (heightSlider != null && playerRig != null)
        {
            // Load saved height or use default
            float savedHeight = PlayerPrefs.GetFloat(HEIGHT_PREF_KEY, defaultHeight);
            savedHeight = Mathf.Clamp(savedHeight, minHeight, maxHeight);

            // Setup slider
            heightSlider.minValue = minHeight;
            heightSlider.maxValue = maxHeight;
            heightSlider.value = savedHeight;
            heightSlider.onValueChanged.AddListener(OnHeightSliderChanged);

            // Apply initial height
            Vector3 newScale = playerRig.localScale;
            newScale.y = savedHeight;
            playerRig.localScale = newScale;

            UpdateHeightValueText();
        }
    }

    private void OnHeightSliderChanged(float value)
    {
        if (playerRig != null)
        {
            Vector3 newScale = playerRig.localScale;
            newScale.y = value;
            playerRig.localScale = newScale;

            // Save the height preference
            PlayerPrefs.SetFloat(HEIGHT_PREF_KEY, value);
            PlayerPrefs.Save();
        }
        UpdateHeightValueText();
    }

    private void UpdateHeightValueText()
    {
        if (heightValueText != null && heightSlider != null)
        {
            heightValueText.text = $"Height: {heightSlider.value:F2}x";
        }
    }

    private void OnEnable()
    {
        if (acceptAction != null)
        {
            acceptAction.Enable();
            acceptAction.performed += OnContinueAction;
        }
    }

    private void OnDisable()
    {
        if (acceptAction != null)
        {
            acceptAction.performed -= OnContinueAction;
            acceptAction.Disable();
        }
    }

    private void OnContinueAction(InputAction.CallbackContext context)
    {
        switch (currentInstructionType)
        {
            case InstructionType.Baton:
                HideInstruction();
                onBatonInstructionComplete?.Invoke();
                break;
            case InstructionType.Height:
                HideInstruction();
                onHeightInstructionComplete?.Invoke();
                break;
            case InstructionType.Recenter:
                HideInstruction();
                onRecenterInstructionComplete?.Invoke();
                break;
            case InstructionType.Box:
                HideInstruction();
                onBoxInstructionComplete?.Invoke();
                break;
        }
    }


    private void HideAllPanels()
    {
        if (batonInstructionPanel != null)
            batonInstructionPanel.SetActive(false);

        if (heightInstructionPanel != null)
            heightInstructionPanel.SetActive(false);

        if (recenterInstructionPanel != null)
            recenterInstructionPanel.SetActive(false);

        if (boxInstructionPanel != null)
            boxInstructionPanel.SetActive(false);
    }
    private void ShowInstruction(InstructionType type)
    {
        HideAllPanels();

        if (instructionCanvas != null)
            instructionCanvas.gameObject.SetActive(true);

        if (continuePrompt != null)
            continuePrompt.text = continueButtonPrompt;

        currentInstructionType = type;

        switch (type)
        {
            case InstructionType.Baton:
                if (batonInstructionPanel != null)
                {
                    batonInstructionPanel.SetActive(true);
                    if (batonInstructionText != null)
                        batonInstructionText.text = batonInstructionMessage;
                }
                break;

            case InstructionType.Height:
                if (heightInstructionPanel != null)
                {
                    heightInstructionPanel.SetActive(true);
                    if (heightInstructionText != null)
                        heightInstructionText.text = heightInstructionMessage;
                    UpdateHeightValueText();
                }
                break;

            case InstructionType.Recenter:
                if (recenterInstructionPanel != null)
                {
                    recenterInstructionPanel.SetActive(true);
                    if (recenterInstructionText != null)
                        recenterInstructionText.text = recenterInstructionMessage;
                    if (recenterButton != null)
                    {
                        recenterButton.onClick.RemoveAllListeners();
                        recenterButton.onClick.AddListener(OnRecenterButtonClicked);
                        TextMeshProUGUI buttonText = recenterButton.GetComponentInChildren<TextMeshProUGUI>();
                        if (buttonText != null)
                            buttonText.text = recenterButtonText;
                    }
                }
                break;

            case InstructionType.Box:
                if (boxInstructionPanel != null)
                {
                    boxInstructionPanel.SetActive(true);
                    if (boxInstructionText != null)
                        boxInstructionText.text = boxInstructionMessage;
                }
                break;
        }
    }
    private void HideInstruction()
    {
        HideAllPanels();

        if (instructionCanvas != null)
            instructionCanvas.gameObject.SetActive(false);

        currentInstructionType = InstructionType.None;
    }

    public void ShowBatonInstruction()
    {
        ShowInstruction(InstructionType.Baton);
    }

    public void ShowHeightInstruction()
    {
        ShowInstruction(InstructionType.Height);
    }

    public void ShowBoxInstruction()
    {
        ShowInstruction(InstructionType.Box);
    }

    public void ResetHeight()
    {
        if (heightSlider != null)
        {
            heightSlider.value = defaultHeight;
            PlayerPrefs.SetFloat(HEIGHT_PREF_KEY, defaultHeight);
            PlayerPrefs.Save();
        }
    }

    private void OnDestroy()
    {
        if (heightSlider != null)
        {
            heightSlider.onValueChanged.RemoveListener(OnHeightSliderChanged);
        }
    }
    public void ShowRecenterInstruction()
{
    ShowInstruction(InstructionType.Recenter);
}

    private void OnRecenterButtonClicked()
    {
        if (recenterScript != null)
        {
            recenterScript.recenter();
        }
        else
        {
            Debug.LogWarning("Recenter script reference is missing!");
        }
    }
}