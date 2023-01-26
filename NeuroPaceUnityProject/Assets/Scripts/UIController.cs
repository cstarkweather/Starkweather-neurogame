using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIController : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI roundsUI;
    [SerializeField]
    private TextMeshProUGUI crystalsUI;
    [SerializeField]
    private TextMeshProUGUI infoUI;
    [SerializeField]
    private TextMeshProUGUI descriptionUI;
    [SerializeField]
    private GameObject endScreenUI;

    public void setCrystals(int amount) { 
        crystalsUI.text = "<sprite index=0>rubies: " + amount.ToString();
    }

    public void setRounds(int round)
    {
        roundsUI.text = round.ToString();
    }

    public void setInfo(string message)
    {
        infoUI.text = message;
    }

    public void setDescription(string message)
    {
        descriptionUI.transform.parent.gameObject.SetActive(message != "");
        descriptionUI.text = message;
    }
    public void setEndScreen(string message)
    {
        if (message == "")
            endScreenUI.SetActive(false);
        else
        {
            endScreenUI.SetActive(true);
            endScreenUI.GetComponentInChildren<TextMeshProUGUI>().text = message;
        }
    }
}
