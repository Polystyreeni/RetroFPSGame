using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HitScanWeapon;

public class WeaponSmg : PlayerWeapon
{
    private Transform playerCamera = null;

    [SerializeField] private float spread = 0f;
    [SerializeField] private GameObject secondaryGun = null;
    [SerializeField] private LayerMask whatIsWall;

    private HitScanWeaponData weaponData;

    void Awake()
    {
        secondaryGun.SetActive(false);

        weaponData = new HitScanWeaponData();
        weaponData.maxDamage = maxDamage;
        weaponData.minDamage = minDamage;
        weaponData.range = range;
        weaponData.whatIsWall = whatIsWall;
        weaponData.knockback = false;
        weaponData.explosionForce = false;
    }

    public override void FireWeapon()
    {
        if (playerCamera == null)
            playerCamera = Camera.main.transform.parent;

        base.FireWeapon();
        HitScanAttack();
    }

    public override void FireSecondary()
    {
        if (playerCamera == null)
            playerCamera = Camera.main.transform.parent;

        fireCounter += (ammoCost * 2);
        ammoClip -= ammoCost;

        // If stock ammo is empty, use twice the ammo each shot
        if (ammoStock <= 0)
            ammoClip -= ammoCost;

        weaponAnimator.StartAnimation(2);
        bCanShoot = false;
        CanSwitchWeapon = false;
        Invoke(nameof(base.EnableShooting), fireTime);
        Invoke(nameof(HideSecondaryWeapon), fireTime);
        Invoke(nameof(base.EnableWeaponSwitching), weaponAnimator.GetAnimLenght(2));

        secondaryGun.SetActive(true);

        WeaponAnimator secAnimator = secondaryGun.GetComponent<WeaponAnimator>();
        if(secAnimator != null)
            secAnimator.StartAnimation(2);

        UpdateAmmoHud();
        aSource.PlayOneShot(sndWeaponFire);

        for(int i = 0; i < 2; i++)
        {
            HitScanAttack();
        }
    }

    void HitScanAttack()
    {
        weaponData.startPosition = playerCamera.position;
        weaponData.direction = playerCamera.forward + new Vector3(Random.Range(0, spread), Random.Range(0, spread), Random.Range(0, spread));
        weaponData.attacker = base.Player;
        WeaponHitScan.FireWeapon(weaponData);
    }

    void HideSecondaryWeapon()
    {
        secondaryGun.SetActive(false);
    }
}
