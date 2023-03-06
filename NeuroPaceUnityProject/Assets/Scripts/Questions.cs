using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Questions : MonoBehaviour
{
    private Slider[] answers;
    private Button button;
    public GameBrain gb;

    private void Start()
    {
        answers = gameObject.GetComponentsInChildren<Slider>();
        button = gameObject.GetComponentInChildren<Button>();        
    }

    public void Submit()
    {
        string logLine = "";
        foreach (Slider s in answers)
            logLine += " " + s.value.ToString();

        gb.SaveLogLine(gb.game_id, logLine.Trim(), 0);
        gameObject.SetActive(false);
    }
}