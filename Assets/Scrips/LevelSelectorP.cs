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
        public string levelId; // Unique identifier for saving/loading accuracy data
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

    private IntroLevel selectedIntroLevel;
    private int selectedLevelIndex = -1;
    private const string ACCURACY_SAVE_PREFIX = "LevelAccuracy_";
    private const string NO_RECORD_TEXT = "No Record";

    private void Start()
    {
        GenerateLevelButtons();
        UpdateLevelInfoPanel(-1);
        startLevelButton.interactable = false;
    }

    private void GenerateLevelButtons()
    {
        foreach (Transform child in levelButtonContainer)
        {
            Destroy(child.gameObject);
        }

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
            if (button != null)
            {
                button.onClick.AddListener(() => SelectLevel(levelIndex));
            }
        }
    }

    public void SelectLevel(int levelIndex)
    {
        if (levelIndex >= 0 && levelIndex < availableLevels.Count)
        {
            selectedLevelIndex = levelIndex;
            selectedIntroLevel = availableLevels[levelIndex].introLevel;
            
            UpdateLevelInfoPanel(levelIndex);
            startLevelButton.interactable = true;
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
        }
    }

    public float GetSavedAccuracy(string levelId)
    {
        return PlayerPrefs.GetFloat(ACCURACY_SAVE_PREFIX + levelId, 0f);
    }
}