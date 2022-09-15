using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Protocol;
using System;

public class GunController : MonoBehaviour
{
    [SerializeField] Transform weaponHold;
    [SerializeField] Gun subGunPrefab;
    [SerializeField] Gun mainGunPrefab;

    [SerializeField] bool isShotButtonActive;
    [SerializeField] float shotDistance;
    [SerializeField] float maxRecoilRadius;

    [SerializeField] int mainAmmoInPouch;
    [SerializeField] int subAmmoInPouch;

    public int MainAmmoInPouch { get; set; }
    public int SubAmmoInPouch { get; set; }
    public Gun equippedGun { get; private set; }
    public Gun mainGun { get; private set; }
    public Gun subGun { get; private set; }
    public Action<int,int> OnFire { get; set; }
    public Action<int,int> OnAmmoChanged { get; set; }

    public Action<GunType> OnWeaponChanged { get; set; }
    public Action OnGetMainWeapon { get; set; }

    float shotCooltime;
    bool isOnShotCooltime;

    Animator playerAnim;

    float currRecoilRadius = 0;
    
    bool isReloading;
    bool isOwner;
    bool isDead;

    
    private void Start()
    {
        InitCommon();
    }

    //Animator 및 시작시 보유할 Gun 초기화, InputManager 및 연동
    void InitCommon()
    {
        if(playerAnim == null)
            playerAnim = GetComponent<Animator>();

        if (subGunPrefab != null)
        {
            EquipGun(GunType.SubWeapon, 0);
            equippedGun = subGun;
            playerAnim.SetInteger("currentWeapon", 0);

            //1.167f는 주무기 / 보조무기 공통 fire 애니메이션 길이
            playerAnim.SetFloat("fireSpeedInterval", 1.167f / equippedGun.ShotCooltime);

            shotCooltime = equippedGun.ShotCooltime;
            equippedGun.inMagazine = equippedGun.MagazineSize;
            if(isOwner)
                OnAmmoChanged(equippedGun.inMagazine, subAmmoInPouch);
        }

        GetComponent<Player>().OnPlayerVisiblityChange += SetEquippedGunVisiblity;
        
    }
    public void InitOwner()
    {
        isOwner = true;
        playerAnim = GetComponent<Animator>();
        BindFireAction();
    }
    void BindFireAction()
    {
        InputManager.GetInstance().AddFireAction(
                startAction: () =>
                {
                    if (!isReloading && !isDead)
                    {
                        isShotButtonActive = true;
                        //playerAnim.SetBool("isShooting", true);
                    }
                },

                stopAction: () =>
                {
                    if (!isDead)
                    {
                        isShotButtonActive = false;
                        SendToMessage(KeyEventCode.STOP_ATTACK);

                        //TODO: 애니메이션 테스트후 적용 여부 결정
                        //equippedGun.transform.forward = weaponHold.transform.forward;


                        //currRecoilRadius = 0;
                        //playerAnim.SetBool("isShooting", false);
                    }
                });

        /*
         * 머지 후 추가된 부분 리로드, 무기교체 액션
         */
        InputManager.GetInstance().AddReloadAction(
            reloadAction_: () =>
            {
                if (!isDead)
                    SendToMessage(KeyEventCode.RELOAD);
                //tryReload();
            });

        InputManager.GetInstance().AddWeaponChangeAction(
            changeAction: () =>
            {
                if (!isDead)
                    SendToMessage(KeyEventCode.SWITCH);
                //ChangeGun();
            });
    }

    //인자로 들어온 Gun 장착 함수
    public void EquipGun(GunType gunType, int grade)
    {
        //if (equippedGun != null)
        //    Destroy(equippedGun.gameObject);

        if(gunType == GunType.MainWeapon)
        {
            mainGun = Instantiate<Gun>(mainGunPrefab, weaponHold.position, weaponHold.rotation);
            mainGun.playerSessionID = GetComponent<Player>().mySessionId;
            mainGun.GradeUpdate(grade);
            mainGun.transform.parent = weaponHold;
            mainGun.SetVisiblity(false);
        }
        else
        {
            subGun = Instantiate<Gun>(subGunPrefab, weaponHold.position, weaponHold.rotation);
            subGun.playerSessionID = GetComponent<Player>().mySessionId;
            subGun.transform.parent = weaponHold;
        }
    }

    public void ChangeGun()
    {
        if (mainGun == null || subGun == null)
            return;

        if (isReloading)
            return;

        isShotButtonActive = false;
        SetEquippedGunVisiblity(false);
        if (equippedGun.GetGunType == GunType.MainWeapon)
        {
            equippedGun = subGun;
            playerAnim.SetInteger("currentWeapon", 0);
            if(isOwner)
            {
                OnAmmoChanged(equippedGun.inMagazine, subAmmoInPouch);
                OnWeaponChanged(GunType.SubWeapon);
            }
        }
        else
        {
            equippedGun = mainGun;
            playerAnim.SetInteger("currentWeapon", 1);
            
            if(isOwner)
            {
                OnAmmoChanged(equippedGun.inMagazine, mainAmmoInPouch);
                OnWeaponChanged(GunType.MainWeapon);
            }
            
        }

        playerAnim.SetFloat("fireSpeedInterval", 1.167f / equippedGun.ShotCooltime);
        shotCooltime = equippedGun.ShotCooltime;
        SetEquippedGunVisiblity(true);

    }

    //재장전
    public void tryReload()
    {
        if (isReloading)
            return;
        if (equippedGun.GetGunType == GunType.SubWeapon && subAmmoInPouch <= 0)
            return;
        if (equippedGun.GetGunType == GunType.MainWeapon && mainAmmoInPouch <= 0)
            return;

        if (equippedGun.inMagazine < equippedGun.MagazineSize)
        {
            isShotButtonActive = false;
            playerAnim.SetBool("isShooting", false);
            playerAnim.SetTrigger("reload");
            SoundManager.GetInstance().PlaySoundAtPoint(SoundEffect.Reload, transform.position);
            isReloading = true;
        }
    }

    //애니메이션 이벤트
    public void AnimNotify_OnReloadEnd() // 리로드 애님 종료시 콜백함수
    {
        if (equippedGun.GetGunType == GunType.SubWeapon)
        {
            int blankAmount = equippedGun.MagazineSize - equippedGun.inMagazine;
            if (subAmmoInPouch >= blankAmount)
            {
                equippedGun.inMagazine += blankAmount;
                subAmmoInPouch -= blankAmount;
            }
            else
            {
                equippedGun.inMagazine += subAmmoInPouch;
                subAmmoInPouch = 0;
            }

            if (isOwner)
                OnAmmoChanged(equippedGun.inMagazine, subAmmoInPouch);
        }
        else
        {
            int blankAmount = equippedGun.MagazineSize - equippedGun.inMagazine;
            if (mainAmmoInPouch >= blankAmount)
            {
                equippedGun.inMagazine += blankAmount;
                mainAmmoInPouch -= blankAmount;
            }
            else
            {
                equippedGun.inMagazine += mainAmmoInPouch;
                mainAmmoInPouch = 0;
            }
            if (isOwner)
                OnAmmoChanged(equippedGun.inMagazine, mainAmmoInPouch);
        }
        isReloading = false;
        if(isShotButtonActive)
            playerAnim.SetBool("isShooting", true);
    }

    public void AnimNotify_OnDead()
    {
        isDead = true;
        isShotButtonActive = false;
        equippedGun.transform.forward = weaponHold.transform.forward;
    }

    public void TryFire()
    {
        if (!isOwner || isReloading)
            return;

        if (isShotButtonActive && isOnShotCooltime == false)
        {
            if (equippedGun.inMagazine > 0)
            {
                Vector3 targetPos = GetShotDirection();
                SendToMessage(targetPos);
                StartCoroutine("StartShotCooltime");

                if (currRecoilRadius < maxRecoilRadius)
                    currRecoilRadius += Time.deltaTime;
            }
            else
            {
                SendToMessage(KeyEventCode.RELOAD);
                //tryReload();
            }
        }


    }
    public void FireAction(Vector3 targetPos)
    {
        if (!isReloading)
        {
            equippedGun.Fire(targetPos);
            //isShotButtonActive = true;
            playerAnim.SetBool("isShooting", true);

            if (equippedGun.GetGunType == GunType.SubWeapon && isOwner)
                OnFire(equippedGun.inMagazine, subAmmoInPouch); 
            else if (equippedGun.GetGunType == GunType.MainWeapon && isOwner)
                OnFire(equippedGun.inMagazine, mainAmmoInPouch);
        }
    }

    public Vector3 GetShotDirection()
    {
        //총구 방향 캐릭터 정면으로 정렬
        
        //총기 반동 범위 계산
        Vector3 circlePos = transform.position + transform.forward * shotDistance;
        Vector2 randomCirclePoint = UnityEngine.Random.insideUnitCircle.normalized * currRecoilRadius;
        Vector3 targetPos = new Vector3(circlePos.x + randomCirclePoint.x, 0, circlePos.z + randomCirclePoint.y);
        return targetPos;
    }

    private void Update()
    {
        if (!isDead)
        {
            equippedGun.transform.forward = transform.forward;
            TryFire();
        }
    }

    /*private void Update()
    {
        equippedGun.transform.forward = this.gameObject.transform.forward;
        TryFire();
    }*/

    public void FireStopAction() // 메세지 받고 실행할 애님 액션
    {
        //isShotButtonActive = false;
        currRecoilRadius = 0;
        playerAnim.SetBool("isShooting", false);
    }

    public void OnGetSubAmmo(int amount)
    {
        subAmmoInPouch += amount;
        if (equippedGun.GetGunType == GunType.SubWeapon && isOwner)
            OnAmmoChanged(equippedGun.inMagazine, subAmmoInPouch);
    }

    public void OnGetMainAmmo(int amount)
    {
        mainAmmoInPouch += amount;
        if (equippedGun.GetGunType == GunType.MainWeapon && isOwner)
            OnAmmoChanged(equippedGun.inMagazine, mainAmmoInPouch);
    }

    private void SendToMessage(Vector3 targetPos)
    {
        int keyCode = 0;
        keyCode |= KeyEventCode.ATTACK;

        if (keyCode <= 0)
        {
            return; // NONE;
        }

        Debug.Log("공격"+ keyCode);
        KeyMessage msg;
        msg = new KeyMessage(keyCode, targetPos);
        if (ServerMatchManager.GetInstance().IsHost())
        {
            Debug.Log("호스트 메세지를 송신합니다.");
            ServerMatchManager.GetInstance().AddMsgToLocalQueue(msg);
        }

        else
        {
            Debug.Log("비 호스트 메세지를 송신합니다.");
            ServerMatchManager.GetInstance().SendDataToInGame<KeyMessage>(msg);
        }
    }
    /*
    private void SendToStopAttackMessage()
    {
        int keyCode = 0;
        keyCode |= KeyEventCode.STOP_ATTACK;


        if (keyCode <= 0)
        {
            return; // NONE;
        }


        KeyMessage msg;
        msg = new KeyMessage(keyCode, Vector3.zero);
        if (ServerMatchManager.GetInstance().IsHost())
        {
            Debug.Log("호스트 메세지를 송신합니다.");
            ServerMatchManager.GetInstance().AddMsgToLocalQueue(msg);
        }

        else
        {
            Debug.Log("비 호스트 메세지를 송신합니다.");
            ServerMatchManager.GetInstance().SendDataToInGame<KeyMessage>(msg);
        }
    }*/

    private void SendToMessage(int keyCode)
    {
        if (keyCode <= 0)
        {
            return; // NONE;
        }


        KeyMessage msg;
        msg = new KeyMessage(keyCode, Vector3.zero);
        Debug.Log("메세지" + keyCode);
        if (ServerMatchManager.GetInstance().IsHost())
        {
            Debug.Log("호스트 메세지를 송신합니다.");
            ServerMatchManager.GetInstance().AddMsgToLocalQueue(msg);
        }

        else
        {
            Debug.Log("비 호스트 메세지를 송신합니다.");
            ServerMatchManager.GetInstance().SendDataToInGame<KeyMessage>(msg);
        }
    }

    
    public void SetEquippedGunVisiblity(bool flag)
    {
        equippedGun.SetVisiblity(flag);
    }

    //발사 쿨타임 코루틴
    IEnumerator StartShotCooltime()
    {
        isOnShotCooltime = true;
        yield return new WaitForSeconds(shotCooltime);
        isOnShotCooltime = false;
    }

    public void GetWeaponItem(GunType type, int newGrade, int ammoAmount)
    {
        if(type == GunType.MainWeapon)
        {
            if(mainGun == null)
            {
                EquipGun(GunType.MainWeapon, newGrade);
                if(isOwner)
                    OnGetMainWeapon();
                return;
            }

            if (newGrade <= mainGun.grade)
                OnGetMainAmmo(ammoAmount);
            else
                mainGun.GradeUpdate(newGrade);
        }
        else
        {
            if (newGrade <= subGun.grade)
                OnGetSubAmmo(ammoAmount);
            else
                subGun.GradeUpdate(newGrade);
        }
    }

}
