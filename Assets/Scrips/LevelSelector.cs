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
    }

    [Header("Level Data")]
    public List<LevelData> availableLevels = new List<LevelData>();

    [Header("UI References")]
    public GameObject levelSelectionPanel;
    public GameObject levelButtonPrefab;
    public Transform levelButtonContainer;
    public TextMeshProUGUI levelDescriptionText;
    public Image levelPreviewImage;
    public TextMeshProUGUI accuracyText;
    public Button startLevelButton;

    [Header("Level Progression")]
    [Range(0f, 1f)]
    public float requiredAccuracyToUnlock = 0.7f;

    private IntroLevel selectedIntroLevel;
    private int selectedLevelIndex = -1;
    private const string ACCURACY_SAVE_PREFIX = "LevelAccuracy_";
    private const string NO_RECORD_TEXT = "No Record";
    private List<Button> levelButtons = new List<Button>();

    private void Start()
    {
        GenerateLevelButtons();
        UpdateLevelInfoPanel(-1);
        startLevelButton.interactable = false;
    }

    private void GenerateLevelButtons()
    {
        // Clear existing buttons
        foreach (Transform child in levelButtonContainer)
        {
            Destroy(child.gameObject);
        }
        levelButtons.Clear();

        // Generate new buttons
        for (int i = 0; i < availableLevels.Count; i++)
        {
            LevelData levelData = availableLevels[i];
            GameObject buttonObj = Instantiate(levelButtonPrefab, levelButtonContainer);
            
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
            levelButtons.Add(button);
            
            if (button != null)
            {
                button.onClick.AddListener(() => SelectLevel(levelIndex));
            }
        }
        
        // Update level buttons' interactable state based on progression
        UpdateLevelButtonsState();
    }

    private void UpdateLevelButtonsState()
    {
        // First level is always unlocked
        if (levelButtons.Count > 0)
        {
            levelButtons[0].interactable = true;
        }

        // Check subsequent levels
        for (int i = 1; i < levelButtons.Count; i++)
        {
            string previousLevelId = availableLevels[i - 1].levelId;
            float previousLevelAccuracy = GetSavedAccuracy(previousLevelId);
            
            // Level is unlocked if previous level has at least required accuracy
            bool isUnlocked = previousLevelAccuracy >= requiredAccuracyToUnlock;
            levelButtons[i].interactable = isUnlocked;
            
            // Add visual indicator for locked levels
            Transform lockIndicator = levelButtons[i].transform.Find("LockIndicator");
            if (lockIndicator != null)
            {
                lockIndicator.gameObject.SetActive(!isUnlocked);
            }
            else
            {
                // If there's no dedicated lock indicator, just gray out the button
                ColorBlock colors = levelButtons[i].colors;
                if (!isUnlocked)
                {
                    // Apply darker tint to locked levels
                    colors.normalColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                    colors.highlightedColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
                    colors.pressedColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
                    colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
                }
                levelButtons[i].colors = colors;
            }
        }
    }

    public void SelectLevel(int levelIndex)
    {
        if (levelIndex >= 0 && levelIndex < availableLevels.Count)
        {
            // Only select if the level button is interactable (level is unlocked)
            if (levelButtons[levelIndex].interactable)
            {
                selectedLevelIndex = levelIndex;
                selectedIntroLevel = availableLevels[levelIndex].introLevel;
                
                UpdateLevelInfoPanel(levelIndex);
                startLevelButton.interactable = true;
            }
            else
            {
                string previousLevelName = availableLevels[levelIndex - 1].levelName;
                float previousLevelAccuracy = GetSavedAccuracy(availableLevels[levelIndex - 1].levelId);
                float requiredPercentage = requiredAccuracyToUnlock * 100f;
                
                string lockMessage = $"This level is locked. Complete \"{previousLevelName}\" with at least {requiredPercentage}% accuracy to unlock.";
                if (previousLevelAccuracy > 0)
                {
                    float currentPercentage = previousLevelAccuracy * 100f;
                    lockMessage += $"\nCurrent accuracy: {currentPercentage:F0}%";
                }
                
                levelDescriptionText.text = lockMessage;
                levelPreviewImage.gameObject.SetActive(false);
                accuracyText.gameObject.SetActive(false);
                startLevelButton.interactable = false;
            }
        }
    }

    private void UpdateLevelInfoPanel(int levelIndex)
    {
        if (levelIndex >= 0 && levelIndex < availableLevels.Count)
        {
            LevelData levelData = availableLevels[levelIndex];
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
            
            if (selectedIntroLevel.gameObject.GetComponent<SaveAccuracy>() == null)
            {
                SaveAccuracy observer = selectedIntroLevel.gameObject.AddComponent<SaveAccuracy>();
                observer.Initialize(this, availableLevels[selectedLevelIndex].levelId);
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