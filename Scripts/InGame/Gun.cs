using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum GunType { SubWeapon, MainWeapon}

public class Gun : MonoBehaviour
{
    [SerializeField] Projectile bulletPrefabYellow;
    [SerializeField] Projectile bulletPrefabGreen;
    [SerializeField] Projectile bulletPrefabBlue;
    [SerializeField] Projectile bulletPrefabRed;
    [SerializeField] Transform firePos;
    [SerializeField] ParticleSystem muzzleFlashYellowPrefab;
    [SerializeField] ParticleSystem muzzleFlashGreenPrefab;
    [SerializeField] ParticleSystem muzzleFlashBluePrefab;
    [SerializeField] ParticleSystem muzzleFlashRedPrefab;
    [SerializeField] PoolingParticle hitParticlePrefab;

    [SerializeField] float shotCooltime;
    [SerializeField] int maxPoolSize;
    [SerializeField] int magazineSize;
    [SerializeField] float damage;
    [SerializeField] GunType gunType;
    [SerializeField] int damageIncrease;


    public float ShotCooltime { get { return shotCooltime; } private set { } }
    public int MagazineSize { get { return magazineSize; } private set {} }
    public GunType GetGunType { get { return gunType; } private set { } }
    public BackEnd.Tcp.SessionId playerSessionID { get; set; }

    public int grade { get; set; }
    public int inMagazine { get; set; }

    Vector3 currTargetPos;
    ParticleSystem muzzleFlashParticle;
    MeshRenderer meshRenderer;
    Projectile currBulletPrefab;
    string bulletPoolKey;
    string hitParticlePoolKey;
    MeshRenderer[] subRenderers;


    private void Awake()
    {
        InitCommon();
    }

    void InitCommon()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        subRenderers = GetComponentsInChildren<MeshRenderer>();

        currBulletPrefab = bulletPrefabYellow;
        initBulletParticlePool();
        InitBulletPool();
        inMagazine = magazineSize;
        muzzleFlashParticle = Instantiate<ParticleSystem>(muzzleFlashYellowPrefab, firePos);
        muzzleFlashParticle.Stop();
    }

    private void InitBulletPool()
    {
        bulletPoolKey = PoolingManager.GetInstance().GetPoolID("bulletPool");
        PoolingManager.GetInstance().AddObjectPool<Projectile>(
            bulletPoolKey,
            _instantiate_func: () =>
            {
                var instance = Instantiate<Projectile>(currBulletPrefab, firePos.position, firePos.rotation);
                instance.transform.LookAt(currTargetPos);
                instance.bulletPoolKey = this.bulletPoolKey;
                instance.hitParticlePoolKey = hitParticlePoolKey;
                instance.damage = damage;
                if (playerSessionID == BackEnd.Tcp.SessionId.None)
                    Debug.LogError("PlayerSessionID was not Initialized (Gun)");
                instance.shooterSessionID = playerSessionID;
                return instance;
            },

            _OnGetAction: (bullet) =>
            {
                bullet.gameObject.SetActive(true);
                bullet.ResetPos(firePos.position, currTargetPos);
            },

            _OnReleaseAction: (bullet) =>
            {
                bullet.gameObject.SetActive(false);
            },

            _OnDestoryAction: (bullet) =>
            {
                Destroy(bullet.gameObject);
            },

            _OnCleanUpAction: (pool) =>
            {
                foreach (Projectile p in pool)
                    Destroy(p.gameObject);
            },

            maxPoolSize //maxSize
            );
    }

    private void initBulletParticlePool()
    {
        hitParticlePoolKey = PoolingManager.GetInstance().GetPoolID("hitParticlePool");
        PoolingManager.GetInstance().AddObjectPool<PoolingParticle>(
            hitParticlePoolKey,
            _instantiate_func: () =>
            {
                var instance = Instantiate<PoolingParticle>(hitParticlePrefab);
                instance.ParticlePoolKey = hitParticlePoolKey;
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

            maxPoolSize //maxSize
            );
    }

    public void GradeUpdate(int newGrade)
    {
        if (grade < newGrade)
        {
            grade = newGrade;
            damage += newGrade * damageIncrease;

            switch(newGrade)
            {
                case 1:
                    currBulletPrefab = bulletPrefabGreen;
                    Destroy(muzzleFlashParticle.gameObject);
                    muzzleFlashParticle = Instantiate<ParticleSystem>(muzzleFlashGreenPrefab, firePos);
                    muzzleFlashParticle.Stop();
                    break;
                case 2:
                    currBulletPrefab = bulletPrefabBlue;
                    Destroy(muzzleFlashParticle.gameObject);
                    muzzleFlashParticle = Instantiate<ParticleSystem>(muzzleFlashBluePrefab, firePos);
                    muzzleFlashParticle.Stop();
                    break;
                case 3:
                    currBulletPrefab = bulletPrefabRed;
                    Destroy(muzzleFlashParticle.gameObject);
                    muzzleFlashParticle = Instantiate<ParticleSystem>(muzzleFlashRedPrefab, firePos);
                    muzzleFlashParticle.Stop();
                    break;
            }

            var pool = PoolingManager.GetInstance().GetObjectPool<Projectile>(bulletPoolKey);
            if (pool == null)
            {
                Debug.LogError("Can't CleanUp the pool because It's not exist");
                return;
            }

            pool.CleanUp();
            PoolingManager.GetInstance().RemoveObjectPool<Projectile>(bulletPoolKey);

            InitBulletPool();

        }
    }
    

    public void Fire(Vector3 targetPos)
    {
        if (inMagazine > 0)
        {
            muzzleFlashParticle.Play(true);
            currTargetPos = targetPos;
            PoolingManager.GetInstance().GetObjectPool<Projectile>(bulletPoolKey).GetObject();
            if (gunType == GunType.MainWeapon)
                SoundManager.GetInstance().PlaySoundAtPoint(SoundEffect.Fire1, firePos.position);
            else
                SoundManager.GetInstance().PlaySoundAtPoint(SoundEffect.Fire2, firePos.position);
            --inMagazine; //발사시 탄창 장탄수 감소
        }
    }


    public void SetVisiblity(bool flag)
    {
        meshRenderer.enabled = flag;
        foreach(var rend in subRenderers)
        {
            if (rend != null)
                rend.enabled = flag;
        }
    }

}
