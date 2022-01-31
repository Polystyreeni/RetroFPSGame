using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponFlamethrower : PlayerWeapon
{
    private Transform playerCamera = null;

    [SerializeField] private LayerMask whatIsWall;
    [SerializeField] private string fireModel = "fire_flamethrower";

    public override void FireWeapon()
    {
        base.FireWeapon();

        if (playerCamera == null)
            playerCamera = Camera.main.transform.parent;

        float velocity = range;
        float lifeTime = 1.3f;

        RaycastHit hit;
        if(Physics.Raycast(playerCamera.position, playerCamera.forward, out hit, range, whatIsWall))
        {
            Vector3 endPos = hit.point;
            velocity = Vector3.Distance(endPos, playerCamera.position) / lifeTime;
        }

        // Spawn object from explosion pool
        GameObject obj = ExplosionManager.Instance.CreateFire(fireModel, playerCamera.position, 0.3f);
        if (obj == null)
            return;

        if(obj.TryGetComponent<Fire>(out Fire fire))
            fire.SetOwner(base.Player);

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.velocity = playerCamera.forward * velocity;
    }

    public override void FireSecondary()
    {
        FireWeapon();
    }
}
