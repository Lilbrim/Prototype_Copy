using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IntroLevel : MonoBehaviour
{
    [Header("Stance UI")]
    public TextMeshProUGUI stanceInstructionText;
    public Image stanceInstructionImage;
    public string stanceInstructionMessage = "Stand in ready position";
    public Sprite stanceInstructionSprite;
    
    [Header("Scene References")]
    public GameObject[] stanceBoxes;
    public StanceManager stanceManager;
    public LevelManager levelManager;
    
    [Header("Effect Settings")]
    public float stanceHoldTime = 3f;

    private StanceDetector[] stanceDetectors;
    private bool[] isBoxHeld;
    private float[] holdTimers;
    private bool stanceCompleted = false;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void ActivateIntro()
    {
        gameObject.SetActive(true);
        InitializeScene();
    }

    private void InitializeScene()
    {
        stanceCompleted = false;
        
        if (stanceManager != null) stanceManager.gameObject.SetActive(false);
        if (levelManager != null) levelManager.gameObject.SetActive(false);

        InitializeStanceDetection();
        StartStancePhase();
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
        if (!stanceCompleted && stanceInstructionText.gameObject.activeSelf)
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
}