using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponGrenade : PlayerWeapon
{
    //private Transform weaponTransform = null;
    private Camera playerCamera = null;
    [SerializeField] private GameObject grenadePrefab = null;
    [SerializeField] private GameObject secondaryPrefab = null;
    [SerializeField] private float grenadeThrowForce;
    [SerializeField] private float forceYMultiplier;
    [SerializeField] private float fuseTime;
    private float fuseTimer = 0f;

    LineRenderer lr = null;
    int lineIterations = 20;

    private void Awake()
    {
        InvokeRepeating(nameof(CheckFuse), 0f, 0.1f);
        fuseTime = grenadePrefab.GetComponent<Grenade>().FuseTime;
        lr = GetComponentInChildren<LineRenderer>();
        lr.positionCount = lineIterations;
        lr.enabled = false;
    }

    public override void FireWeaponThrow()
    {
        base.FireWeaponThrow();

        if (throwForce <= 0f)
            return;

        if (playerCamera == null)
            playerCamera = Camera.main;

        lr.enabled = true;

        Vector3 position = transform.position;
        Vector3 velocity = new Vector3(playerCamera.transform.forward.x * throwForce * grenadeThrowForce,
                playerCamera.transform.forward.y * throwForce * grenadeThrowForce * forceYMultiplier,
                playerCamera.transform.forward.z * throwForce * grenadeThrowForce);
        float timeDelta = 1f / velocity.magnitude;

        for (int i = 0; i < lineIterations; i++)
        {
            lr.SetPosition(i, position);
            position += velocity * timeDelta + 0.5f * Physics.gravity * Mathf.Pow(timeDelta, 2);
            velocity += Physics.gravity * timeDelta;
        }
    }

    public override void FireWeapon()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        lr.enabled = false;
        Vector3 forward = playerCamera.transform.forward;
        Vector3 grenadePos = playerCamera.transform.position + forward;
        GameObject grenade = Instantiate(grenadePrefab, grenadePos, Quaternion.identity);
        Rigidbody rb = grenade.GetComponent<Rigidbody>();
        rb.velocity = new Vector3(forward.x * throwForce * grenadeThrowForce, forward.y * throwForce * grenadeThrowForce * forceYMultiplier, forward.z * throwForce * grenadeThrowForce);

        Grenade g = grenade.GetComponent<Grenade>();
        if (g != null)
        {
            g.Owner = base.Player;
            g.FuseTimer = fuseTimer;
        }

        fuseTimer = 0;
        base.FireWeapon();
    }

    public override void FireSecondary()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        lr.enabled = false;
        Vector3 forward = playerCamera.transform.forward;
        Vector3 grenadePos = playerCamera.transform.position + forward;
        GameObject grenade = Instantiate(secondaryPrefab, grenadePos, Quaternion.identity);
        Rigidbody rb = grenade.GetComponent<Rigidbody>();
        rb.velocity = new Vector3(forward.x * throwForce * grenadeThrowForce, forward.y * throwForce * grenadeThrowForce * forceYMultiplier, forward.z * throwForce * grenadeThrowForce);

        Grenade g = grenade.GetComponent<Grenade>();
        if (g != null)
        {
            g.Owner = base.Player;
            g.FuseTimer = fuseTimer;
        }
            
        fuseTimer = 0;
        base.FireSecondary();
    }

    void CheckFuse()
    {
        if (fuseTimer > fuseTime)
        {
            FireWeapon();
            fuseTimer = 0f;
            return;
        }
            
        if (!IsFiringAny())
            return;

        fuseTimer += 0.1f;
    }
}
