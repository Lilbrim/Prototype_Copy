using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelSelector : MonoBehaviour
{
    [System.Serializable]
    public class LevelData
    {
        public string levelName;
        public IntroLevel introLevel;
        public Sprite levelThumbnail;
        [TextArea]
        public string levelDescription;
        public string levelId;
        public bool isSparLevel = false;
        public bool isTournamentLevel = false;
        
        [Tooltip("Minimum score required to unlock this spar/tournament level")]
        public int requiredScore = 0;
        
        [Tooltip("Minimum accuracy required to unlock next level (only used for prac levels)")]
        [Range(0f, 1f)]
        public float requiredAccuracy = 0.7f;
        
        public List<string> requiredLevelIds = new List<string>();
    }

    public enum LevelBackground
    {
        House,
        Gym,
        Tournament
    }

    [Header("Levels")]
    public List<LevelData> availableLevels = new List<LevelData>();
    public List<LevelData> availableSparLevels = new List<LevelData>();
    public List<LevelData> availableTournamentLevels = new List<LevelData>();

    [Header("Level Backgrounds")]
    public LevelBackground currentBackground = LevelBackground.Gym;
    
    public GameObject houseBackground;
    public GameObject gymBackground;
    public GameObject tournamentBackground;
    
    [Range(0.1f, 3f)]
    public float backgroundTransitionDuration = 1.0f;

    public int tutorialLevelIndex = 0;

    [Header("UI Stuff")]
    public GameObject levelSelectionPanel;
    public GameObject levelButtonPrefab;
    public Transform levelButtonContainer;
    public Transform sparLevelButtonContainer; 
    public Transform tournamentLevelButtonContainer;
    public TextMeshProUGUI levelDescriptionText;
    public Image levelPreviewImage;
    public TextMeshProUGUI scoreText;
    public Button startLevelButton;
    
    [Header("Tabs")]
    public GameObject pracLevelsTab;
    public GameObject sparLevelsTab;
    public GameObject tournamentLevelsTab;
    public Button pracLevelsTabButton;
    public Button sparLevelsTabButton;
    public Button tournamentLevelsTabButton;

    private IntroLevel selectedIntroLevel;
    public int selectedLevelIndex = -1;
    private bool isViewingSparLevels = false;
    private bool isViewingTournamentLevels = false;
    private const string ACCURACY_SAVE_PREFIX = "LevelAccuracy_";
    private const string SCORE_SAVE_PREFIX = "LevelScore_";
    private const string NO_RECORD_TEXT = "No Record";
    private const string BACKGROUND_SAVE_KEY = "CurrentBackground";
    private List<Button> levelButtons = new List<Button>();
    private List<Button> sparLevelButtons = new List<Button>();
    private List<Button> tournamentLevelButtons = new List<Button>();
    private bool hasInitializedFade = false;


    public Button houseBackgroundButton;
    public Button gymBackgroundButton;

    [Header("StoryMode")]
    public GameObject storyUIPanel;
    public Button nextLevelButton;
    public TextMeshProUGUI storyLevelNameText;
    public TextMeshProUGUI storyProgressText;
    public Image storyLevelPreviewImage;
    public TextMeshProUGUI storyLevelDescriptionText;

    private int currentStoryLevelIndex = 0;
    private List<LevelData> allLevelsInOrder = new List<LevelData>();
    public bool isInStoryMode = false;

    
    private bool isTransitioningBackground = false;

    private void Start()
    {
        
        InitializeScreenFaded();
        
        LoadSavedBackground(); 
            
        if (currentBackground != LevelBackground.House)
        {
            ShowpracLevelsTab();
        }
        
        GenerateLevelButtons();
        UpdateLevelInfoPanel(-1);
        startLevelButton.interactable = false;
        InitializeStoryModeOrder();

        if (pracLevelsTabButton != null)
            pracLevelsTabButton.onClick.AddListener(ShowpracLevelsTab);

        if (sparLevelsTabButton != null)
            sparLevelsTabButton.onClick.AddListener(ShowSparLevelsTab);

        if (tournamentLevelsTabButton != null)
            tournamentLevelsTabButton.onClick.AddListener(ShowTournamentLevelsTab);
            
        if (houseBackgroundButton != null)
            houseBackgroundButton.onClick.AddListener(SetHouseBackground);

        if (gymBackgroundButton != null)
            gymBackgroundButton.onClick.AddListener(SetGymBackground);
            
        
        StartCoroutine(InitialFadeIn());
    }

    private void InitializeScreenFaded()
    {
        SceneTransitionManager transitionManager = SceneTransitionManager.Instance;
        if (transitionManager != null && transitionManager.fadeCanvasGroup != null)
        {
            transitionManager.fadeCanvasGroup.alpha = 1f;
            transitionManager.fadeCanvasGroup.blocksRaycasts = true;
            hasInitializedFade = true;
        }
    }

    private IEnumerator InitialFadeIn()
    {
        if (!hasInitializedFade)
            yield break;
            
        // Wait a brief moment for everything to be ready
        yield return new WaitForSeconds(0.1f);
        
        SceneTransitionManager transitionManager = SceneTransitionManager.Instance;
        if (transitionManager != null)
        {
            yield return StartCoroutine(FadeFromBlack(transitionManager));
        }
    }

    public void ChangeBackground(LevelBackground newBackground)
    {
        if (currentBackground != newBackground && !isTransitioningBackground)
        {
            StartCoroutine(TransitionToNewBackground(newBackground));
        }
    }


    private void LoadSavedBackground()
    {
        int savedBackground = PlayerPrefs.GetInt(BACKGROUND_SAVE_KEY, (int)LevelBackground.House);
        Debug.Log($"Loading background: {(LevelBackground)savedBackground}");
        currentBackground = (LevelBackground)savedBackground;
        SetBackground(currentBackground);
    }

    private void SaveCurrentBackground()
    {
        Debug.Log($"Saving background: {currentBackground}");
        PlayerPrefs.SetInt(BACKGROUND_SAVE_KEY, (int)currentBackground);
        PlayerPrefs.Save();
    }

    
    [ContextMenu("Set House Background")]
    public void SetHouseBackground()
    {
        isInStoryMode = true;
        ChangeBackground(LevelBackground.House);
    }

    [ContextMenu("Set Gym Background")]
    public void SetGymBackground()
    {
        isInStoryMode = false;
        ChangeBackground(LevelBackground.Gym);
    }

    [ContextMenu("Set Tournament Background")]
    public void SetTournamentBackground()
    {
        isInStoryMode = false;
        ChangeBackground(LevelBackground.Tournament);
    }

    
  private IEnumerator TransitionToNewBackground(LevelBackground newBackground)
    {
        isTransitioningBackground = true;
        
        SceneTransitionManager transitionManager = SceneTransitionManager.Instance;
        if (transitionManager != null)
        {
            yield return StartCoroutine(FadeToBlack(transitionManager));
            
            SetBackground(newBackground);
            currentBackground = newBackground;
            
            SaveCurrentBackground();
            
            yield return new WaitForSeconds(0.1f);
            
            yield return StartCoroutine(FadeFromBlack(transitionManager));
        }
        else
        {
            Debug.LogWarning("SceneTransitionManager not found. Changing background without fade.");
            SetBackground(newBackground);
            currentBackground = newBackground;
            SaveCurrentBackground(); 
        }
        
        isTransitioningBackground = false;
    }
        
    private IEnumerator FadeToBlack(SceneTransitionManager transitionManager)
    {
        if (transitionManager.fadeCanvasGroup != null)
        {
            transitionManager.fadeCanvasGroup.blocksRaycasts = true;
            
            float elapsedTime = 0f;
            while (elapsedTime < backgroundTransitionDuration)
            {
                elapsedTime += Time.deltaTime;
                transitionManager.fadeCanvasGroup.alpha = Mathf.Clamp01(elapsedTime / backgroundTransitionDuration);
                yield return null;
            }
            
            transitionManager.fadeCanvasGroup.alpha = 1f;
        }
    }
    
    private IEnumerator FadeFromBlack(SceneTransitionManager transitionManager)
    {
        if (transitionManager.fadeCanvasGroup != null)
        {
            float elapsedTime = 0f;
            while (elapsedTime < backgroundTransitionDuration)
            {
                elapsedTime += Time.deltaTime;
                transitionManager.fadeCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsedTime / backgroundTransitionDuration);
                yield return null;
            }
            
            transitionManager.fadeCanvasGroup.alpha = 0f;
            transitionManager.fadeCanvasGroup.blocksRaycasts = false;
        }
    }
    
private void SetBackground(LevelBackground background)
{
    
    if (houseBackground != null) houseBackground.SetActive(false);
    if (gymBackground != null) gymBackground.SetActive(false);
    if (tournamentBackground != null) tournamentBackground.SetActive(false);
    if (levelSelectionPanel != null) levelSelectionPanel.SetActive(false);
    if (storyUIPanel != null) storyUIPanel.SetActive(false);
    
    switch (background)
    {
        case LevelBackground.House:
            if (houseBackground != null) 
            {
                houseBackground.SetActive(true);
                Debug.Log("Switched to House background");
            }
            
            if (storyUIPanel != null)
            {
                storyUIPanel.SetActive(true);
                UpdateStoryUI();
            }
            break;
            
        case LevelBackground.Gym:
            if (gymBackground != null) 
            {
                gymBackground.SetActive(true);
                Debug.Log("Switched to Gym background");
            }
            
            if (levelSelectionPanel != null)
            {
                levelSelectionPanel.SetActive(true);
                ShowpracLevelsTab();
            }
            break;
            
        case LevelBackground.Tournament:
            if (tournamentBackground != null) 
            {
                tournamentBackground.SetActive(true);
                Debug.Log("Switched to Tournament background");
            }
            
            if (levelSelectionPanel != null)
            {
                levelSelectionPanel.SetActive(true);
                ShowTournamentLevelsTab();
            }
            break;
    }
}

    
    public void StartStoryModeWithTutorialInGym()
    {
        isInStoryMode = true;
        InitializeStoryModeOrder();
        
        ChangeBackground(LevelBackground.House);
    }

    
    private void InitializeStoryModeOrder()
    {
        allLevelsInOrder.Clear();
        allLevelsInOrder.AddRange(availableLevels);
        allLevelsInOrder.AddRange(availableSparLevels);
        allLevelsInOrder.AddRange(availableTournamentLevels);


        currentStoryLevelIndex = 0;
        for (int i = 0; i < allLevelsInOrder.Count; i++)
        {
            if (IsLevelCompleted(allLevelsInOrder[i]))
            {
                currentStoryLevelIndex = i + 1;
            }
            else
            {
                break;
            }
        }


        if (currentStoryLevelIndex >= allLevelsInOrder.Count)
        {
            currentStoryLevelIndex = allLevelsInOrder.Count - 1;
        }
    }

    private bool IsLevelCompleted(LevelData level)
    {
        if (level.isSparLevel || level.isTournamentLevel)
        {
            return GetSavedScore(level.levelId) >= level.requiredScore;
        }
        else
        {
            return GetSavedAccuracy(level.levelId) >= level.requiredAccuracy;
        }
    }

    private void UpdateStoryUI()
    {
        if (currentStoryLevelIndex < allLevelsInOrder.Count)
        {
            LevelData currentLevel = allLevelsInOrder[currentStoryLevelIndex];
            
            
            if (storyLevelNameText != null)
                storyLevelNameText.text = currentLevel.levelName;
                
            if (storyLevelDescriptionText != null)
                storyLevelDescriptionText.text = currentLevel.levelDescription;
                
            if (storyLevelPreviewImage != null)
                storyLevelPreviewImage.sprite = currentLevel.levelThumbnail;
                
            if (storyProgressText != null)
            {
                int completedLevels = currentStoryLevelIndex;
                int totalLevels = allLevelsInOrder.Count;
                storyProgressText.text = $"Progress: {completedLevels}/{totalLevels}";
            }
            
            
            if (nextLevelButton != null)
            {
                nextLevelButton.interactable = true;
                
                
                if (currentStoryLevelIndex >= availableLevels.Count + availableSparLevels.Count)
                {
                    
                    if (currentBackground != LevelBackground.Tournament)
                    {
                        ChangeBackground(LevelBackground.Tournament);
                    }
                }
            }
        }
        else
        {
            
            if (storyLevelNameText != null)
                storyLevelNameText.text = "All Levels Complete";
                
            if (storyLevelDescriptionText != null)
                storyLevelDescriptionText.text = "All Levels done";
                
            if (nextLevelButton != null)
                nextLevelButton.interactable = false;
        }
    }

    public void StartNextLevel()
    {
        if (currentStoryLevelIndex < allLevelsInOrder.Count)
        {
            LevelData levelToStart = allLevelsInOrder[currentStoryLevelIndex];
            selectedIntroLevel = levelToStart.introLevel;
            
            if (selectedIntroLevel != null)
            {
                
                if (storyUIPanel != null)
                    storyUIPanel.SetActive(false);
                    
                string levelId = levelToStart.levelId;
                
                
                if (levelToStart.isSparLevel || levelToStart.isTournamentLevel)
                {
                    if (selectedIntroLevel.gameObject.GetComponent<SaveSparScore>() == null)
                    {
                        SaveSparScore observer = selectedIntroLevel.gameObject.AddComponent<SaveSparScore>();
                        observer.Initialize(this, levelId);
                    }
                    
                    SparResultsManager resultsManager = FindObjectOfType<SparResultsManager>();
                    if (resultsManager != null)
                    {
                        resultsManager.InitializeForLevel(levelId);
                    }
                }
                else
                {
                    if (selectedIntroLevel.gameObject.GetComponent<SaveAccuracy>() == null)
                    {
                        SaveAccuracy observer = selectedIntroLevel.gameObject.AddComponent<SaveAccuracy>();
                        observer.Initialize(this, levelId);
                    }
                }
                
                selectedIntroLevel.ActivateIntro();
            }
        }
    }
    
    public void ShowpracLevelsTab()
    {
        isViewingSparLevels = false;
        isViewingTournamentLevels = false;
        ChangeBackground(LevelBackground.Gym);


        if (pracLevelsTab != null)
            pracLevelsTab.SetActive(true);

        if (sparLevelsTab != null)
            sparLevelsTab.SetActive(false);

        if (tournamentLevelsTab != null)
            tournamentLevelsTab.SetActive(false);

        selectedLevelIndex = -1;
        selectedIntroLevel = null;
        UpdateLevelInfoPanel(-1);
        startLevelButton.interactable = false;

        if (pracLevelsTabButton != null)
            pracLevelsTabButton.interactable = false;

        if (sparLevelsTabButton != null)
            sparLevelsTabButton.interactable = true;

        if (tournamentLevelsTabButton != null)
            tournamentLevelsTabButton.interactable = true;
    }
    
    public void ShowSparLevelsTab()
    {
        isViewingSparLevels = true;
        isViewingTournamentLevels = false;
        ChangeBackground(LevelBackground.Gym);
        
        if (pracLevelsTab != null)
            pracLevelsTab.SetActive(false);
        
        if (sparLevelsTab != null)
            sparLevelsTab.SetActive(true);
            
        if (tournamentLevelsTab != null)
            tournamentLevelsTab.SetActive(false);
        
        selectedLevelIndex = -1;
        selectedIntroLevel = null;
        UpdateLevelInfoPanel(-1);
        startLevelButton.interactable = false;
        
        if (pracLevelsTabButton != null)
            pracLevelsTabButton.interactable = true;
        
        if (sparLevelsTabButton != null)
            sparLevelsTabButton.interactable = false;
            
        if (tournamentLevelsTabButton != null)
            tournamentLevelsTabButton.interactable = true;
    }
    
    public void ShowTournamentLevelsTab()
    {
        isViewingSparLevels = false;
        isViewingTournamentLevels = true;
        ChangeBackground(LevelBackground.Tournament);
        if (pracLevelsTab != null)
            pracLevelsTab.SetActive(false);
        
        if (sparLevelsTab != null)
            sparLevelsTab.SetActive(false);
            
        if (tournamentLevelsTab != null)
            tournamentLevelsTab.SetActive(true);
        
        selectedLevelIndex = -1;
        selectedIntroLevel = null;
        UpdateLevelInfoPanel(-1);
        startLevelButton.interactable = false;
        
        if (pracLevelsTabButton != null)
            pracLevelsTabButton.interactable = true;
        
        if (sparLevelsTabButton != null)
            sparLevelsTabButton.interactable = true;
            
        if (tournamentLevelsTabButton != null)
            tournamentLevelsTabButton.interactable = false;
    }

    private void GenerateLevelButtons()
    {
        GenerateButtonsForList(availableLevels, levelButtonContainer, levelButtons, false, false);
        
        GenerateButtonsForList(availableSparLevels, sparLevelButtonContainer, sparLevelButtons, true, false);
        
        GenerateButtonsForList(availableTournamentLevels, tournamentLevelButtonContainer, tournamentLevelButtons, false, true);
        
        UpdateLevelButtonsState();
    }
    
    private void GenerateButtonsForList(List<LevelData> levels, Transform container, List<Button> buttonsList, bool isSparLevel, bool isTournamentLevel)
    {
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
        buttonsList.Clear();

        for (int i = 0; i < levels.Count; i++)
        {
            LevelData levelData = levels[i];
            GameObject buttonObj = Instantiate(levelButtonPrefab, container);
            
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = levelData.levelName;
            }
            
            Transform indicatorTransform = buttonObj.transform.Find("AccuracyIndicator");
            if (indicatorTransform != null && indicatorTransform.GetComponent<TextMeshProUGUI>() != null)
            {
                TextMeshProUGUI indicator = indicatorTransform.GetComponent<TextMeshProUGUI>();
                
                if (isSparLevel)
                {
                    int savedScore = GetSavedScore(levelData.levelId);
                    if (savedScore > 0)
                    {
                        indicator.text = $"Score: {savedScore}";
                    }
                    else
                    {
                        indicator.text = NO_RECORD_TEXT;
                    }
                }
                else if (isTournamentLevel)
                {
                    int savedScore = GetSavedScore(levelData.levelId);
                    if (savedScore > 0)
                    {
                        indicator.text = $"Score: {savedScore}";
                    }
                    else
                    {
                        indicator.text = NO_RECORD_TEXT;
                    }
                }
                else
                {
                    float savedAccuracy = GetSavedAccuracy(levelData.levelId);
                    if (savedAccuracy > 0)
                    {
                        indicator.text = $"{savedAccuracy:P0}";
                    }
                    else
                    {
                        indicator.text = NO_RECORD_TEXT;
                    }
                }
            }

            int levelIndex = i; 
            Button button = buttonObj.GetComponent<Button>();
            buttonsList.Add(button);
            
            if (button != null)
            {
                if (isSparLevel)
                {
                    button.onClick.AddListener(() => SelectSparLevel(levelIndex));
                }
                else if (isTournamentLevel)
                {
                    button.onClick.AddListener(() => SelectTournamentLevel(levelIndex));
                }
                else
                {
                    button.onClick.AddListener(() => SelectLevel(levelIndex));
                }
            }
        }
    }

    public void UpdateLevelButtonsState()
    {
        UpdateButtonsStateForList(levelButtons, availableLevels, false, false);
        
        UpdateButtonsStateForList(sparLevelButtons, availableSparLevels, true, false);
        
        UpdateButtonsStateForList(tournamentLevelButtons, availableTournamentLevels, false, true);
    }
    
    private void UpdateButtonsStateForList(List<Button> buttons, List<LevelData> levels, bool isSparLevel, bool isTournamentLevel)
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            LevelData levelData = levels[i];
            bool isUnlocked;
            
            if (i == 0) 
            {
                isUnlocked = true;
            }
            else if (isSparLevel)
            {
                isUnlocked = CheckSparLevelUnlockRequirements(levelData);
            }
            else if (isTournamentLevel)
            {
                isUnlocked = CheckTournamentLevelUnlockRequirements(levelData);
            }
            else
            {
                string previousLevelId = levels[i - 1].levelId;
                float previousLevelAccuracy = GetSavedAccuracy(previousLevelId);
                float requiredAccuracy = levels[i - 1].requiredAccuracy; 
                isUnlocked = previousLevelAccuracy >= requiredAccuracy;
            }
            
            buttons[i].interactable = isUnlocked;
            
            Transform lockIndicator = buttons[i].transform.Find("LockIndicator");
            if (lockIndicator != null)
            {
                lockIndicator.gameObject.SetActive(!isUnlocked);
            }
            else
            {
                ColorBlock colors = buttons[i].colors;
                if (!isUnlocked)
                {
                    colors.normalColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                    colors.highlightedColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
                    colors.pressedColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
                    colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
                }
                buttons[i].colors = colors;
            }
        }
    }
    
    private bool CheckSparLevelUnlockRequirements(LevelData sparLevel)
    {
        if (sparLevel.requiredLevelIds.Count == 0)
        {
            int index = availableSparLevels.IndexOf(sparLevel);
            if (index > 0)
            {
                string previousLevelId = availableSparLevels[index - 1].levelId;
                int previousLevelScore = GetSavedScore(previousLevelId);
                return previousLevelScore >= sparLevel.requiredScore;
            }
            return true;
        }
        
        return CheckLevelUnlockRequirements(sparLevel, GetSavedScore);
    }
    
    private bool CheckTournamentLevelUnlockRequirements(LevelData tournamentLevel)
    {
        if (tournamentLevel.requiredLevelIds.Count == 0)
        {
            int index = availableTournamentLevels.IndexOf(tournamentLevel);
            if (index > 0)
            {
                string previousLevelId = availableTournamentLevels[index - 1].levelId;
                int previousLevelScore = GetSavedScore(previousLevelId);
                return previousLevelScore >= tournamentLevel.requiredScore;
            }
            return true;
        }
        
        return CheckLevelUnlockRequirements(tournamentLevel, GetSavedScore);
    }
    
    private bool CheckLevelUnlockRequirements(LevelData level, System.Func<string, int> getScoreFunc)
    {
        foreach (string requiredLevelId in level.requiredLevelIds)
        {
            bool found = false;
            
            foreach (LevelData pracLevel in availableLevels)
            {
                if (pracLevel.levelId == requiredLevelId)
                {
                    found = true;
                    float accuracy = GetSavedAccuracy(requiredLevelId);
                    if (accuracy < pracLevel.requiredAccuracy)
                    {
                        return false;
                    }
                    break;
                }
            }
            
            if (!found)
            {
                foreach (LevelData otherSparLevel in availableSparLevels)
                {
                    if (otherSparLevel.levelId == requiredLevelId)
                    {
                        found = true;
                        int score = GetSavedScore(requiredLevelId);
                        if (score < level.requiredScore)
                        {
                            return false;
                        }
                        break;
                    }
                }
            }
            
            if (!found)
            {
                foreach (LevelData otherTournamentLevel in availableTournamentLevels)
                {
                    if (otherTournamentLevel.levelId == requiredLevelId)
                    {
                        found = true;
                        int score = getScoreFunc(requiredLevelId);
                        if (score < level.requiredScore)
                        {
                            return false;
                        }
                        break;
                    }
                }
            }
            
            if (!found)
            {
                return false;
            }
        }
        
        return true;
    }
    

    public void SelectLevel(int levelIndex)
    {
        SelectLevelFromList(levelIndex, availableLevels, levelButtons, false, false);
    }
    
    public void SelectSparLevel(int levelIndex)
    {
        SelectLevelFromList(levelIndex, availableSparLevels, sparLevelButtons, true, false);
    }
    
    public void SelectTournamentLevel(int levelIndex)
    {
        SelectLevelFromList(levelIndex, availableTournamentLevels, tournamentLevelButtons, false, true);
    }
    
    public void SelectLevelFromList(int levelIndex, List<LevelData> levels, List<Button> buttons, bool isSparLevel, bool isTournamentLevel)
    {
        if (levelIndex >= 0 && levelIndex < levels.Count)
        {
            selectedLevelIndex = levelIndex;
            selectedIntroLevel = levels[levelIndex].introLevel;
            
            if (buttons[levelIndex].interactable)
            {
                UpdateLevelInfoPanel(levelIndex, levels);
                startLevelButton.interactable = true;
            }
            else
            {
                LevelData levelData = levels[levelIndex];
                string unlockMessage = "Level locked! ";
                
                if ((isSparLevel || isTournamentLevel) && levelData.requiredLevelIds.Count > 0)
                {
                    unlockMessage += "Complete required levels:";
                    foreach (string requiredId in levelData.requiredLevelIds)
                    {
                        string levelName = GetLevelNameById(requiredId);
                        unlockMessage += $"\n- {levelName}";
                    }
                    
                    if (levelData.requiredScore > 0)
                    {
                        unlockMessage += $"\nWith minimum score: {levelData.requiredScore}";
                    }
                }
                else if (levelIndex > 0)
                {
                    string previousLevelName = levels[levelIndex - 1].levelName;
                    if (isSparLevel)
                    {
                        int score = GetSavedScore(levels[levelIndex - 1].levelId);
                        unlockMessage += $"Complete {previousLevelName} with minimum score of {levelData.requiredScore} (current: {score})";
                    }
                    else if (isTournamentLevel)
                    {
                        int score = GetSavedScore(levels[levelIndex - 1].levelId);
                        unlockMessage += $"Complete {previousLevelName} with minimum score of {levelData.requiredScore} (current: {score})";
                    }
                    else
                    {
                        float previousLevelAccuracy = GetSavedAccuracy(levels[levelIndex - 1].levelId);
                        float requiredAccuracy = levels[levelIndex - 1].requiredAccuracy;
                        unlockMessage += $"Complete {previousLevelName} with at least {requiredAccuracy:P0} accuracy (current: {previousLevelAccuracy:P0})";
                    }
                }
                
                levelDescriptionText.text = unlockMessage;
                startLevelButton.interactable = false;
            }
        }
    }

    private string GetLevelNameById(string levelId)
    {
        foreach (LevelData level in availableLevels)
        {
            if (level.levelId == levelId)
                return level.levelName;
        }
        
        foreach (LevelData level in availableSparLevels)
        {
            if (level.levelId == levelId)
                return level.levelName;
        }
        
        foreach (LevelData level in availableTournamentLevels)
        {
            if (level.levelId == levelId)
                return level.levelName;
        }
        
        return "Unknown Level";
    }

    public void UpdateLevelInfoPanel(int levelIndex)
    {
        if (isViewingSparLevels)
        {
            UpdateLevelInfoPanel(levelIndex, availableSparLevels);
        }
        else if (isViewingTournamentLevels)
        {
            UpdateLevelInfoPanel(levelIndex, availableTournamentLevels);
        }
        else
        {
            UpdateLevelInfoPanel(levelIndex, availableLevels);
        }
    }

    private void UpdateLevelInfoPanel(int levelIndex, List<LevelData> levels)
    {
        if (levelIndex >= 0 && levelIndex < levels.Count)
        {
            LevelData levelData = levels[levelIndex];
            levelDescriptionText.text = levelData.levelDescription;
            levelPreviewImage.sprite = levelData.levelThumbnail;
            levelPreviewImage.gameObject.SetActive(true);
            
            if (scoreText != null)
            {
                scoreText.gameObject.SetActive(true);
                
                if (levelData.isSparLevel)
                {
                    int savedScore = GetSavedScore(levelData.levelId);
                    if (savedScore > 0)
                    {
                        scoreText.text = $"Best Score: {savedScore}";
                    }
                    else
                    {
                        scoreText.text = NO_RECORD_TEXT;
                    }
                }
                else if (levelData.isTournamentLevel)
                {
                    int savedScore = GetSavedScore(levelData.levelId);
                    if (savedScore > 0)
                    {
                        scoreText.text = $"Best Score: {savedScore}";
                    }
                    else
                    {
                        scoreText.text = NO_RECORD_TEXT;
                    }
                }
                else
                {
                    float savedAccuracy = GetSavedAccuracy(levelData.levelId);
                    if (savedAccuracy > 0)
                    {
                        scoreText.text = $"Best Accuracy: {savedAccuracy:P0}";
                    }
                    else
                    {
                        scoreText.text = NO_RECORD_TEXT;
                    }
                }
            }
        }
        else
        {
            levelDescriptionText.text = "Select level";
            levelPreviewImage.gameObject.SetActive(false);
            
            if (scoreText != null)
            {
                scoreText.gameObject.SetActive(false);
            }
        }
    }

    public void StartSelectedLevel()
    {
            Debug.Log("StartSelectedLevel called!");
             Debug.Log($"selectedIntroLevel is null: {selectedIntroLevel == null}");
        if (selectedIntroLevel != null)
        {
            levelSelectionPanel.SetActive(false);

            string levelId;
            bool isSparLevel = false;
            bool isTournamentLevel = false;

            if (isViewingSparLevels)
            {
                levelId = availableSparLevels[selectedLevelIndex].levelId;
                isSparLevel = true;
            }
            else if (isViewingTournamentLevels)
            {
                levelId = availableTournamentLevels[selectedLevelIndex].levelId;
                isTournamentLevel = true;
            }
            else
            {
                levelId = availableLevels[selectedLevelIndex].levelId;
            }

            if (isSparLevel)
            {
                if (selectedIntroLevel.gameObject.GetComponent<SaveSparScore>() == null)
                {
                    SaveSparScore observer = selectedIntroLevel.gameObject.AddComponent<SaveSparScore>();
                    observer.Initialize(this, levelId);
                }

                SparResultsManager resultsManager = FindObjectOfType<SparResultsManager>();
                if (resultsManager != null)
                {
                    resultsManager.InitializeForLevel(levelId);
                }
            }
            else if (isTournamentLevel)
            {
                if (selectedIntroLevel.gameObject.GetComponent<SaveSparScore>() == null)
                {
                    SaveSparScore observer = selectedIntroLevel.gameObject.AddComponent<SaveSparScore>();
                    observer.Initialize(this, levelId);
                }

                SparResultsManager resultsManager = FindObjectOfType<SparResultsManager>();
                if (resultsManager != null)
                {
                    resultsManager.InitializeForLevel(levelId);
                }
            }
            else
            {
                if (selectedIntroLevel.gameObject.GetComponent<SaveAccuracy>() == null)
                {
                    SaveAccuracy observer = selectedIntroLevel.gameObject.AddComponent<SaveAccuracy>();
                    observer.Initialize(this, levelId);
                }
            }

            selectedIntroLevel.ActivateIntro();
        }
    }

    public void SaveLevelAccuracy(string levelId, float accuracy)
    {
        float currentAccuracy = GetSavedAccuracy(levelId);
        if (accuracy > currentAccuracy)
        {
            PlayerPrefs.SetFloat(ACCURACY_SAVE_PREFIX + levelId, accuracy);
            PlayerPrefs.Save();
            UpdateLevelButtonsState();
            
            
            if (isInStoryMode)
            {
                OnLevelCompleted();
            }
        }
    }

    public float GetSavedAccuracy(string levelId)
    {
        return PlayerPrefs.GetFloat(ACCURACY_SAVE_PREFIX + levelId, 0f);
    }
    
    public void SaveLevelScore(string levelId, int score)
    {
        int currentScore = GetSavedScore(levelId);
        if (score > currentScore)
        {
            PlayerPrefs.SetInt(SCORE_SAVE_PREFIX + levelId, score);
            PlayerPrefs.Save();
            UpdateLevelButtonsState();
            
            if (isInStoryMode)
            {
                OnLevelCompleted();
            }
        }
    }

    public void OnLevelCompleted()
    {
        if (isInStoryMode)
        {
            InitializeStoryModeOrder();
            UpdateStoryUI();
        }
    }

        
        public int GetSavedScore(string levelId)
        {
            return PlayerPrefs.GetInt(SCORE_SAVE_PREFIX + levelId, 0);
        }
}