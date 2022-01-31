using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponKnife : PlayerWeapon
{
    private Camera playerCamera = null;

    [SerializeField] private int secondaryDamage = 300;
    [SerializeField] private float secondaryTime = 2f;
    [SerializeField] private float secondaryRange = 6f;
    private float meleeDelay;

    private Rigidbody playerRb = null;
    private PlayerMovement playerMov = null;

    [SerializeField] private LayerMask whatIsWall;
    [SerializeField] private LayerMask primaryFireLayer;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private bool shakeCamera = false;
    [SerializeField] private AudioClip hitSound = null;

    private void Awake()
    {
        meleeDelay = weaponAnimator.GetAnimLenght(2) / 2;
    }

    public override void FireWeapon()
    {
        base.FireWeapon();

        if (playerCamera == null)
            playerCamera = Camera.main;

        DefaultMeleeAttack();
    }

    public override void FireSecondary()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerRb == null || playerMov == null || base.Player == null)
            SetPlayer();

        Transform enemy = GetEnemyToMelee();

        bCanShoot = false;
        CanSwitchWeapon = false;
        weaponAnimator.StartAnimation(2);

        if (enemy != null)
        {
            Vector3 targetPos = enemy.position + (playerRb.transform.position - enemy.position).normalized * 1.5f;
            playerRb.velocity = Vector3.zero;
            playerMov.AllowMovement = false;
            StartCoroutine(MoveToTarget(targetPos));

            EnemyMovement movement = enemy.gameObject.GetComponent<EnemyMovement>();
            if (movement != null)
            {
                if(movement.EnemyHealth < secondaryDamage)
                {
                    movement.FreezeEnemy();
                }

                StartCoroutine(DamageEnemy(movement, enemy));
            }      
        }

        else
        {
            DefaultMeleeAttack();
        }

        if (shakeCamera)
        {
            playerCamera.GetComponentInChildren<CameraShake>().ShakeAmountOfTimes(10, 6);
        }

        Invoke(nameof(base.EnableShooting), secondaryTime);
        Invoke(nameof(base.EnableWeaponSwitching), weaponAnimator.GetAnimLenght(2));
    }

    void DefaultMeleeAttack()
    {
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, range, primaryFireLayer))
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Actor"))
            {
                EnemyMovement movement;
                if (hit.collider.gameObject.name == "Head")
                    movement = hit.collider.gameObject.GetComponentInParent<EnemyMovement>();
                else
                    movement = hit.collider.gameObject.GetComponent<EnemyMovement>();

                if (movement != null)
                    movement.TakeDamage(hit.point, hit.collider, maxDamage, base.Player);
            }

            else if (hit.collider.gameObject.GetComponent<Destructible>() != null)
            {
                hit.collider.gameObject.GetComponent<Destructible>().ObjectTakeDamage(base.Player, maxDamage, EnumContainer.DAMAGETYPE.Melee);
                FxManager.Instance.PlayFX("fx_impact_bullet_sparks", hit.point, Quaternion.identity);
            }

            else
            {
                if (hit.collider != null)
                {
                    // TODO: Change to impact particle fx
                    Quaternion rotation = Quaternion.LookRotation(hit.normal, Vector3.up);
                    Vector3 pos = hit.point + hit.normal * 0.01f;
                    FxManager.Instance.PlayFX("fx_impact_bullet_sparks", pos, rotation);
                }
            }

            if (shakeCamera)
            {
                playerCamera.GetComponent<CameraShake>().ShakeCamera(2, .1f);
            }
        }   
    }

    /// <summary>
    /// Checks for valid enemies within melee range and returns one if found
    /// TODO: Check for "best" enemy instead of returning the first valid enemy
    /// </summary>
    /// <returns></returns>
    Transform GetEnemyToMelee()
    {
        float minDist = Mathf.Infinity;
        Transform target = null;
        Collider[] colliders = Physics.OverlapSphere(transform.position, secondaryRange, enemyLayer);
        foreach(Collider col in colliders)
        {
            if (col.gameObject.layer == LayerMask.NameToLayer("Actor") && col.gameObject.CompareTag("Enemy"))
            {
                // Are walls blocking the player from reaching?
                RaycastHit hit;
                Vector3 dir = (col.transform.position - base.Player.position).normalized;
                float dist = Vector3.Distance(col.transform.position, base.Player.position);
                if (Physics.Raycast(transform.position, dir, out hit, dist, whatIsWall))
                    continue;
                
                // Is the player facing the enemy
                float dotP = Vector3.Dot(dir, playerCamera.transform.forward);
                if(dotP >= 0.75)
                {
                    if(target == null || minDist > dist)
                    {
                        target = col.transform;
                        minDist = dist;
                    }
                }
            }
        }

        return target;
    } 
    void SetPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        playerRb = player.GetComponent<Rigidbody>();
        playerMov = player.GetComponent<PlayerMovement>();
        AssingPlayer();
    }

    IEnumerator MoveToTarget(Vector3 targetPos)
    {
        float sqrMag = (playerRb.transform.position - targetPos).sqrMagnitude;
        float moveSpeed = 8f;
        float timeToWait = Mathf.Sqrt(sqrMag) / moveSpeed;
        float timePassed = 0f;

        while (timePassed < timeToWait)
        {
            playerRb.MovePosition(Vector3.Lerp(playerRb.transform.position, targetPos, timePassed / timeToWait));
            timePassed += Time.deltaTime;
            yield return null;
        }

        playerRb.position = targetPos;
    }

    IEnumerator DamageEnemy(EnemyMovement movement, Transform enemy)
    {
        yield return new WaitForSeconds(meleeDelay);
        if(movement != null)
            movement.TakeDamage(enemy.position, enemy.GetComponent<CapsuleCollider>(), secondaryDamage, base.Player, false);

        playerMov.AllowMovement = true;
        aSource.PlayOneShot(hitSound);
    }
}
