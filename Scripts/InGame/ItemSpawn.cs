using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

enum ItemSpawnType { Random, HealPackOnly, ShieldPackOnly, MainAmmoOnly, SubAmmoOnly,  Rifle_0_Only,
    Pistol_1_Only, Rifle_1_Only, Pistol_2_Only, Rifle_2_Only, Pistol_3_Only, Rifle_3_Only }

public class ItemSpawn : MonoBehaviour
{
    [SerializeField] float spawnCooltime = 3;
    [SerializeField] Transform spawnTransform;
    [SerializeField] ItemSpawnType spawnType;

    [SerializeField] Item healPackPrefab;
    [SerializeField] Item shieldPackPrefab;
    [SerializeField] Item mainAmmoPrefab;
    [SerializeField] Item subAmmoPrefab;
    [SerializeField] Item pistolGrade1Prefab;
    [SerializeField] Item pistolGrade2Prefab;
    [SerializeField] Item pistolGrade3Prefab;
    [SerializeField] Item rifleGrade0Prefab;
    [SerializeField] Item rifleGrade1Prefab;
    [SerializeField] Item rifleGrade2Prefab;
    [SerializeField] Item rifleGrade3Prefab;

    private void Start()
    {
        StartSpawn();
    }


    void StartSpawn()
    {
        switch (spawnType)
        {
            case ItemSpawnType.Random:
                ItemCategory randomCategory = (ItemCategory)UnityEngine.Random.Range(0, Enum.GetValues(typeof(ItemCategory)).Length);
                if (randomCategory == ItemCategory.Pistol || randomCategory == ItemCategory.Rifle)
                    SpawnWeapon(randomCategory, (int)UnityEngine.Random.Range(1, 4));
                else
                    SpawnItem(randomCategory);
                break;
            case ItemSpawnType.HealPackOnly:
                SpawnItem(ItemCategory.HealPack);
                break;
            case ItemSpawnType.ShieldPackOnly:
                SpawnItem(ItemCategory.ShieldPack);
                break;
            case ItemSpawnType.MainAmmoOnly:
                SpawnItem(ItemCategory.MainAmmo);
                break;
            case ItemSpawnType.SubAmmoOnly:
                SpawnItem(ItemCategory.SubAmmo);
                break;
            case ItemSpawnType.Pistol_1_Only:
                SpawnWeapon(ItemCategory.Pistol, 1);
                break;
            case ItemSpawnType.Pistol_2_Only:
                SpawnWeapon(ItemCategory.Pistol, 2);
                break;
            case ItemSpawnType.Pistol_3_Only:
                SpawnWeapon(ItemCategory.Pistol, 3);
                break;
            case ItemSpawnType.Rifle_0_Only:
                SpawnWeapon(ItemCategory.Rifle, 0);
                break;
            case ItemSpawnType.Rifle_1_Only:
                SpawnWeapon(ItemCategory.Rifle, 1);
                break;
            case ItemSpawnType.Rifle_2_Only:
                SpawnWeapon(ItemCategory.Rifle, 2);
                break;
            case ItemSpawnType.Rifle_3_Only:
                SpawnWeapon(ItemCategory.Rifle, 3);
                break;
        }
    return;
    }

    void SpawnItem(ItemCategory itemCatergory)
    {
        Item item = null;
        switch (itemCatergory)
        {
            case ItemCategory.HealPack:
                item = Instantiate<Item>(healPackPrefab, spawnTransform);
                break;
            case ItemCategory.ShieldPack:
                item = Instantiate<Item>(shieldPackPrefab, spawnTransform);
                break;
            case ItemCategory.MainAmmo:
                item = Instantiate<Item>(mainAmmoPrefab, spawnTransform);
                break;
            case ItemCategory.SubAmmo:
                item = Instantiate<Item>(subAmmoPrefab, spawnTransform);
                break;
        }

        item.OnItemDisable += RespawnItem;
    }

    void SpawnWeapon(ItemCategory itemCatergory, int grade)
    {
        Item item = null;

        if(itemCatergory == ItemCategory.Pistol)
        {
            switch(grade)
            {
                case 1:
                    item = Instantiate<Item>(pistolGrade1Prefab, spawnTransform);
                    break;

                case 2:
                    item = Instantiate<Item>(pistolGrade2Prefab, spawnTransform);
                    break;

                case 3:
                    item = Instantiate<Item>(pistolGrade3Prefab, spawnTransform);
                    break;
            }
        }
        else if(itemCatergory == ItemCategory.Rifle)
        {
            switch (grade)
            {
                case 0:
                    item = Instantiate<Item>(rifleGrade0Prefab, spawnTransform);
                    break;
                case 1:
                    item = Instantiate<Item>(rifleGrade1Prefab, spawnTransform);
                    break;

                case 2:
                    item = Instantiate<Item>(rifleGrade2Prefab, spawnTransform);
                    break;

                case 3:
                    item = Instantiate<Item>(rifleGrade3Prefab, spawnTransform);
                    break;
            }
        }

        item.OnItemDisable += RespawnItem;
    }

    public void RespawnItem()
    {
        StartCoroutine("ItemSpawnCooltime");
    }

    IEnumerator ItemSpawnCooltime()
    {
        yield return new WaitForSeconds(spawnCooltime);
        StartSpawn();
    }
    
}