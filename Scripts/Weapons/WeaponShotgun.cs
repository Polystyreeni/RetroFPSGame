using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HitScanWeapon;

public class WeaponShotgun : PlayerWeapon
{
    private Transform playerCamera = null;

    [SerializeField] private float xRecoil, yRecoil, zRecoil;
    [SerializeField] private int shotCount = 8;
    [SerializeField] private LayerMask whatIsWall;

    private HitScanWeaponData weaponData;

    private void OnEnable()
    {
        weaponData = new HitScanWeaponData();
        weaponData.maxDamage = maxDamage;
        weaponData.minDamage = minDamage;
        weaponData.range = range;
        weaponData.whatIsWall = whatIsWall;
        weaponData.knockback = true;
        weaponData.explosionForce = false;
    }

    public override void FireWeapon()
    {
        base.FireWeapon();

        if (playerCamera == null)
            playerCamera = Camera.main.transform.parent;

        for(int i = 0; i < shotCount; i++)
            HitScanAttack(true);
    }

    public override void FireSecondary()
    {
        // Normal fire mode, if only one barrel is full
        if (ammoClip <= 1)
        {
            FireWeapon();
            return;
        }
            
        fireCounter += ammoCost * 2;
        ammoClip -= ammoCost * 2;

        if (playerCamera == null)
            playerCamera = Camera.main.transform.parent;

        int animIndex = 2;
        weaponAnimator.StartAnimation(animIndex);
        bCanShoot = false;
        CanSwitchWeapon = false;

        aSource.PlayOneShot(sndWeaponFire);

        Invoke(nameof(base.EnableShooting), fireTime);
        Invoke(nameof(base.EnableWeaponSwitching), weaponAnimator.GetAnimLenght(animIndex));

        UpdateAmmoHud();

        for(int i = 0; i < shotCount * 2; i++)
            HitScanAttack(true);
    }

    void HitScanAttack(bool useRecoil = false)
    {
        weaponData.startPosition = playerCamera.position;
        Vector3 offset = Vector3.zero;
        if (useRecoil)
            offset = new Vector3(Random.Range(-xRecoil, xRecoil), Random.Range(-yRecoil, yRecoil), Random.Range(-zRecoil, zRecoil));

        weaponData.direction = playerCamera.forward + offset;
        weaponData.attacker = base.Player;
        WeaponHitScan.FireWeapon(weaponData);
    }
}
