using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveAccuracy : MonoBehaviour
{
    private LevelSelector levelSelector;
    private string levelId;
    private bool isInitialized = false;

    public void Initialize(LevelSelector selector, string id)
    {
        levelSelector = selector;
        levelId = id;
        isInitialized = true;
        Debug.Log($"SaveAccuracy initialized for level: {levelId}");
    }

    public void OnLevelCompleted(float accuracy)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("SaveAccuracy not initialized!");
            return;
        }

        SaveLevelAccuracy(accuracy);
    }

    private void SaveLevelAccuracy(float accuracy)
    {
        if (levelSelector != null)
        {
            levelSelector.SaveLevelAccuracy(levelId, accuracy);
            Debug.Log($"Saved accuracy for level {levelId}: {accuracy:P0}");
            
            levelSelector.UpdateLevelButtonsState();
            
            if (levelSelector.selectedLevelIndex >= 0)
            {
                levelSelector.UpdateLevelInfoPanel(levelSelector.selectedLevelIndex);
            }
        }
        else
        {
            Debug.LogWarning("Could not save level accuracy - LevelSelector not found");
        }
    }
}