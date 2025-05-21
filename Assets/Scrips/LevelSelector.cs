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
        
        [Tooltip("Minimum score required to unlock this spar level (only used for spar levels)")]
        public int requiredScore = 0;
        
        [Tooltip("Minimum accuracy required to unlock next level (only used for prac levels)")]
        [Range(0f, 1f)]
        public float requiredAccuracy = 0.7f;
        
        public List<string> requiredLevelIds = new List<string>();
    }

    [Header("Levels")]
    public List<LevelData> availableLevels = new List<LevelData>();
    public List<LevelData> availableSparLevels = new List<LevelData>();

    [Header("UI Stuff")]
    public GameObject levelSelectionPanel;
    public GameObject levelButtonPrefab;
    public Transform levelButtonContainer;
    public Transform sparLevelButtonContainer; 
    public TextMeshProUGUI levelDescriptionText;
    public Image levelPreviewImage;
    public TextMeshProUGUI scoreText;
    public Button startLevelButton;
    
    [Header("Tabs")]
    public GameObject pracLevelsTab;
    public GameObject sparLevelsTab;
    public Button pracLevelsTabButton;
    public Button sparLevelsTabButton;

    private IntroLevel selectedIntroLevel;
    public int selectedLevelIndex = -1;
    private bool isViewingSparLevels = false;
    private const string ACCURACY_SAVE_PREFIX = "LevelAccuracy_";
    private const string SCORE_SAVE_PREFIX = "LevelScore_";
    private const string NO_RECORD_TEXT = "No Record";
    private List<Button> levelButtons = new List<Button>();
    private List<Button> sparLevelButtons = new List<Button>();

    private void Start()
    {
        ShowpracLevelsTab();
        
        GenerateLevelButtons();
        UpdateLevelInfoPanel(-1);
        startLevelButton.interactable = false;
        
        if (pracLevelsTabButton != null)
            pracLevelsTabButton.onClick.AddListener(ShowpracLevelsTab);
        
        if (sparLevelsTabButton != null)
            sparLevelsTabButton.onClick.AddListener(ShowSparLevelsTab);
    }
    
    public void ShowpracLevelsTab()
    {
        isViewingSparLevels = false;
        
        if (pracLevelsTab != null)
            pracLevelsTab.SetActive(true);
        
        if (sparLevelsTab != null)
            sparLevelsTab.SetActive(false);
        
        selectedLevelIndex = -1;
        selectedIntroLevel = null;
        UpdateLevelInfoPanel(-1);
        startLevelButton.interactable = false;
        
        if (pracLevelsTabButton != null)
            pracLevelsTabButton.interactable = false;
        
        if (sparLevelsTabButton != null)
            sparLevelsTabButton.interactable = true;
    }
    
    public void ShowSparLevelsTab()
    {
        isViewingSparLevels = true;
        
        if (pracLevelsTab != null)
            pracLevelsTab.SetActive(false);
        
        if (sparLevelsTab != null)
            sparLevelsTab.SetActive(true);
        
        selectedLevelIndex = -1;
        selectedIntroLevel = null;
        UpdateLevelInfoPanel(-1);
        startLevelButton.interactable = false;
        
        if (pracLevelsTabButton != null)
            pracLevelsTabButton.interactable = true;
        
        if (sparLevelsTabButton != null)
            sparLevelsTabButton.interactable = false;
    }

    private void GenerateLevelButtons()
    {
        GenerateButtonsForList(availableLevels, levelButtonContainer, levelButtons, false);
        
        GenerateButtonsForList(availableSparLevels, sparLevelButtonContainer, sparLevelButtons, true);
        
        UpdateLevelButtonsState();
    }
    
    private void GenerateButtonsForList(List<LevelData> levels, Transform container, List<Button> buttonsList, bool isSparLevel)
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
                else
                {
                    button.onClick.AddListener(() => SelectLevel(levelIndex));
                }
            }
        }
    }

    public void UpdateLevelButtonsState()
    {
        UpdateButtonsStateForList(levelButtons, availableLevels, false);
        
        UpdateButtonsStateForList(sparLevelButtons, availableSparLevels, true);
    }
    
    private void UpdateButtonsStateForList(List<Button> buttons, List<LevelData> levels, bool isSparLevel)
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
        
        foreach (string requiredLevelId in sparLevel.requiredLevelIds)
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
                        if (score < sparLevel.requiredScore)
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
        SelectLevelFromList(levelIndex, availableLevels, levelButtons, false);
    }
    
    public void SelectSparLevel(int levelIndex)
    {
        SelectLevelFromList(levelIndex, availableSparLevels, sparLevelButtons, true);
    }
    
    public void SelectLevelFromList(int levelIndex, List<LevelData> levels, List<Button> buttons, bool isSparLevel)
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
                
                if (isSparLevel && levelData.requiredLevelIds.Count > 0)
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
        
        return "Unknown Level";
    }

    public void UpdateLevelInfoPanel(int levelIndex)
    {
        if (isViewingSparLevels)
        {
            UpdateLevelInfoPanel(levelIndex, availableSparLevels);
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
        if (selectedIntroLevel != null)
        {
            levelSelectionPanel.SetActive(false);
            
            string levelId;
            bool isSparLevel;
            
            if (isViewingSparLevels)
            {
                levelId = availableSparLevels[selectedLevelIndex].levelId;
                isSparLevel = true;
            }
            else
            {
                levelId = availableLevels[selectedLevelIndex].levelId;
                isSparLevel = false;
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
        }
    }
    
    public int GetSavedScore(string levelId)
    {
        return PlayerPrefs.GetInt(SCORE_SAVE_PREFIX + levelId, 0);
    }
}