using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolingParticle : MonoBehaviour
{
    public string ParticlePoolKey { get; set; }
    ParticleSystem particle;

    private void Awake()
    {
        particle = GetComponent<ParticleSystem>();
    }

    public void ResetPos(Vector3 pos, Vector3 new_forward)
    {
        transform.position = pos;
        transform.forward = new_forward;
    }

    private void OnParticleSystemStopped()
    {
        PoolingManager.GetInstance().GetObjectPool<PoolingParticle>(ParticlePoolKey).ReleaseObject(this);
    }
}
