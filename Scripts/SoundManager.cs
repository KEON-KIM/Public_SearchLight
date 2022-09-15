using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SoundEffect { Reload, Fire1, Fire2, ItemGet, Dead, Hit }

public class SoundManager : MonoBehaviour
{
    private static SoundManager instance = null;

    [SerializeField] AudioClip ReloadSound;
    [SerializeField] AudioClip FireSound1;
    [SerializeField] AudioClip FireSound2;
    [SerializeField] AudioClip ItemGetSound;
    [SerializeField] AudioClip DeadSound;
    [SerializeField] AudioClip HitSound;
    //AudioSource audioSource;

    public static SoundManager GetInstance()
    {
        if (instance == null)
        {
            Debug.LogError("SoundManager instance does not exist.");
            return null;
        }
        return instance;
    }

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(instance);
        }

        instance = this;
        DontDestroyOnLoad(this.gameObject);
        //audioSource = GetComponent<AudioSource>();
    }

    


    public void PlaySoundAtPoint(SoundEffect soundType, Vector3 pos)
    {
        switch (soundType)
        {
            case SoundEffect.Reload:
                AudioSource.PlayClipAtPoint(ReloadSound, pos);
                break;
            case SoundEffect.Fire1:
                AudioSource.PlayClipAtPoint(FireSound1, pos);
                break;
            case SoundEffect.Fire2:
                AudioSource.PlayClipAtPoint(FireSound2, pos, 0.65f);
                break;
            case SoundEffect.ItemGet:
                AudioSource.PlayClipAtPoint(ItemGetSound, pos);
                break;
            case SoundEffect.Dead:
                AudioSource.PlayClipAtPoint(DeadSound, pos);
                break;
            case SoundEffect.Hit:
                AudioSource.PlayClipAtPoint(HitSound, pos);
                break;
        }
    }

}
