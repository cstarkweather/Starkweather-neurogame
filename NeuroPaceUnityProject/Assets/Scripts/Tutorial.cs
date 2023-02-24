using UnityEngine;
using UnityEngine.UI;

public class Tutorial : MonoBehaviour
{
    public int tutorials_left { get; private set; } = 999;
    private Button[] buttons;

    private void Start()
    {
        buttons = gameObject.GetComponentsInChildren<Button>(true);
        tutorials_left = buttons.Length;
        foreach(Button b in buttons)
            b.onClick.AddListener(() => Submit(b.transform));
    }

    public void Submit(Transform o)
    {
        while (!GameObject.ReferenceEquals(o.parent, gameObject.transform))
            o = o.parent;
        o.gameObject.SetActive(false);
        tutorials_left -= 1;
    }
}