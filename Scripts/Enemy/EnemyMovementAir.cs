using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// !!!!!!!!!!!!!!!!!!!!
// CLASS DEPRECIATED, TO BE REMOVED !!!
// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

public class EnemyMovementAir : EnemyMovement
{
    //[Header("Flying Enemy variables")]
    //[SerializeField] private float enemyFlySpeed = 1f;
    //[SerializeField] private float flyUpdateRate = 1f;
    //[SerializeField] private float targetRadius = 1.5f;
    //[SerializeField] private Rigidbody rb = null;

    //private NavMeshPath navPath = null;
    //private int navPathIndex = 0;
    /*public override IEnumerator EnemyChase()
    {
        base.CurrentState = ENEMY_STATE.Chase;

        while (true)
        {
            if (base.Target == null || !TargetIsValid(base.Target))
            {
                base.Target = FindTarget();
                if (base.Target == null)
                {
                    ChangeState(ENEMY_STATE.Idle);
                    yield break;
                }
            }

            base.EAnimator.SetFloat("enemyMovement", 1);

            Invoke(nameof(base.EnableShooting), EnemyReactionTime);

            float initialWait = EnemyReactionTime;
            while (Target != null)
            {
                Vector3 startPos = transform.position;
                Vector3 goalPos = Target.transform.position;
                Vector3 direction = (goalPos - startPos).normalized;
                Vector3 desiredPos = GetNextMovePosition(startPos, goalPos, direction);

                Debug.DrawLine(startPos, goalPos);

                // Update sprite rotation to face target
                Vector3 LookAtDir = new Vector3(Target.position.x - transform.position.x, 0, Target.position.z - transform.position.z);
                transform.rotation = Quaternion.LookRotation(LookAtDir.normalized, Vector3.up);

                float elapsedTime = 0f;
                while(elapsedTime < flyUpdateRate)
                {
                    Vector3 dirToMove = (desiredPos - transform.position).normalized;
                    rb.MovePosition(transform.position + dirToMove * enemyFlySpeed * Time.fixedDeltaTime);

                    elapsedTime += Time.fixedDeltaTime;
                    initialWait -= Time.fixedDeltaTime;

                    // Cancel move, if close to an enemy
                    if (Target == null || (transform.position - Target.position).sqrMagnitude <= targetRadius * targetRadius)
                    {
                        Debug.Log("AIR: Canceling move");
                        elapsedTime = flyUpdateRate;
                    }

                    if (CanShoot)
                    {
                        if (IsValidShot())
                        {
                            ChangeState(ENEMY_STATE.Fire);
                        }
                    }

                   yield return null;
                }
            }
        }
    }

    Vector3 GetNextMovePosition(Vector3 startPos, Vector3 goalPos, Vector3 direction)
    {
        Vector3 target = Vector3.zero;
        float distanceToMove = enemyFlySpeed * flyUpdateRate;

        if ((startPos - goalPos).sqrMagnitude <= targetRadius * targetRadius)
        {
            target = startPos;
            return target;
        }
            
        if (navPath == null)
            navPath = new NavMeshPath();

        RaycastHit hit;
        if (Physics.Raycast(startPos, direction, out hit, distanceToMove, WhatIsWall))
        {
            if(NavMesh.CalculatePath(startPos, goalPos, NavMesh.AllAreas, navPath))
            {
                if (navPath.corners.Length > 1)
                {
                    target = navPath.corners[1] + new Vector3(0, 1, 0);
                    Debug.Log("AIR: Using navpath, position: " + target);
                }
            }

            else
            {
                target = hit.point + hit.normal;
            }  
        }

        else
        {
            //navPath.ClearCorners();
            target = startPos + direction * distanceToMove;
        }

        Debug.Log("AIR: Target position: " + target);
        return target;
    }

    bool TryMove(Vector3 startPos, Vector3 direction)
    {
        Debug.Log("Attempting to find place to move in: " + direction);
        float minWallDistance = 1;
        RaycastHit hit;

        if (Physics.Raycast(startPos, direction, out hit, minWallDistance, WhatIsWall))
        {
            return false;
        }

        else
        {
            return true;
        }
    }*/
}
