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
    }

    public void OnLevelCompleted()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("not initialized!");
            return;
        }

        if (AccuracyTracker.Instance != null)
        {
            float accuracy = AccuracyTracker.Instance.CalculateAccuracy();
            SaveLevelAccuracy(accuracy);
        }
    }

    private void SaveLevelAccuracy(float accuracy)
    {
        if (levelSelector != null)
        {
            levelSelector.SaveLevelAccuracy(levelId, accuracy);
            Debug.Log($"Saved accuracy for level {levelId}: {accuracy:P0}");
        }
        else
        {
            Debug.LogWarning("Could not save level accuracy - LevelSelector not found");
        }
    }
}