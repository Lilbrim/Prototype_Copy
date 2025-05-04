using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerPrefsManager : MonoBehaviour
{
    public Button resetButton;
    public GameObject confirmationPanel;
    public Button confirmButton;
    public Button cancelButton;
    public TextMeshProUGUI confirmationText;

    private void Start()
    {
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ShowResetConfirmation);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(ConfirmReset);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(CancelReset);
        }

        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }
    }

    public void ShowResetConfirmation()
    {
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(true);
            
            if (confirmationText != null)
            {
                confirmationText.text = "Reset?";
            }
        }
    }

    public void ConfirmReset()
    {
        PlayerPrefs.DeleteAll();
        Debug.Log("Deleted");

        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }
    }

    public void CancelReset()
    {
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }
    }
}