using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] Vector3 offset;

    Player player;
    RectTransform rectTransform;
    Image hpBar;
    Image shieldBar;

    void Awake()
    {
        hpBar = transform.Find("hpBar").GetComponent<Image>();
        shieldBar = transform.Find("shieldBar").GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
    }


    public void InitStatusBar(GameObject player_)
    {
        player = player_.GetComponent<Player>();
        player.OnHPChange += SetHPPercent;
        player.OnShieldChange += SetShieldPercent;
        player.OnPlayerVisiblityChange += SetVisiblity;
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        rectTransform.position = Camera.main.WorldToScreenPoint(player.transform.position + offset);
    }

    void SetVisiblity(bool flag)
    {
        gameObject.SetActive(flag);
    }



    void SetHPPercent(float hpPercent)
    {
        hpBar.fillAmount = hpPercent;
    }
    void SetShieldPercent(float shieldPercent)
    {
        shieldBar.fillAmount = shieldPercent;
    }
}
