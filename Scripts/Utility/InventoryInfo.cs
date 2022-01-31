using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryInfo : MonoBehaviour
{
    public static InventoryInfo Instance = null;
    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        else if(Instance != this)
        {
            Destroy(this);
        }

        // TODO: Any better way to do this
        weaponList.Add(Resources.Load("Weapon/GunKnife") as GameObject);        // Index 0
        weaponList.Add(Resources.Load("Weapon/GunPistol") as GameObject);       // Index 1
        weaponList.Add(Resources.Load("Weapon/GunShotgun") as GameObject);      // Index 2
        weaponList.Add(Resources.Load("Weapon/GunSmgGerman") as GameObject);    // Index 3

        // Add other weapons here!
        weaponList.Add(Resources.Load("Weapon/GunFlamethrower") as GameObject);    // Index 5

        weaponList.Add(Resources.Load("Weapon/GunGrenade") as GameObject);      // Index 6
    }

    private List<GameObject> weaponList = new List<GameObject>();

    public GameObject GetWeaponByIndex(int index)
    {
        return weaponList[index - 1];
    }
}
