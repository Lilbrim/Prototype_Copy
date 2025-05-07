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
    }

    [Header("Level Data")]
    public List<LevelData> availableLevels = new List<LevelData>();
    public List<LevelData> availableSparLevels = new List<LevelData>();

    [Header("UI References")]
    public GameObject levelSelectionPanel;
    public GameObject levelButtonPrefab;
    public Transform levelButtonContainer;
    public Transform sparLevelButtonContainer; 
    public TextMeshProUGUI levelDescriptionText;
    public Image levelPreviewImage;
    public TextMeshProUGUI accuracyText;
    public Button startLevelButton;
    
    [Header("Tab System")]
    public GameObject normalLevelsTab;
    public GameObject sparLevelsTab;
    public Button normalLevelsTabButton;
    public Button sparLevelsTabButton;

    [Header("Level Progression")]
    [Range(0f, 1f)]
    public float requiredAccuracyToUnlock = 0.7f;

    private IntroLevel selectedIntroLevel;
    private int selectedLevelIndex = -1;
    private bool isViewingSparLevels = false;
    private const string ACCURACY_SAVE_PREFIX = "LevelAccuracy_";
    private const string NO_RECORD_TEXT = "No Record";
    private List<Button> levelButtons = new List<Button>();
    private List<Button> sparLevelButtons = new List<Button>();

    private void Start()
    {
        ShowNormalLevelsTab();
        
        GenerateLevelButtons();
        UpdateLevelInfoPanel(-1);
        startLevelButton.interactable = false;
        
        if (normalLevelsTabButton != null)
            normalLevelsTabButton.onClick.AddListener(ShowNormalLevelsTab);
        
        if (sparLevelsTabButton != null)
            sparLevelsTabButton.onClick.AddListener(ShowSparLevelsTab);
    }
    
    public void ShowNormalLevelsTab()
    {
        isViewingSparLevels = false;
        
        if (normalLevelsTab != null)
            normalLevelsTab.SetActive(true);
        
        if (sparLevelsTab != null)
            sparLevelsTab.SetActive(false);
        
        selectedLevelIndex = -1;
        selectedIntroLevel = null;
        UpdateLevelInfoPanel(-1);
        startLevelButton.interactable = false;
        
        if (normalLevelsTabButton != null)
            normalLevelsTabButton.interactable = false;
        
        if (sparLevelsTabButton != null)
            sparLevelsTabButton.interactable = true;
    }
    
    public void ShowSparLevelsTab()
    {
        isViewingSparLevels = true;
        
        if (normalLevelsTab != null)
            normalLevelsTab.SetActive(false);
        
        if (sparLevelsTab != null)
            sparLevelsTab.SetActive(true);
        
        selectedLevelIndex = -1;
        selectedIntroLevel = null;
        UpdateLevelInfoPanel(-1);
        startLevelButton.interactable = false;
        
        if (normalLevelsTabButton != null)
            normalLevelsTabButton.interactable = true;
        
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
            
            float savedAccuracy = GetSavedAccuracy(levelData.levelId);
            Transform accuracyIndicator = buttonObj.transform.Find("AccuracyIndicator");
            if (accuracyIndicator != null && accuracyIndicator.GetComponent<TextMeshProUGUI>() != null)
            {
                if (savedAccuracy > 0)
                {
                    accuracyIndicator.GetComponent<TextMeshProUGUI>().text = $"{savedAccuracy:P0}";
                }
                else
                {
                    accuracyIndicator.GetComponent<TextMeshProUGUI>().text = NO_RECORD_TEXT;
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

    private void UpdateLevelButtonsState()
    {
        UpdateButtonsStateForList(levelButtons, availableLevels);
        
        UpdateButtonsStateForList(sparLevelButtons, availableSparLevels);
    }
    
    private void UpdateButtonsStateForList(List<Button> buttons, List<LevelData> levels)
    {
        if (buttons.Count > 0)
        {
            buttons[0].interactable = true;
        }

        for (int i = 1; i < buttons.Count; i++)
        {
            string previousLevelId = levels[i - 1].levelId;
            float previousLevelAccuracy = GetSavedAccuracy(previousLevelId);
            
            bool isUnlocked = previousLevelAccuracy >= requiredAccuracyToUnlock;
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

    public void SelectLevel(int levelIndex)
    {
        SelectLevelFromList(levelIndex, availableLevels, levelButtons, false);
    }
    
    public void SelectSparLevel(int levelIndex)
    {
        SelectLevelFromList(levelIndex, availableSparLevels, sparLevelButtons, true);
    }
    
    private void SelectLevelFromList(int levelIndex, List<LevelData> levels, List<Button> buttons, bool isSparLevel)
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
                string previousLevelName = levels[levelIndex - 1].levelName;
                float previousLevelAccuracy = GetSavedAccuracy(levels[levelIndex - 1].levelId);
                float requiredPercentage = requiredAccuracyToUnlock * 100f;
            }      
        }
    }

    private void UpdateLevelInfoPanel(int levelIndex)
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
            
            float savedAccuracy = GetSavedAccuracy(levelData.levelId);
            accuracyText.gameObject.SetActive(true);
            
            if (savedAccuracy > 0)
            {
                accuracyText.text = $"Best Accuracy: {savedAccuracy:P0}";
            }
            else
            {
                accuracyText.text = $"{NO_RECORD_TEXT}";
            }
        }
        else
        {
            levelDescriptionText.text = "Select level";
            levelPreviewImage.gameObject.SetActive(false);
            accuracyText.gameObject.SetActive(false);
        }
    }

    public void StartSelectedLevel()
    {
        if (selectedIntroLevel != null)
        {
            levelSelectionPanel.SetActive(false);
            
            selectedIntroLevel.isSparLevel = isViewingSparLevels;
            
            if (selectedIntroLevel.gameObject.GetComponent<SaveAccuracy>() == null)
            {
                SaveAccuracy observer = selectedIntroLevel.gameObject.AddComponent<SaveAccuracy>();
                
                string levelId;
                if (isViewingSparLevels)
                {
                    levelId = availableSparLevels[selectedLevelIndex].levelId;
                }
                else
                {
                    levelId = availableLevels[selectedLevelIndex].levelId;
                }
                
                observer.Initialize(this, levelId);
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
}