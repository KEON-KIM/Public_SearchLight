using UnityEngine;

public interface IDamageable
{
    void TakeHit(float damage, BackEnd.Tcp.SessionId shooterId);
    void TakeDamage(float damage, BackEnd.Tcp.SessionId shooterId);
    void PlayHitParticle(Vector3 hitPoint, Vector3 hitDirection);
}