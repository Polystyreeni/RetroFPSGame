using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HitScanWeapon
{
    public static class WeaponHitScan
    {
        public static void FireWeapon(HitScanWeaponData shootData)
        {
            RaycastHit hit;
            if (Physics.Raycast(shootData.startPosition, shootData.direction, out hit, shootData.range, shootData.whatIsWall))
            {
                float distance = Vector3.Distance(shootData.startPosition, hit.point);
                int finalDamage = (int)(shootData.maxDamage * (-distance / shootData.range + 1));    // (int)(shootData.maxDamage * Mathf.Cos((Mathf.PI * distance) / (2 * shootData.range))

                if (finalDamage < shootData.minDamage)
                    finalDamage = shootData.minDamage;

                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Actor"))
                {
                    EnemyMovement movement;
                    if (hit.collider.gameObject.name == "Head")
                        movement = hit.collider.gameObject.GetComponentInParent<EnemyMovement>();
                    else
                        movement = hit.collider.gameObject.GetComponent<EnemyMovement>();

                    if (movement != null)
                    {
                        movement.TakeDamage(hit.point, hit.collider, finalDamage, shootData.attacker, shootData.knockback, shootData.explosionForce, shootData.headShotMultiplier, EnumContainer.DAMAGETYPE.Bullet);
                    }
                }

                else if(hit.collider.gameObject.layer == LayerMask.NameToLayer("Player"))
                {
                    PlayerContainer playerContainer = hit.collider.gameObject.GetComponentInParent<PlayerContainer>();
                    playerContainer.TakeDamage(finalDamage, shootData.attacker, false, EnumContainer.DAMAGETYPE.Bullet);
                }

                else if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Explosive"))
                {
                    if (hit.collider.gameObject.TryGetComponent<Grenade>(out Grenade g))
                        g.Explode();
                }

                else if (hit.collider.gameObject.GetComponent<Destructible>() != null)
                {
                    hit.collider.gameObject.GetComponent<Destructible>().ObjectTakeDamage(shootData.attacker, finalDamage, EnumContainer.DAMAGETYPE.Bullet);
                    FxManager.Instance.PlayFX("fx_impact_bullet_sparks", hit.point, Quaternion.identity);
                }

                else
                {
                    // TODO: Change to impact particle fx
                    Quaternion rotation = Quaternion.LookRotation(hit.normal, Vector3.up);
                    Vector3 pos = hit.point + hit.normal * 0.01f;

                    if(hit.collider.gameObject.layer == LayerMask.NameToLayer("Dynamic"))
                        FxManager.Instance.PlayFX("fx_impact_bullet_sparks", pos, rotation); 

                    else
                        FxManager.Instance.PlayFX("fx_impact_bullet", pos, rotation);
                }
            }
        }
    }

    [System.Serializable]
    public struct HitScanWeaponData
    {
        public Vector3 startPosition;
        public Vector3 direction;
        public Transform attacker;
        public LayerMask whatIsWall;
        public int maxDamage;
        public int minDamage;
        public float headShotMultiplier;
        public float range;
        public bool knockback;
        public bool explosionForce;
    }
}
