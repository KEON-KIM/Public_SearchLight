using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using BackEnd.Tcp;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(GunController))]
public class Player : MonoBehaviour, IDamageable
{
    [SerializeField] float maxHP = 100;
    [SerializeField] float currentHP = 100;
    [SerializeField] float maxShield = 100;
    [SerializeField] float currentShield = 100;
    [SerializeField] float moveSpeed;
    [SerializeField] float sightDistance;
    [SerializeField] float sightAngle;

    [SerializeField] Detect detector;
    [SerializeField] Light spotLight;
    [SerializeField] Light pointLight;
    [SerializeField] PoolingParticle bloodParticlePrefab;
    [SerializeField] int maxParticlePoolSize;

    string bloodParticlePoolKey;

    public float MoveSpeed { get { return moveSpeed; } private set { } }
    public Action<float> OnHPChange{ get; set; }
    public Action<float> OnShieldChange { get; set; }
    //컨트롤러 및 카메라 초기화
    public Action OnSetAsOwner { get; set; }

    public Action<bool> OnPlayerVisiblityChange { get; set; }
    public Action<int> OnPlayerKillCountUpdate { get; set; }
    public Action<int> OnAlivePlayerCountUpdate { get; set; }
    //public Action OnGetItemHealPack { get; set; }
    //public Action OnGetItemShieldPack { get; set; }
    //public Action OnGetItemMainAmmo { get; set; }
    //public Action OnGetItemSubAmmo { get; set; }
    public bool isOwner { get; private set; }
    public int killCnt = 0;
    public SessionId mySessionId;//{ get; set; }

    public bool isMove = false;
    private bool isLive = false;
    private Vector3 curMoveVector = Vector3.zero;

    Vector3 lookDir = Vector3.zero;
    //Vector3 hitParticlePos;
    //Quaternion hitParticleRot;

    Camera viewCamera;
    public PlayerController controller;
    public GunController guntroller;
    SkinnedMeshRenderer meshRenderer;
    public bool isDead { get; private set; }

    private void Start()
    {
        InitCommon();
    }
    void InitOwner()
    {
        detector.gameObject.SetActive(true);
        spotLight.enabled = true;
        pointLight.enabled = true;
        spotLight.range = sightDistance * 2;
        spotLight.spotAngle = sightAngle;
        detector.Init(sightDistance, sightAngle);

        controller = GetComponent<PlayerController>();
        guntroller = GetComponent<GunController>();

        controller.InitOwner();
        guntroller.InitOwner();

        //카메라 좌표, 각도 초기화
        viewCamera = Camera.main;
        viewCamera.transform.position = new Vector3(transform.position.x, 10, transform.position.z);
        viewCamera.transform.Rotate(90, 0, 0);

        meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        setRendererActive(true);
    }
    
    void InitCommon()
    {
        if(controller == null)
            controller = GetComponent<PlayerController>();
        if(guntroller == null)
            guntroller = GetComponent<GunController>();

        if(meshRenderer == null)
            meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        initBloodParticlePool();
    }

    public void SetAsOwner()
    {
        isOwner = true;
        InitOwner();
    }
    //조이스틱 축을 이용한 이동 벡터를 PlayerController에 전달하여 캐릭터 이동을 수행

    /*
     TODO (sj) : move 함수 처리 어떻게할지 서버쪽에서 체크해야함
     */


    //private void Move() // 두가지 Move로 변경 할 예정-> 호스트 : 인풋 매니저의 인풋대로 작동 / 비호스트 : 메세지에 의해서 Move(MoveVector) 작동
    //{
    //    Vector3 moveInput = Vector3.forward * InputManager.GetInstance().GetJoystickAxis("Vertical") +
    //        Vector3.right * InputManager.GetInstance().GetJoystickAxis("Horizontal");
    //    Vector3 moveVelocity = moveInput * moveSpeed;

    //    //조이스틱을 놓으면 moveInput이 zero벡터가 되도록 되어있으므로 캐릭터 rotation을 위한 현 방향 보관
    //    if (moveInput.x != 0 || moveInput.z != 0)
    //        lookDir = moveInput;

    //    controller.Move(moveVelocity);
    //    controller.LookAt(lookDir);
    //}


    public virtual void TakeHit(float damage, SessionId shooterId)
    {
        if(!isDead)
            TakeDamage(damage, shooterId); //실제 체력 감소 처리
    }
    public virtual void TakeDamage(float damage, SessionId shooterId)
    {
        float hpDamage = (currentShield - damage < 0) ? -(currentShield - damage) : 0;
        currentShield = (currentShield - damage < 0) ? 0 : currentShield - damage;
        currentHP = (currentHP - hpDamage < 0) ? 0 : currentHP - hpDamage;

        if (currentHP <= 0)
        {
            //Die();
            isDead = true;
            controller.SetDead();
            SoundManager.GetInstance().PlaySoundAtPoint(SoundEffect.Dead, transform.position);
            InGameManager.GetInstance().dieEvent(mySessionId, shooterId);
        }

        OnShieldChange(currentShield / maxShield);
        OnHPChange(currentHP / maxHP);
    }
    public virtual void PlayHitParticle(Vector3 hitPoint, Vector3 hitDirection)
    {
        //blood: 피격 파티클 (피 튐)
        SoundManager.GetInstance().PlaySoundAtPoint(SoundEffect.Hit, transform.position);
        var blood = PoolingManager.GetInstance().GetObjectPool<PoolingParticle>(bloodParticlePoolKey).GetObject();
        blood.ResetPos(hitPoint, hitDirection);
    }

    /*void Die()
    {
        isDead = true;
        controller.SetDead();
    }*/

    public void GetItem(ItemCategory catergory, int amount)
    {
        if (isDead)
            return;

        SoundManager.GetInstance().PlaySoundAtPoint(SoundEffect.ItemGet, transform.position);
        switch (catergory)
        {
            case ItemCategory.HealPack:
                currentHP = (currentHP + amount > maxHP) ? maxHP : currentHP + amount;
                OnHPChange(currentHP / maxHP);

                //OnGetItemHealPack();
                break;

            case ItemCategory.ShieldPack:
                currentShield = (currentShield + amount > maxShield) ? maxShield : currentShield + amount;
                OnShieldChange(currentShield / maxShield);

                //OnGetItemShieldPack();
                break;

            case ItemCategory.MainAmmo:
                guntroller.OnGetMainAmmo(amount);

                //OnGetItemMainAmmo();
                break;

            case ItemCategory.SubAmmo:
                guntroller.OnGetSubAmmo(amount);

                //OnGetItemSubAmmo();
                break;
        }
        return;
    }


    /*void Update()
    {
        if (isTest)
        {
            Move(); // Test Move
            return;
        }
    }*/




    //public void PrevMove()
    //{
    //    Move(curMoveVector);
    //}

    public void GetWeapon(ItemCategory catergory, int grade, int amount)
    {
        if (isDead)
            return;

        SoundManager.GetInstance().PlaySoundAtPoint(SoundEffect.ItemGet, transform.position);

        if (catergory == ItemCategory.Rifle)
            guntroller.GetWeaponItem(GunType.MainWeapon, grade, amount);

        else
            guntroller.GetWeaponItem(GunType.SubWeapon, grade, amount);
    }

    public void setRendererActive(bool active)
    {
        meshRenderer.enabled = active;
        OnPlayerVisiblityChange(active);
        if (active)
        {
            OnHPChange(currentHP / maxHP);
            OnShieldChange(currentShield / maxShield);
        }
    }
    /*
    public void Move(Vector3 moveVector) // 메세지로 처리 되는 비호스트 플레이어들
    {
        if(moveVector.x != 0 || moveVector.z != 0)
        {
            lookDir = moveVector;
        }
        Vector3 moveVelocity = moveVector * moveSpeed;

        controller.Move(moveVelocity);
        controller.LookAt(lookDir);
    }*/

    public void Move(Vector3 moveVector) // 메세지로 처리 되는 플레이어 오브젝트
    {
        if(!isLive)
        {
            return;
        }
        if(moveVector.x != 0 || moveVector.z != 0)
        {
            lookDir = moveVector;
        }
        Vector3 moveVelocity = moveVector * moveSpeed;

        controller.Move(moveVelocity);
        controller.LookAt(lookDir);
    }

   

    private void LateUpdate()
    {
        if(isOwner)
            CameraFollow();
    }

    //카메라 위치 조정
    void CameraFollow()
    {
        viewCamera.transform.position = new Vector3(transform.position.x, 10, transform.position.z);
    }

    private void initBloodParticlePool()
    {
        bloodParticlePoolKey = PoolingManager.GetInstance().GetPoolID("bloodParticlePool");
        PoolingManager.GetInstance().AddObjectPool<PoolingParticle>(
            bloodParticlePoolKey,
            _instantiate_func: () =>
            {
                var instance = Instantiate<PoolingParticle>(bloodParticlePrefab);
                instance.ParticlePoolKey = bloodParticlePoolKey;
                return instance;
            },

            _OnGetAction: (particle) =>
            {
                particle.gameObject.SetActive(true);
            },

            _OnReleaseAction: (particle) =>
            {
                particle.gameObject.SetActive(false);
            },

            _OnDestoryAction: (particle) =>
            {
                Destroy(particle.gameObject);
            },

            _OnCleanUpAction: (pool) =>
            {
                foreach (PoolingParticle p in pool)
                {
                    Destroy(p.gameObject);
                }
            },

            maxParticlePoolSize //maxSize
            );
    }

    public Vector3 GetPosition()
    {
        return transform.position;
    }

    public void SetPosition(Vector3 playerPos)
    {
        transform.position = playerPos;
    }

    public SessionId GetSessionId()
    {
        return mySessionId;
    }
}
