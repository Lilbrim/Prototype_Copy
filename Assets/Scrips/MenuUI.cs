using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public GameObject start;
    public GameObject menu;
    public GameObject begin;

    void Start()
    {
        start.SetActive(true);
        menu.SetActive(false);
        begin.SetActive(false);
    }

    public void OnStartButtonPressed()
    {
        start.SetActive(false);
        menu.SetActive(true);
    }

    public void OnBeginButtonPressed()
    {
        menu.SetActive(false);
        begin.SetActive(true);
    }

    public void OnExitButtonPressed()
    {
        if (begin.activeSelf)
        {
            begin.SetActive(false);
            menu.SetActive(true);
        }
        else if (menu.activeSelf)
        {
            menu.SetActive(false);
            start.SetActive(true);
        }
    }

    public void OnTutorialButtonPressed()
    {
        SceneManager.LoadScene("Tutorial");
    }

    public void OnQuitButtonPressed()
    {
        Application.Quit();
    }
}