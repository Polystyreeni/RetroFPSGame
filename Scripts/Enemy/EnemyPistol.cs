using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HitScanWeapon;
public class EnemyPistol : MonoBehaviour
{
    [SerializeField] private Enemy enemyData = null;

    private EnemyMovement enemyMov = null;

    private LayerMask whatIsWall;
    private float shootValue = 1f;

    private float accuratyRange = 0f;
    private HitScanWeaponData weaponData = new HitScanWeaponData();

    void Start()
    {
        enemyMov = GetComponentInParent<EnemyMovement>();
        if(enemyData != null)
        {
            whatIsWall = enemyMov.WhatIsWall;
            shootValue = 1f / enemyData.GetShootRange();
            accuratyRange = enemyData.GetAccuratyRange();
        }

        weaponData.attacker = transform.parent;
        weaponData.minDamage = 1;
        weaponData.range = enemyData.GetEnemySeeDistance();
        weaponData.whatIsWall = whatIsWall;
    }
    public void EnemyShoot()
    {
        Transform target = enemyMov.Target;
        if (target == null)
            return;

        float speedOffset = 0;
        Rigidbody rb = target.GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            speedOffset = rb.velocity.sqrMagnitude * accuratyRange * 0.025f;
            Debug.Log("Fire Offset: " + speedOffset);
        }
            
        Vector3 offset = new Vector3(Random.Range(0, accuratyRange), Random.Range(0, accuratyRange), Random.Range(0, accuratyRange));
        offset += new Vector3(Random.Range(-speedOffset, speedOffset), 0, Random.Range(-speedOffset, speedOffset));

        weaponData.startPosition = transform.position;
        weaponData.direction = target.transform.position - transform.position + offset;
        weaponData.maxDamage = CalculateDamage(target);

        WeaponHitScan.FireWeapon(weaponData);
    }

    int CalculateDamage(Transform target)
    {
        //float distSq = Vector3.Distance(target.position, transform.position);
        //int damage = (int)(enemyData.GetDamage() * Mathf.Abs(Mathf.Cos(shootValue * distSq)));

        int damage = enemyData.GetDamage();

        if(GameManager.Instance.DifficultyLevel < EnumContainer.DIFFICULTY.NORMAL)
            damage = (int)(damage * 0.6f);

        else if(GameManager.Instance.DifficultyLevel == EnumContainer.DIFFICULTY.NORMAL)
            damage = (int)(damage * 0.8f);

        return damage;
    }
}
