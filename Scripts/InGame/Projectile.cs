using System.Collections;
using System.Collections.Generic;
using BackEnd;
using BackEnd.Tcp;
using Protocol;
using UnityEngine;
using System;

public class Projectile : MonoBehaviour
{
    [SerializeField] float muzzleVelocity;
    [SerializeField] LayerMask collisionMask;

    Vector3 originPos;
    Quaternion originRot;
    Action<Projectile> releaseAction;

    float meshLength = 0.15f;

    public Action<Projectile> ReleaseAction { get { return releaseAction; } set { releaseAction = value; } }

    public float damage { get; set; }
    public string bulletPoolKey { get; set; }
    public string hitParticlePoolKey { get; set; }
    public BackEnd.Tcp.SessionId shooterSessionID { get; set; }


    public void ResetPos(Vector3 pos, Vector3 target)
    {
        transform.position = pos;
        transform.LookAt(target);
    }

    private void Update()
    {
        float moveDistance = muzzleVelocity * Time.deltaTime;
        CheckCollisions(moveDistance);
        gameObject.transform.Translate(transform.forward * moveDistance, Space.World);
        Debug.DrawRay(gameObject.transform.position, transform.forward * moveDistance, Color.red);
    }

    private void CheckCollisions(float moveDistance)
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, moveDistance + meshLength, collisionMask, QueryTriggerInteraction.Collide))
        {
            OnHitObject(hit.collider, hit.point);
        }
    }

    void OnHitObject(Collider other, Vector3 hitPoint)
    {
        IDamageable damageableObject = other.GetComponent<IDamageable>();
        if (damageableObject != null)
        {
            SessionId session = other.GetComponent<Player>().GetSessionId();
            damageableObject.PlayHitParticle(hitPoint, transform.forward);
            
            if (ServerMatchManager.GetInstance().IsHost() == false)
            {
                return;
            }
            Vector3 newPos = new Vector3(other.transform.position.x, 0f, other.transform.position.z);
            Protocol.PlayerDamegedMessage message =
                        new Protocol.PlayerDamegedMessage(session, shooterSessionID, newPos, damage);
            ServerMatchManager.GetInstance().SendDataToInGame<Protocol.PlayerDamegedMessage>(message);
            //damageableObject.TakeHit(damage, shooterSessionID); //메세지 처리
            //메세지 X, 클라에서 처리
        }
        
        var hitParticle = PoolingManager.GetInstance().GetObjectPool<PoolingParticle>(hitParticlePoolKey).GetObject();
        hitParticle.ResetPos(hitPoint, -transform.forward);

        var bulletPool = PoolingManager.GetInstance().GetObjectPool<Projectile>(bulletPoolKey);
        if (bulletPool == null)
            Destroy(gameObject);
        bulletPool.ReleaseObject(this);
        //ReleaseAction(this);
    }
    

}
