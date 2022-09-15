using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Player))]
public class PlayerHUD : MonoBehaviour
{
    [SerializeField] Text killScoreText; // 내가 죽인 플레이어 수
    [SerializeField] Text timeLimitText;
    [SerializeField] Text magazineAmmoText;
    [SerializeField] Text pouchAmmoText;
    [SerializeField] Text survivedPlayerCountText; // 현재 살아있는 플레이어 수
    [SerializeField] Sprite pistolSprite;
    [SerializeField] Sprite rifleSprite;
    [SerializeField] Image mainSlotImage;
    [SerializeField] Image subSlotImage;
    //GunType currentGunType;

    //private void Start()
    //{
    //    var gunController = GetComponent<GunController>();
    //    gunController.OnWeaponChanged += setAmmoText;
    //    gunController.OnReloadEnd += setAmmoText;
    //    gunController.OnFire += setAmmoText;
    //    /* TODO:
    //     * 무기 변경 넣을 시에 변경 이벤트에 따른 바인딩도 필요
    //     */
    //}

    public void InitializeHUD_Text(Text killScoreText_, Text timeLimitText_,
        Text magazineAmmoText_, Text pouchAmmoText_, Text survivedPlayerCountText_, Image mainSlotImage_, 
        Image subSlotImage_, Sprite pistolSprite_, Sprite rifleSprite_)
    {
        killScoreText = killScoreText_;
        timeLimitText = timeLimitText_;
        magazineAmmoText = magazineAmmoText_;
        pouchAmmoText = pouchAmmoText_;
        survivedPlayerCountText = survivedPlayerCountText_;
        mainSlotImage = mainSlotImage_;
        subSlotImage = subSlotImage_;
        pistolSprite = pistolSprite_;
        rifleSprite = rifleSprite_;

        var gunController = GetComponent<GunController>();
        gunController.OnGetMainWeapon += UpdateRifleImage;
        gunController.OnWeaponChanged += SwapWeaponSlotImage;
        gunController.OnAmmoChanged += SetAmmoText;
        gunController.OnFire += SetAmmoText;

        var player = GetComponent<Player>();
        player.OnAlivePlayerCountUpdate += SetSurvivedPlayerCountText;
        player.OnPlayerKillCountUpdate +=  SetKillScoreText;
    }

    public void SetKillScoreText(int _score)
    {
        killScoreText.text = _score.ToString();
    }

    public void SetTimeLimitText(int _timelimit)
    {
        timeLimitText.text = string.Format("{0}:{1}", _timelimit / 60, _timelimit % 60);
    }

    public void SetAmmoText(int inMagazine, int inPouch)
    {
        magazineAmmoText.text = inMagazine.ToString();
        pouchAmmoText.text = inPouch.ToString();
    }

    public void SwapWeaponSlotImage(GunType type)
    {
        if (type == GunType.MainWeapon)
        {
            SetMainSlotImage(GunType.MainWeapon);
            SetSubSlotImage(GunType.SubWeapon);
        }
        else if (type == GunType.SubWeapon)
        {
            SetMainSlotImage(GunType.SubWeapon);
            SetSubSlotImage(GunType.MainWeapon);
        }
    }

    public void UpdateRifleImage()
    {
        //SetSubSlotImage(GunType.MainWeapon);
        subSlotImage.color = new Color(255, 255, 255, 255);
    }

    public void SetMainSlotImage(GunType type)
    {
        if (type == GunType.MainWeapon)
        {
            mainSlotImage.sprite = rifleSprite;
        }
        else if (type == GunType.SubWeapon)
        {
            mainSlotImage.sprite = pistolSprite;
        }
    }
    public void SetSubSlotImage(GunType type)
    {
        if (type == GunType.MainWeapon)
        {
            subSlotImage.sprite = rifleSprite;
        }
        else if (type == GunType.SubWeapon)
        {
            subSlotImage.sprite = pistolSprite;
        }
    }

    public void SetSurvivedPlayerCountText(int count)
    {
        survivedPlayerCountText.text = count.ToString();
    }

    public void DecreaseSurvivedPlayerCount()
    {
        int count = System.Convert.ToInt32(survivedPlayerCountText.text)-1;
        survivedPlayerCountText.text = (count >= 0) ? count.ToString() : "0";
    }
}
