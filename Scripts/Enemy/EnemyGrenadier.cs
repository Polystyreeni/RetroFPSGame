using UnityEngine;

public class EnemyGrenadier : MonoBehaviour
{
    [SerializeField] private GameObject spawnedProjectile = null;
    [SerializeField] private LayerMask whatIsWall;

    private EnemyMovement enemyMov = null;

    void Start()
    {
        enemyMov = GetComponentInParent<EnemyMovement>();
    }
    public void EnemyShoot()
    {
        Transform target = enemyMov.Target;
        if (target == null)
            return;

        Vector3 startPos = transform.position;
        Vector3 endPos = target.transform.position + new Vector3(0, 1, 0);
        Vector3 dir = (endPos - startPos).normalized;

        Vector3 launchVel = CalculateLaunchVelocity(target, dir);
        
        GameObject projectile = Instantiate(spawnedProjectile, (transform.position + dir * 2), transform.rotation);
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        rb.velocity = launchVel;

        Grenade g = projectile.GetComponent<Grenade>();
        if (g != null)
        {
            g.Owner = transform.parent;
        }
    }

    /// <summary>
    /// Calculates a parabel shaped flypath for projectile
    /// </summary>
    /// <param name="target"></param>
    /// <param name="dirToTarget"></param>
    /// <returns></returns>
    Vector3 CalculateLaunchVelocity(Transform target, Vector3 dirToTarget)
    {
        // Target is close, aim directly at it
        if((target.position - transform.position).sqrMagnitude < 4f)
        {
            return dirToTarget * (target.position - transform.position).sqrMagnitude;
        }

        float maxHeight = 3f;
        if (Physics.Raycast(transform.position + new Vector3(0, 1, 0), Vector3.up, out RaycastHit hit, maxHeight, whatIsWall))
        {
            maxHeight = hit.point.y - transform.position.y - 1;
        }

        float displacementY = target.position.y - transform.position.y;
        Vector3 displacementXZ = new Vector3(target.position.x - transform.position.x, 0, target.position.z - transform.position.z);

        float h = transform.position.y + maxHeight;
        float heightDiff = -Mathf.Abs(displacementY - h);

        Vector3 velY = Vector3.up * Mathf.Sqrt(-2 * Physics.gravity.y * maxHeight);
        Vector3 velXZ = displacementXZ / (Mathf.Sqrt(-2 * maxHeight / Physics.gravity.y) + Mathf.Sqrt(2 * heightDiff / Physics.gravity.y));
        Vector3 launchVel = velXZ + velY * -Mathf.Sign(Physics.gravity.y);
        return launchVel;
    }
}
