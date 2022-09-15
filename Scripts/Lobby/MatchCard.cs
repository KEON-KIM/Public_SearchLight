using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MatchCard : MonoBehaviour
{
    public bool isOn;
    public int index = 0;
    private Text matchText;

    /*static readonly Color offColor = new Color((float)207 / 255, (float)207 / 255, (float)207 / 255, 255 / 255);
    static readonly Color onColor = new Color((float)87 / 255, (float)87 / 255, (float)87 / 255, 255 / 255);*/


    void Awake()
    {
        isOn = true;
        matchText = this.GetComponentInChildren<Text>();
    }

    public void SetTabText(string matchTitle)
    {
        matchText.text = matchTitle;
    }

}
