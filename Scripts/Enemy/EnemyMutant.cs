using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMutant : MonoBehaviour
{
    [SerializeField] private Enemy enemyData = null;
    [SerializeField] private AudioClip sndHit = null;
    [SerializeField] private LayerMask whatIsWall;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private string explosionFx = null;

    private EnemyMovement enemyMov = null;
    private AudioSource aSource = null;
    private Animator animator = null;

    void Start()
    {
        enemyMov = GetComponentInParent<EnemyMovement>();
        animator = GetComponentInParent<Animator>();
        enemyMov.OnEnemyFire += EnemyJump;
        if (enemyData != null)
        {
            whatIsWall = enemyMov.WhatIsWall;
        }

        aSource = GetComponentInParent<AudioSource>();
    }

    private void OnDestroy()
    {
        enemyMov.OnEnemyFire -= EnemyJump;
    }
    public void EnemyShoot()
    {
        Transform target = enemyMov.Target;
        if (target == null)
            return;

        GroundSlam(target);
    }

    void GroundSlam( Transform target )
    {
        aSource.PlayOneShot(sndHit);
        FxManager.Instance.PlayFX(explosionFx, transform.position, Quaternion.identity);
        float distanceSq = (target.position - transform.position).sqrMagnitude;
        if (distanceSq > Mathf.Pow(enemyData.GetShootRange(), 2))
            return;

        PlayerContainer pc = target.GetComponentInParent<PlayerContainer>();
        if (pc != null)
        {
            PlayerMovement movement = pc.PlayerMovement;
            if (!movement.grounded)
                return;

            Rigidbody rb = pc.gameObject.GetComponentInChildren<Rigidbody>();
            int damage = enemyData.GetDamage() - (int)(Mathf.Sqrt(distanceSq));
            ExplosionManager.Instance.ExplosionDamagePlayer(rb, damage, damage * damage, 10);
            rb.AddExplosionForce(damage * 10, rb.position, distanceSq, 1f);
        }  

        else
        {
            EnemyMovement mov = target.GetComponentInParent<EnemyMovement>();
            if (mov != null)
            {
                mov.TakeDamage(target.position, target.GetComponent<CapsuleCollider>(), enemyData.GetDamage(), transform.parent, true);
            }
        }
    }

    void EnemyJump()
    {
        CapsuleCollider[] cc = GetComponentsInParent<CapsuleCollider>();
        if(cc != null)
        {
            foreach(var col in cc)
            {
                if (col.transform.name == "Head")
                    col.enabled = false;

                else
                    col.height *= 0.2f;
            }
            
        }

        StartCoroutine(JumpMovement());
    }

    IEnumerator JumpMovement()
    {
        enemyMov.EnableStateChange = false;
        Vector3 jumpPos;
        Rigidbody rb = GetComponentInParent<Rigidbody>();
        rb.isKinematic = true;
        Vector3 startPos = rb.position;

        if (Physics.Raycast(transform.position, Vector3.up, out RaycastHit hit, jumpHeight, whatIsWall))
        {
            jumpPos = hit.point;
        }

        else
        {
            jumpPos = transform.position + new Vector3(0, jumpHeight, 0);
        }

        float jumpTime = 0.4f;
        float jumpForce = (jumpPos.y - rb.position.y) / jumpTime;
        float elapsedTime = 0f;

        while (elapsedTime < jumpTime)
        {
            //Debug.Log("Y pos: " + rb.position.y);
            rb.MovePosition(rb.position + Vector3.up * jumpForce * Time.deltaTime);
            elapsedTime += Time.deltaTime;
            yield return new WaitForFixedUpdate();
        }

        jumpTime /= 2;
        elapsedTime = 0;
        while (elapsedTime < jumpTime)
        {
            rb.MovePosition(rb.position - Vector3.up * Time.deltaTime * 2 * jumpForce);
            elapsedTime += Time.deltaTime;
            yield return new WaitForFixedUpdate();
        }

        rb.position = startPos - Vector3.up;

        while (AnimatorIsPlaying())
            yield return null;

        ResetCollider();
        animator.SetBool("enemyShoot", false);
        enemyMov.EnableStateChange = true;
        enemyMov.ChangeState(EnemyMovement.ENEMY_STATE.Chase);
    }

    void ResetCollider()
    {
        CapsuleCollider[] cc = GetComponentsInParent<CapsuleCollider>();
        if (cc != null)
        {
            foreach (var col in cc)
            {
                if (col.transform.name == "Head")
                    col.enabled = true;

                else
                    col.height *= 5f;
            }
        }

        // Re-enable agent also
        Rigidbody rb = GetComponentInParent<Rigidbody>();
        rb.position += Vector3.up;
        rb.isKinematic = false;
    }

    bool AnimatorIsPlaying()
    {
        return animator.GetCurrentAnimatorStateInfo(0).length >
               animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
    }
}
