using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
// CLASS DEPRECIATED, TO BE REMOVED !!!
// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

public class EnemyMovementRB : MonoBehaviour
{
    public Transform target;
    public float pathRecalcTime = 1f;
    public float speed = 10f;
    public float height = 2.56f;
    public float radius = 0.4464798f;
    public Transform slopeCheck = null;
    public Transform groundCheck = null;
    public LayerMask whatIsGround;

    private Rigidbody rb = null;
    private Vector3 offset = Vector3.zero;
    private bool onRamp = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        offset = Vector3.up * (height / 2f);    //new Vector3(0, height / 2f, 0);
        StartCoroutine(EnemyChase());
    }

    void FixedUpdate()
    {
        RaycastHit hit;
        Debug.DrawLine(groundCheck.position, groundCheck.position + transform.forward * (radius + 0.1f), Color.red, 1f);

        // We're in stairs / slope, check if enemy can climb or is it too high
        if (Physics.Raycast(groundCheck.position, transform.forward, out hit, radius + 0.1f, whatIsGround))
        {
            Debug.Log("Collision Check");
            RaycastHit target;
            if(Physics.Raycast(slopeCheck.position, transform.forward, out target, radius + 0.2f, whatIsGround))
            {
                if(onRamp)
                {
                    onRamp = false;
                    rb.isKinematic = false;
                }
            }

            else
            {
                rb.isKinematic = true;
                onRamp = true;
            }
        }
        
        else
        {
            onRamp = false;
            rb.isKinematic = false;
        }
    }

    private bool IsRamp(Vector3 v)
    {
        float angle = Vector3.Angle(Vector3.up, v);
        return angle > 10;  // TODO: Make this a variable
    }

    private bool IsFloor(Vector3 v)
    {
        float angle = Vector3.Angle(Vector3.up, v);
        return angle < 45;
    }

    void DrawPath(NavMeshPath path)
    {
        if (path.corners.Length < 2)
            return;

        for(int i = 1; i < path.corners.Length; i++)
        {
            Debug.DrawLine(path.corners[i], path.corners[i - 1], Color.red, 1f);
        }
    }

    IEnumerator EnemyChase()
    {
        float elapsedTime = 0;
        int pathIndex = 0;
        Vector3 targetPos = Vector3.zero;
        NavMeshPath path = new NavMeshPath();

        while (true)
        {
            if (target == null)
            {
                target = FindTarget();
                Debug.Log("Target new set");
                yield return null;
                continue;
            }

            // No path or recalculate path required
            if (path.corners.Length <= 0 || elapsedTime > pathRecalcTime)
            {
                path.ClearCorners();
                bool success = NavMesh.CalculatePath(rb.position, target.position, NavMesh.AllAreas, path);
                PrintStatus(path);
                if (!success)
                {
                    Vector3 closestPoint = target.position;
                    bool findClosest = NavMesh.SamplePosition(target.position, out NavMeshHit hit, 5f, NavMesh.AllAreas);
                    if (findClosest)
                    {
                        closestPoint = hit.position;
                    }

                    if (!NavMesh.CalculatePath(rb.position, closestPoint, NavMesh.AllAreas, path))
                    {
                        yield return null;
                        continue;
                    }

                }

                pathIndex = 0;
                elapsedTime = 0;
                DrawPath(path);
            }

            // Path completed
            if (path.corners.Length < pathIndex)
            {
                Debug.Log("RB: Path Index too high, Clearing path");
                path.ClearCorners();
                yield return null;
                continue;
            }

            // Don't update path if target withing attack range
            if ((target.position - rb.position).sqrMagnitude < 1f)
            {
                Debug.Log("RB: Target near player, not updating path");
                yield return null;
                continue;
            }

            // Set new path index, since we're at target position
            Vector2 targetPos2D = new Vector2(targetPos.x, targetPos.z);
            Vector2 currentPos2D = new Vector2(rb.position.x, rb.position.z);

            if ((targetPos2D - currentPos2D).sqrMagnitude < 0.05f)
            {
                Debug.Log("RB: Close to target position, set new index");
                Vector3 posToSet = path.corners[pathIndex];
                posToSet.y = rb.position.y;
                rb.MovePosition(posToSet);
                int newIndex = pathIndex + 1;
                if (newIndex >= path.corners.Length)
                {
                    Debug.Log("RB: PathIndex too high, setting new path");
                    path.ClearCorners();
                    yield return null;
                    continue;
                }

                else
                {
                    pathIndex = newIndex;
                }
            }

            // Offset required to make RB target proper height
            targetPos = path.corners[pathIndex] + offset;

            // Move Enemy towards target position
            Vector3 dir = Vector3.forward;

            // No downwards motion
            if (target.position.y <= rb.position.y)
            {
                Debug.Log("RB: Target move with no height difference");
                dir = new Vector3(targetPos.x - rb.position.x, 0, targetPos.z - rb.position.z).normalized;   //(targetPos - transform.position).normalized;
            }
                
            // Upwards motion for stairs
            else
            {
                Debug.Log("RB: Target move with height difference");
                dir = (targetPos - rb.position).normalized;   //(targetPos - transform.position).normalized;
            }
                
            // If we're on a ramp, move without physics to avoid getting stuck
            if(rb.isKinematic)
            {
                transform.position += dir * speed * Time.fixedDeltaTime;
            }

            else
            {
                rb.MovePosition(rb.position + dir * speed * Time.fixedDeltaTime);
            }

            Vector3 lookPos = new Vector3(targetPos.x, transform.position.y, targetPos.z);
            transform.LookAt(lookPos, Vector3.up);

            Debug.Log("Direction: " + dir);
            Debug.LogFormat("Current Pos: {0} - TargetPos: {1}", rb.position, targetPos);
            Debug.Log("PathIndex: " + pathIndex);

            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }

    Transform FindTarget()
    {
        Transform t = null;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            t = player.transform;

        return t;
    }

    void PrintStatus(NavMeshPath path)
    {
        string text = "NavPath Status: ";
        switch(path.status)
        {
            case NavMeshPathStatus.PathComplete:
                text += "Complete";
                break;

            case NavMeshPathStatus.PathPartial:
                text += "Partial";
                break;

            case NavMeshPathStatus.PathInvalid:
                text += "Invalid";
                break;
        }

        Debug.Log(text);
    }
}
