using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
// CLASS DEPRECIATED, TO BE REMOVED !!!
// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

public class EnemyCharacterController : MonoBehaviour
{
    [SerializeField] private Transform target = null;
    [SerializeField] private CharacterController cc = null;

    [SerializeField] private float pathRecalcTime = 1f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Vector3 ccVelocity = Vector3.zero;
    float elapsedTime = 0;
    private NavMeshPath path = null;
    private int pathIndex = 0;

    private bool flying = false;

    private void Start()
    {
        elapsedTime = pathRecalcTime;
        StartCoroutine(EnemyChase());
    }
    IEnumerator EnemyChase()
    {
        while(true)
        {
            elapsedTime += Time.deltaTime;

            if (target == null)
            {
                target = FindTarget();
                yield return null;
            }

            Vector3 targetPos = Vector3.zero;
            if(elapsedTime > pathRecalcTime)
            {
                elapsedTime = 0;
                pathIndex = 0;
                path = new NavMeshPath();
                bool pathFound = NavMesh.CalculatePath(transform.position, target.position, NavMesh.AllAreas, path);
                if (pathFound)
                    targetPos = path.corners[0];

                else
                {
                    targetPos = target.position;
                    Debug.Log("Path not found, new target: " + targetPos);
                }
                    
            }

            if (targetPos == Vector3.zero && path.corners.Length > pathIndex)
                targetPos = path.corners[pathIndex];

            else
                targetPos = target.position;

            // AI reached path corner node
            if ((targetPos - transform.position).sqrMagnitude < 1 && path != null)
            {
                pathIndex++;
                if (pathIndex >= path.corners.Length - 1 && path.corners.Length > 0)
                {
                    Debug.Log("Path index too high!");
                    elapsedTime = pathRecalcTime;
                    yield return null;
                }

                targetPos = path.corners[pathIndex];
            }

            ccVelocity = Vector3.Normalize(targetPos - transform.position) * moveSpeed;
            Debug.Log("Chase cc vel: " + ccVelocity);

            yield return null;
        }
    }

    IEnumerator EnemyFly(Vector3 launchV, float mag)
    {
        flying = true;
        while (!cc.isGrounded && mag > 0)
        {
            Debug.Log("CC flying");
            ccVelocity = launchV * mag;
            //cc.Move(launchV * mag * Time.deltaTime);
            mag -= Time.deltaTime * 10;
            yield return null;
        }

        flying = false;
        StartChase();
        //StartCoroutine(EnemyChase());

        yield return null;
    }

    void StartChase()
    {
        StopAllCoroutines();
        StartCoroutine(EnemyChase());
    }

    Transform FindTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player.transform;
        //if (player != null)
        //    target = player.transform;
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.F) && !flying)
        {
            StopAllCoroutines();
            StartCoroutine(EnemyFly(new Vector3(Random.Range(-10, 10), Random.Range(0, 20), Random.Range(-10, 10)).normalized, 20f));
        }

        Vector3 vel = ccVelocity;
        vel.y += -9.81f * Time.deltaTime;
        Debug.Log("ccVelotity: " + ccVelocity);
        cc.Move(vel * Time.deltaTime);
    }

    /*private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Debug.Log("Controller collider hit");
        if (flying)
            return;

        if(hit.collider.gameObject.CompareTag("Player"))
        {
            StopAllCoroutines();
            StartCoroutine(EnemyFly((hit.transform.position - transform.position).normalized, 20f));
        }
    }*/
}
