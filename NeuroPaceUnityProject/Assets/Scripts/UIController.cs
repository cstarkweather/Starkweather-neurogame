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

    private float rubies = 0;
    public int rubiesTarget = 20;

    private void Start()
    {
        rubies = rubiesTarget;
        updateCrystalsCounter();
    }

    private void Update()
    {
        if (rubies != rubiesTarget)
        {
            crystalsUI.color = new Color(1, 1, 1, 1);
            crystalsUI.fontSize = 60;
            float change = Time.deltaTime * 10;
            rubies = (rubies < rubiesTarget) ? Mathf.Min(rubies + change, rubiesTarget) : Mathf.Max(rubies - change, rubiesTarget);
            updateCrystalsCounter();
        }
        else
        {
            crystalsUI.color = new Color(0.75f, 0.75f, 0.75f, 1);
            crystalsUI.fontSize = 55;
        }
    }

    public void updateCrystalsCounter() { 
        crystalsUI.text = "<sprite index=4>rubies: " + Mathf.RoundToInt(rubies).ToString();
    }

    public void printRounds(int round)
    {
        roundsUI.text = round.ToString();
    }

    public void printInfo(string message)
    {
        infoUI.text = message;
    }

    public void printDescription(string message)
    {
        descriptionUI.transform.parent.gameObject.SetActive(message != "");
        descriptionUI.text = message;
    }
    public void printEndScreen(string message)
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
