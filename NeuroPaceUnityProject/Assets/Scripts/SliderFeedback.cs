using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SliderFeedback : MonoBehaviour
{
    private Slider slider;
    public TextMeshProUGUI output;

    public void Start()
    {
        slider = gameObject.GetComponent<Slider>();
        slider.onValueChanged.AddListener(delegate { Feedback(); });
    }

    public void Feedback()
    {
        output.text = slider.value.ToString();
    }
}
