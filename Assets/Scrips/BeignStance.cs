using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BeginStance : MonoBehaviour
{
    [Header("Stance Settings")]
    public GameObject[] stanceBoxes;
    public float requiredHoldTime = 3f;

    [Header("UI Elements")]
    public TextMeshProUGUI instructionText;
    public Image instructionImage;
    public string stanceInstructionText = "Stand in ready position";
    public Sprite stanceInstructionSprite;

    [Header("Game Managers")]
    public StanceManager stanceManager;
    public LevelManager levelManager;

    private StanceDetector[] stanceDetectors;
    private bool[] isBoxHeld;
    private float[] holdTimers;
    private bool stanceCompleted = false;

    private void Start()
    {
        levelManager.gameObject.SetActive(false);

        InitializeStanceCheck();
        ShowInstructions();
    }

    private void InitializeStanceCheck()
    {
        stanceDetectors = new StanceDetector[stanceBoxes.Length];
        isBoxHeld = new bool[stanceBoxes.Length];
        holdTimers = new float[stanceBoxes.Length];

        for (int i = 0; i < stanceBoxes.Length; i++)
        {
            stanceDetectors[i] = stanceBoxes[i].GetComponent<StanceDetector>();
            stanceBoxes[i].SetActive(true);
            isBoxHeld[i] = false;
            holdTimers[i] = 0f;
        }
    }

    private void ShowInstructions()
    {
        instructionText.text = stanceInstructionText;
        instructionImage.sprite = stanceInstructionSprite;
        instructionText.gameObject.SetActive(true);
        instructionImage.gameObject.SetActive(true);
    }

    private void Update()
    {
        if (stanceCompleted) return;

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

                if (holdTimers[i] < requiredHoldTime)
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
            StartCoroutine(CompleteBeginStance());
        }
    }

    private IEnumerator CompleteBeginStance()
    {
        stanceCompleted = true;

        foreach (var box in stanceBoxes)
        {
            box.SetActive(false);
        }

        instructionText.gameObject.SetActive(false);
        instructionImage.gameObject.SetActive(false);

        yield return new WaitForSeconds(1f);

        stanceManager.gameObject.SetActive(true);
        levelManager.gameObject.SetActive(true);
        
        levelManager.StartLevel();

        gameObject.SetActive(false);
    }
}