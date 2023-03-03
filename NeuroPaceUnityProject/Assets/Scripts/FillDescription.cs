using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FillDescription : MonoBehaviour
{
    private GameBrain gb;
    private TextMeshProUGUI textField;
    private bool filled = false;

    // Start is called before the first frame update
    void Start()
    {
        gb = Camera.main.gameObject.GetComponent<GameBrain>();
        textField = gameObject.GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        if (gb.game_params != null && !filled)
        {
            string parsedText = textField.text;
            parsedText = parsedText.Replace("{trialsCount}", $"{gb.trials_count}");
            parsedText = parsedText.Replace("{goalDescription}", $"{gb.game_params.game_settings.goal_description}");
            parsedText = parsedText.Replace("{KeyToSkip}", $"{gb.key_skip}");
            parsedText = parsedText.Replace("{KeyToTry}", $"{gb.key_try}");
            textField.text = parsedText;
            filled = true;
        }
    }
}
