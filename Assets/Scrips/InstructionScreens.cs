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

    [Header("Baton Instruction UI")]
    public Canvas batonInstructionCanvas;
    public TextMeshProUGUI batonInstructionText;
    public Image batonInstructionImage;
    public TextMeshProUGUI batonContinuePrompt;

    [Header("Box Instruction UI")]
    public Canvas boxInstructionCanvas;
    public TextMeshProUGUI boxInstructionText;
    public Image boxInstructionImage;
    public TextMeshProUGUI boxContinuePrompt;

    [Header("Instruction Content")]
    [SerializeField] private string batonInstructionMessage = "Baton Text";
    [SerializeField] private string boxInstructionMessage = "Box Text";
    [SerializeField] private string continueButtonPrompt = "Press A to Continue";

    public UnityEvent onBatonInstructionComplete;
    public UnityEvent onBoxInstructionComplete;

    private InstructionType currentInstructionType = InstructionType.None;

    private enum InstructionType
    {
        None,
        Baton,
        Box
    }

    private void Awake()
    {
        if (inputActions != null)
        {
            acceptAction = inputActions.FindAction("Accept");
        }

        if (batonInstructionCanvas != null)
            batonInstructionCanvas.gameObject.SetActive(false);

        if (boxInstructionCanvas != null)
            boxInstructionCanvas.gameObject.SetActive(false);
    }

    private void Start()
    {
        inputActions.Enable();
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
                HideBatonInstruction();
                onBatonInstructionComplete?.Invoke();
                break;
            case InstructionType.Box:
                HideBoxInstruction();
                onBoxInstructionComplete?.Invoke();
                break;
        }
    }

    public void ShowBatonInstruction()
    {
        batonInstructionText.text = batonInstructionMessage;
        batonContinuePrompt.text = continueButtonPrompt;

        batonInstructionCanvas.gameObject.SetActive(true);
        currentInstructionType = InstructionType.Baton;
    }

    public void ShowBoxInstruction()
    {
        boxInstructionText.text = boxInstructionMessage;
        boxContinuePrompt.text = continueButtonPrompt;

        boxInstructionCanvas.gameObject.SetActive(true);
        currentInstructionType = InstructionType.Box;
    }

    private void HideBatonInstruction()
    {
        batonInstructionCanvas.gameObject.SetActive(false);
        currentInstructionType = InstructionType.None;
    }

    private void HideBoxInstruction()
    {
        boxInstructionCanvas.gameObject.SetActive(false);
        currentInstructionType = InstructionType.None;
    }
}
