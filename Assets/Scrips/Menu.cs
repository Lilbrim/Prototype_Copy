using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class Menu : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PracticeGame()
    {
        SceneManager.LoadScene("PracticeGame");
    }

    public void Exit()
    {
        Application.Quit();
    }
}
