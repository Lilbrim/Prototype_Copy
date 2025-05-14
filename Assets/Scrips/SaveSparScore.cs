using UnityEngine;

public class SaveSparScore : MonoBehaviour
{
    private LevelSelector levelSelector;
    private string levelId;
    private bool isInitialized = false;

    public void Initialize(LevelSelector selector, string id)
    {
        levelSelector = selector;
        levelId = id;
        isInitialized = true;
        Debug.Log($"Save initialized for level: {id}");
    }

    public void OnSparCompleted(int playerScore, int opponentScore)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("Save not initialized!");
            return;
        }

        SaveLevelScore(playerScore);
    }

    private void SaveLevelScore(int score)
    {
        if (levelSelector != null)
        {
            levelSelector.SaveLevelScore(levelId, score);
            Debug.Log($"Saved score for spar level {levelId}: {score}");
        }
        else
        {
            Debug.LogWarning("Error");
        }
    }
}