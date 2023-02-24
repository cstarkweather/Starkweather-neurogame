using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Questions : MonoBehaviour
{
    public int questions_left { get; private set; } = 999;
    private Button[] buttons;
    public GameBrain gb;

    private void Start()
    {
        buttons = gameObject.GetComponentsInChildren<Button>(true);
        questions_left = buttons.Length;
        foreach (Button b in buttons)
            b.onClick.AddListener(() => Submit(b.transform));
    }

    private void Update()
    {
        if (questions_left == 0)
        {
            string logLine = "";

            // collect answers in one line
            var answers = gameObject.GetComponentsInChildren<Slider>(true);
            Array.Reverse(answers);
            foreach (Slider s in answers)
                logLine += " " + s.value.ToString();

            gb.SaveLogLine(gb.game_id, logLine.Trim(), 0);
            gameObject.SetActive(false);
        }
    }

    public void Submit(Transform o)
    {
        while (!GameObject.ReferenceEquals(o.parent, gameObject.transform))
            o = o.parent;
        o.gameObject.SetActive(false);
        questions_left -= 1;
    }
}