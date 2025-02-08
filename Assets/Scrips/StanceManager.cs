using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StanceManager : MonoBehaviour
{
    public static StanceManager Instance;

    public enum Stance { Default, BasicStrike, Redonda }
    public Stance currentStance = Stance.Default;

    public GameObject[] defaultBoxes;
    public GameObject[] basicStrikeBoxes;
    public GameObject[] redondaBoxes;

    public float stanceTimeout =2f; 
    private float timer;

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

    private void Start()
    {
        SetStance(Stance.Default);
    }

    private void Update()
    {
        if (currentStance != Stance.Default && !IsAnyBatonInStanceBox())
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                SetStance(Stance.Default);
            }
        }
    }

    public void EnterStance(string stanceName)
    {
        if (stanceName == "BasicStrike" && currentStance != Stance.BasicStrike)
        {
            SetStance(Stance.BasicStrike);
        }
        else if (stanceName == "Redonda" && currentStance != Stance.Redonda)
        {
            SetStance(Stance.Redonda);
        }
    }

    private void SetStance(Stance newStance)
    {
        StanceDetector[] allDetectors = FindObjectsOfType<StanceDetector>();
        foreach (var detector in allDetectors)
        {
            detector.ResetStance();
        }

        currentStance = newStance;
        timer = stanceTimeout;

        foreach (var box in defaultBoxes) box.SetActive(false);
        foreach (var box in basicStrikeBoxes) box.SetActive(false);
        foreach (var box in redondaBoxes) box.SetActive(false);

        switch (currentStance)
        {
            case Stance.Default:
                foreach (var box in defaultBoxes) box.SetActive(true);
                break;
            case Stance.BasicStrike:
                foreach (var box in basicStrikeBoxes) box.SetActive(true);
                break;
            case Stance.Redonda:
                foreach (var box in redondaBoxes) box.SetActive(true);
                break;
        }
    }

    private bool IsAnyBatonInStanceBox()
    {
        StanceDetector[] allDetectors = FindObjectsOfType<StanceDetector>();
        foreach (var detector in allDetectors)
        {
            if (detector.IsLeftHandInStance() || detector.IsRightHandInStance())
            {
                return true; 
            }
        }
        return false;
    }
}