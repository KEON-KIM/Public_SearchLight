using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BackEnd;
using Protocol;
using BackEnd.Tcp;
using System;

public enum ItemCategory { HealPack, ShieldPack, MainAmmo, SubAmmo, Pistol, Rifle };

public class Item : MonoBehaviour
{
    [SerializeField] ItemCategory catergory;
    [SerializeField] int grade;
    [SerializeField] int effectAmount;

    public Action OnItemDisable { get; set; }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag != "Player")
        {
            return;
        }
        var player = other.GetComponent<Player>();
        SessionId session = player.GetSessionId();

        OnItemDisable();
        Destroy(this.gameObject);

        if (ServerMatchManager.GetInstance().IsHost() == false)
        {
            return;
        }

        if (player != null)
        {
            Vector3 newPos = new Vector3(other.transform.position.x, 0f, other.transform.position.z);
            Protocol.PlayerAcquireMessage message =
                        new Protocol.PlayerAcquireMessage(session, catergory, grade, effectAmount, player.transform.position);
            ServerMatchManager.GetInstance().SendDataToInGame<Protocol.PlayerAcquireMessage>(message);
            /*switch (catergory)
            {
                case ItemCategory.HealPack:
                    player.GetItem(ItemCategory.HealPack, effectAmount);
                    break;
                case ItemCategory.ShieldPack:
                    player.GetItem(ItemCategory.ShieldPack, effectAmount);
                    break;
                case ItemCategory.MainAmmo:
                    player.GetItem(ItemCategory.MainAmmo, effectAmount);
                    break;
                case ItemCategory.SubAmmo:
                    player.GetItem(ItemCategory.SubAmmo, effectAmount);
                    break;
                case ItemCategory.Pistol:
                    player.GetWeapon(ItemCategory.Pistol, grade, effectAmount);
                    break;
                case ItemCategory.Rifle:
                    player.GetWeapon(ItemCategory.Rifle, grade, effectAmount);
                    break;
            }*/
        }
    }

    void rotateItem()
    {
        if(catergory == ItemCategory.Pistol || catergory == ItemCategory.Rifle)
        {
            gameObject.transform.Rotate(new Vector3(0, 1, 0));
        }
    }

    private void Update()
    {
        rotateItem();
    }
}
