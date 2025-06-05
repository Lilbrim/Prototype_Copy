using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public interface ILevelManager
{
    void StartLevel();
}

public class IntroLevel : MonoBehaviour
{
    [Header("Stance UI")]
    public TextMeshProUGUI stanceInstructionText;
    public Image stanceInstructionImage;
    public string stanceInstructionMessage = "Stand in ready position";
    public Sprite stanceInstructionSprite;
    
    [Header("Scene Stuff")]
    public GameObject[] stanceBoxes;
    public StanceManager stanceManager;
    public MonoBehaviour levelManagerComponent; 
    
    [Header("Time")]
    public float stanceHoldTime = 3f;

    [Header("Partner")]
    public bool includeSparringPartner = false;
    public GameObject sparringPartnerPrefab;
    public Transform sparringPartnerSpawnPoint;
    public string sparringPartnerAnimation = "Change This";
    
    [Header("Mirroring")]
    public bool mirrorSparringPartner = false;
    
    private ILevelManager levelManager; 
    private StanceDetector[] stanceDetectors;
    private bool[] isBoxHeld;
    private float[] holdTimers;
    private bool stanceCompleted = false;
    private GameObject instantiatedSparringPartner;

    private void Awake()
    {
        gameObject.SetActive(false);
        
        levelManager = levelManagerComponent as ILevelManager;
        if (levelManager == null)
        {
            Debug.LogError("Level manager component does not implement ILevelManager interface!");
        }
    }

    public void ActivateIntro()
    {
        gameObject.SetActive(true);
        InitializeScene();
    }

    private void InitializeScene()
    {
        stanceCompleted = false;
        
        if (instantiatedSparringPartner != null)
        {
            Destroy(instantiatedSparringPartner);
            instantiatedSparringPartner = null;
        }
        
        if (levelManagerComponent != null) levelManagerComponent.gameObject.SetActive(false);

        InitializeStanceDetection();
        
        if (includeSparringPartner)
        {
            SetupSparringPartner();
        }
        
        StartStancePhase();
        
        this.enabled = true;
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

    private void SetupSparringPartner()
    {
        if (sparringPartnerPrefab != null && sparringPartnerSpawnPoint != null)
        {
            instantiatedSparringPartner = Instantiate(
                sparringPartnerPrefab,
                sparringPartnerSpawnPoint.position,
                sparringPartnerSpawnPoint.rotation
            );

            if (stanceManager != null)
            {
                mirrorSparringPartner = stanceManager.isRightHandDominant;
            }

            if (mirrorSparringPartner)
            {
                Vector3 scale = instantiatedSparringPartner.transform.localScale;
                scale.x = -Mathf.Abs(scale.x); 
                instantiatedSparringPartner.transform.localScale = scale;
            }

            instantiatedSparringPartner.tag = "SparringPartner";

            Animator animator = instantiatedSparringPartner.GetComponent<Animator>();
            if (animator != null && !string.IsNullOrEmpty(sparringPartnerAnimation))
            {
                animator.enabled = true;
                animator.Play(sparringPartnerAnimation);
                Debug.Log($"Playing intro animation: {sparringPartnerAnimation}");
            }

            if (stanceInstructionImage != null)
            {
                stanceInstructionImage.gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.LogWarning("Sparring partner or spawn point is missing. Cannot spawn sparring partner.");
        }
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

    private void OnEnable()
    {
        StopAllCoroutines();
        stanceCompleted = false;
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
        Debug.Log("Skipping");
        
        StopAllCoroutines();

        if (!stanceCompleted)
        {
            stanceCompleted = true;

            GameObject[] activeBoxes = GetActiveStanceBoxes(); // CHANGED
            foreach (var box in activeBoxes)
            {
                box.SetActive(false);
            }
            stanceInstructionText.gameObject.SetActive(false);
            
            if (stanceInstructionImage != null && stanceInstructionImage.gameObject.activeSelf)
            {
                stanceInstructionImage.gameObject.SetActive(false);
            }
        }

        if (instantiatedSparringPartner != null && levelManagerComponent != null)
        {
            SparManager sparManager = levelManagerComponent.GetComponent<SparManager>();
            if (sparManager != null)
            {
                sparManager.SetSparringPartner(instantiatedSparringPartner);
            }
        }

        if (stanceManager != null)
        {
            stanceManager.gameObject.SetActive(true);
        }
        
        if (levelManagerComponent != null)
        {
            levelManagerComponent.gameObject.SetActive(true);
            
            SparManager sparManager = levelManagerComponent.GetComponent<SparManager>();
            if (sparManager != null)
            {
                sparManager.StartLevel();
            }
            else if (levelManager != null)
            {
                levelManager.StartLevel();
            }
        }

        this.enabled = false;
    }

    private void StartStancePhase()
    {
        stanceInstructionText.text = stanceInstructionMessage;
        stanceInstructionText.gameObject.SetActive(true);
        
        if (stanceInstructionImage != null && !includeSparringPartner)
        {
            stanceInstructionImage.sprite = stanceInstructionSprite;
            stanceInstructionImage.gameObject.SetActive(true);
        }

        GameObject[] activeBoxes = GetActiveStanceBoxes(); // CHANGED
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
        
        if (stanceInstructionImage != null && stanceInstructionImage.gameObject.activeSelf)
        {
            stanceInstructionImage.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(1f);

        yield return new WaitForSeconds(1f);

        if (instantiatedSparringPartner != null && levelManagerComponent != null)
        {
            SparManager sparManager = levelManagerComponent.GetComponent<SparManager>();
            if (sparManager != null)
            {
                sparManager.SetSparringPartner(instantiatedSparringPartner);
            }
        }

        if (stanceManager != null)
        {
            stanceManager.gameObject.SetActive(true);
        }
        
        if (levelManagerComponent != null)
        {
            levelManagerComponent.gameObject.SetActive(true);
            levelManager.StartLevel();
        }

        this.enabled = false;
    }
}