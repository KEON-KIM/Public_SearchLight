using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeAnimation : MonoBehaviour
{
    [SerializeField]
    [Range(0.01f, 10f)]
    private float fadeTime;

    public Image fadeObject;
    
    void Awake()
    {

        Debug.Log("FadeAnimation 설정 완료");
        StartCoroutine(FadeIn(1.0f, 0.0f));

    }
    private IEnumerator FadeIn(float start, float end)
    {
        float currentTime = 0.0f;
        float percent = 0.0f;
        fadeObject.gameObject.SetActive(true);
        while (percent < 1)
        {
            currentTime += Time.deltaTime;
            percent = currentTime / fadeTime;

            Color color = fadeObject.color;
            color.a = Mathf.Lerp(start, end, percent);
            fadeObject.color = color;

            yield return null;
        }
        fadeObject.gameObject.SetActive(false);
    }
    
}
