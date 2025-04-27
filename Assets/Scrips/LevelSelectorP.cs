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
    }

    [Header("Level Data")]
    public List<LevelData> availableLevels = new List<LevelData>();

    [Header("UI References")]
    public GameObject levelSelectionPanel;
    public GameObject levelButtonPrefab;
    public Transform levelButtonContainer;
    public TextMeshProUGUI levelDescriptionText;
    public Image levelPreviewImage;
    public Button startLevelButton;

    private IntroLevel selectedIntroLevel;
    private int selectedLevelIndex = -1;

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
        }
        else
        {
            levelDescriptionText.text = "Select level";
            levelPreviewImage.gameObject.SetActive(false);
        }
    }

    public void StartSelectedLevel()
    {
        if (selectedIntroLevel != null)
        {
            levelSelectionPanel.SetActive(false);
            
            selectedIntroLevel.ActivateIntro();
        }
    }

}