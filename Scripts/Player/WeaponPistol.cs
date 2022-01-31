using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HitScanWeapon;

public class WeaponPistol : PlayerWeapon
{
    private Transform playerCamera = null;

    [SerializeField] private float xRecoil, yRecoil, zRecoil;
    [SerializeField] private int secondaryFireDecrease = 4;
    [SerializeField] private float headShotMultiplier = 4f;
    [SerializeField] private LayerMask whatIsWall;

    HitScanWeaponData weaponData;

    private void OnEnable()
    {
        weaponData = new HitScanWeaponData();
        weaponData.maxDamage = maxDamage;
        weaponData.minDamage = minDamage;
        weaponData.range = range;
        weaponData.headShotMultiplier = headShotMultiplier;
        weaponData.whatIsWall = whatIsWall;
        weaponData.knockback = true;
        weaponData.explosionForce = false;
    }

    public override void FireWeapon()
    {
        base.FireWeapon();

        if (playerCamera == null)
            playerCamera = Camera.main.transform.parent;

        HitScanAttack();
    }

    public override void FireSecondary()
    {
        fireCounter += ammoCost;
        ammoClip -= ammoCost;

        if (playerCamera == null)
            playerCamera = Camera.main.transform.parent;

        int animIndex = 2;
        weaponAnimator.StartAnimation(animIndex);
        bCanShoot = false;
        CanSwitchWeapon = false;

        aSource.PlayOneShot(sndWeaponFire);

        Invoke(nameof(base.EnableShooting), fireTime / secondaryFireDecrease);
        Invoke(nameof(base.EnableWeaponSwitching), weaponAnimator.GetAnimLenght(animIndex));

        UpdateAmmoHud();

        HitScanAttack(true);
    }

    void HitScanAttack(bool useRecoil = false)
    {
        weaponData.startPosition = playerCamera.position;
        Vector3 offset = Vector3.zero;
        if(useRecoil)
            offset = new Vector3(Random.Range(-xRecoil, xRecoil), Random.Range(-yRecoil, yRecoil), Random.Range(-zRecoil, zRecoil));

        weaponData.direction = playerCamera.forward + offset;
        weaponData.attacker = base.Player;

        WeaponHitScan.FireWeapon(weaponData);
    }
}
