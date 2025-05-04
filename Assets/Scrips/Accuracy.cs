using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AccuracyTracker : MonoBehaviour
{
    public static AccuracyTracker Instance;

    [Header("Tracking")]
    [SerializeField] private int totalBoxes;
    [SerializeField] private int totalBoxesTouched;
    [SerializeField] private List<int> objectiveTotalBoxes = new List<int>();
    [SerializeField] private List<int> objectiveTouchedBoxes = new List<int>();

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ResetTracking()
    {
        totalBoxes = 0;
        totalBoxesTouched = 0;
        objectiveTotalBoxes.Clear();
        objectiveTouchedBoxes.Clear();

    }

    public void RecordSequenceData(int sequenceBoxCount, int touchedBoxCount)
    {
        objectiveTotalBoxes.Add(sequenceBoxCount);
        objectiveTouchedBoxes.Add(touchedBoxCount);
        
        totalBoxes += sequenceBoxCount;
        totalBoxesTouched += touchedBoxCount;

        if (showDebugLogs)
            Debug.Log($"AccuracyTracker: Recorded sequence - Boxes: {sequenceBoxCount}, Touched: {touchedBoxCount}");
    }

    public float CalculateAccuracy()
    {
        if (totalBoxes <= 0)
            return 0f;

        float accuracy = (float)totalBoxesTouched / totalBoxes;
        
        if (showDebugLogs)
            Debug.Log($"AccuracyTracker: Final accuracy - {accuracy * 100}% ({totalBoxesTouched}/{totalBoxes})");
            
        return accuracy;
    }
    
    public int GetTotalBoxes()
    {
        return totalBoxes;
    }
    
    public int GetTotalBoxesTouched()
    {
        return totalBoxesTouched;
    }
}