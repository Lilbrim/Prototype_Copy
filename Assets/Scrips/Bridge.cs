using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEventBridge : MonoBehaviour
{
    public void OnObjectiveStart(int objectiveIndex)
    {
        if (SparManager.Instance != null)
        {
            SparManager.Instance.OnAnimationEventObjectiveStart(objectiveIndex);
            Debug.Log($"Animation Event Bridge: Objective {objectiveIndex} started");
        }
        else
        {
            Debug.LogWarning("SparManager.Instance is null when trying to call OnAnimationEventObjectiveStart");
        }
    }

    public void OnObjectiveEnd(int objectiveIndex)
    {
        if (SparManager.Instance != null)
        {
            SparManager.Instance.OnAnimationEventObjectiveEnd(objectiveIndex);
            Debug.Log($"Animation Event Bridge: Objective {objectiveIndex} ended");
        }
        else
        {
            Debug.LogWarning("SparManager.Instance is null when trying to call OnAnimationEventObjectiveEnd");
        }
    }
    
    
    
    public void ObjectiveWindowOpen(int objectiveIndex)
    {
        OnObjectiveStart(objectiveIndex);
    }
    
    public void ObjectiveWindowClose(int objectiveIndex)
    {
        OnObjectiveEnd(objectiveIndex);
    }
    
    public void OnObjectiveEvent(string eventType)
    {
        
        string[] parts = eventType.Split('_');
        if (parts.Length == 2)
        {
            string action = parts[0];
            if (int.TryParse(parts[1], out int objectiveIndex))
            {
                if (action == "start")
                {
                    OnObjectiveStart(objectiveIndex);
                }
                else if (action == "end")
                {
                    OnObjectiveEnd(objectiveIndex);
                }
            }
        }
    }
}