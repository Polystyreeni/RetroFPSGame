using UnityEngine;
using HitScanWeapon;
public class EnemyZombie : MonoBehaviour
{
    [SerializeField] private Enemy enemyData = null;
    [SerializeField] private int moveSpeedMultiplier = 3;
    [SerializeField] private AudioClip sndHit = null;
    [SerializeField] private LayerMask whatIsWall;

    private EnemyMovement enemyMov = null;
    private AudioSource aSource = null;
    private HitScanWeaponData weaponData = new HitScanWeaponData();

    void Start()
    {
        enemyMov = GetComponentInParent<EnemyMovement>();
        enemyMov.OnEnemyDamage += ChangeRunCycle;
        if (enemyData != null)
        {
            whatIsWall = enemyMov.WhatIsWall; 
        }

        aSource = GetComponentInParent<AudioSource>();

        weaponData.attacker = transform.parent;
        weaponData.minDamage = enemyData.GetDamage();
        weaponData.range = enemyData.GetShootRange();
        weaponData.whatIsWall = whatIsWall;

    }
    public void EnemyShoot()
    {
        Transform target = enemyMov.Target;
        if (target == null)
            return;

        weaponData.startPosition = transform.position;
        weaponData.direction = target.transform.position - transform.position;
        weaponData.maxDamage = CalculateDamage();

        WeaponHitScan.FireWeapon(weaponData);
    }

    int CalculateDamage()
    {
        int maxDamage = enemyData.GetDamage();
        int minDamage = enemyData.GetDamage() - Random.Range(0, maxDamage / 2);
        return Random.Range(minDamage, maxDamage);
    }

    void ChangeRunCycle()
    {
        int maxHealth = enemyData.GetHealth();
        int currHealth = enemyMov.EnemyHealth;
        
        // Change run cycle, if enough damage is dealt
        if(currHealth < maxHealth / 2)
        {
            // TODO: Add sprite changing logic?
            Animator animator = GetComponent<Animator>();
            animator.speed = 1.25f;

            enemyMov.EnemySpeed *= moveSpeedMultiplier;
            enemyMov.OnEnemyDamage -= ChangeRunCycle;
        }
    }
}
