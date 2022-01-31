/*
 * An Object Pooler class for handling explosion and fire in the game
 * 
 */



using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionManager : MonoBehaviour
{
    public static ExplosionManager Instance = null;
    
    [SerializeField] private LayerMask affectLayer = 0; // These layers are affected by explosions
    [SerializeField] private LayerMask wallLayer = 0;   // These layers block explosion (walls, floor etc)

    public List<Explosion> explosionType = new List<Explosion>();
    private Dictionary<string, Queue<GameObject>> explosionPool = new Dictionary<string, Queue<GameObject>>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        else if (Instance != this)
            Destroy(this);
    }

    private void Start()
    {
        // Populate object pool on Game Start
        for(int i  = 0; i < explosionType.Count; i++)
        {
            for(int j = 0; j < explosionType[i].maxCount; j++)
            {
                GameObject pooledObj = Instantiate(explosionType[i].explosionFX, transform.position, transform.rotation);
                pooledObj.SetActive(false);
                if(explosionPool.ContainsKey(explosionType[i].id))
                {
                    explosionPool[explosionType[i].id].Enqueue(pooledObj);
                }

                else
                {
                    explosionPool[explosionType[i].id] = new Queue<GameObject>();
                    explosionPool[explosionType[i].id].Enqueue(pooledObj);
                }
            }
        }
    }

    /// <summary>
    /// Deques an explosion from pool with given values, Enques after duration
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="explosionId"></param>
    /// <param name="radius"></param>
    /// <param name="force"></param>
    /// <param name="minDamage"></param>
    /// <param name="maxDamage"></param>
    /// <param name="owner"></param>
    public void CreateExplosion(Vector3 origin, string explosionId, float radius, float force, int minDamage, int maxDamage, Transform owner = null)
    {
        GameObject FX = GetPooledObject(explosionId);
        if (FX == null)
            return;

        FX.gameObject.SetActive(true);
        FX.transform.position = origin;
        ParticleSystem ps = FX.GetComponent<ParticleSystem>();
        float duration = 3f;
        if (ps != null)
        {
            duration = ps.main.duration;
            ps.Play();
        }

        AudioSource aSource = FX.GetComponent<AudioSource>();
        if (aSource != null)
            aSource.Play();
            
        Collider[] colliders = Physics.OverlapSphere(origin, radius, affectLayer);

        // Expensive as hell method with distance checks for each collider
        foreach(Collider col in colliders)
        {
            float distance = Vector3.Distance(col.transform.position, origin);
            RaycastHit hit;
            if (Physics.Raycast(origin, (col.transform.position - origin).normalized, out hit, distance, wallLayer))
            {
                // Allow destructibles to be overlapped
                if (col.gameObject.layer != LayerMask.NameToLayer("Destructible") && col.gameObject.layer != LayerMask.NameToLayer("DestructibleTp"))
                {
                    continue;
                }
            }

            int damage = CalculateDamage(distance, radius, maxDamage, minDamage);
            Rigidbody rb = col.GetComponent<Rigidbody>();
            if (rb != null)
            {
                if (rb.gameObject.CompareTag("Player"))
                {
                    ExplosionDamagePlayer(rb, damage, force, radius);
                }

                rb.AddExplosionForce(force, origin, radius * 10f, 1f);
            }

            if(col.gameObject.layer == LayerMask.NameToLayer("Actor"))
            {
                EnemyMovement movement = col.gameObject.GetComponent<EnemyMovement>();
                if(movement != null)
                {
                    movement.TakeDamage(origin, col, damage, owner, true);
                    continue;
                }
            }

            if (col.TryGetComponent<Destructible>(out Destructible dest))
                dest.ObjectTakeDamage(owner, damage, EnumContainer.DAMAGETYPE.Explosion);

            if(col.CompareTag("Explosive"))
            {
                if (col.TryGetComponent<Grenade>(out Grenade grenade))
                    grenade.Explode();
            }
        }

        StartCoroutine(ReturnToPool(FX, duration, explosionId));
    }

    /// <summary>
    /// Create an explosion at the origin on given transform object
    /// </summary>
    /// <param name="positionObj"></param>
    public void CreateFXExplosionAtPosition(Transform positionObj)
    {
        // TODO: Allow more types of explosions
        CreateExplosion(positionObj.position, "explosion_medium", 6f, 500f, 1, 150);
    }

    /// <summary>
    /// Spawn pooled fire effects
    /// </summary>
    /// <param name="id">Effect id</param>
    /// <param name="position">Position to spawn</param>
    /// <param name="duration">Time to return object to the object pool</param>
    /// <returns></returns>
    public GameObject CreateFire(string id, Vector3 position, float duration)
    {
        GameObject fireObject = GetPooledObject(id);
        if (fireObject == null)
            return null;

        fireObject.transform.position = position;
        fireObject.gameObject.SetActive(true);

        StartCoroutine(ReturnToPool(fireObject, duration, id));

        return fireObject;
    }

    /// <summary>
    /// Calculate explosion damage depending on params
    /// </summary>
    /// <param name="distance"></param>
    /// <param name="radius"></param>
    /// <param name="maxDamage"></param>
    /// <param name="minDamage"></param>
    /// <returns></returns>
    int CalculateDamage(float distance, float radius, int maxDamage, int minDamage)
    {
        int impactDamage = 0;

        if (distance < 2)
            impactDamage = maxDamage;

        int damage = (int)(maxDamage * (-distance / radius + 1) + impactDamage);  //(int)(maxDamage * Mathf.Cos((Mathf.PI * distance) / (2 * radius)) + impactDamage);

        if (damage < minDamage)
            damage = minDamage;

        return damage;
    }

    public void ExplosionDamagePlayer(Rigidbody player, int damage, float force, float radius)
    {
        Camera mainCam = player.transform.parent.GetComponentInChildren<Camera>();
        if(mainCam != null)
        {
            CameraShake shake = mainCam.GetComponent<CameraShake>();
            float shakeTime = force * 0.001f;
            shake.ShakeCamera(force / radius, Random.Range(shakeTime, 2 * shakeTime));   //TODO: Change
        }
        
        PlayerContainer pC = player.GetComponentInParent<PlayerContainer>();
        if (pC != null)
            pC.TakeDamage(damage, null, false, EnumContainer.DAMAGETYPE.Explosion);
    }

    GameObject GetPooledObject(string explosionId)
    {
        if(!explosionPool.ContainsKey(explosionId))
        {
            Debug.LogWarning("No explosion found with ID:" + explosionId);
            return null;
        }

        if (explosionPool[explosionId].Count < 1)
            return null;

        GameObject pooledObject = explosionPool[explosionId].Dequeue();
        return pooledObject;
    }

    IEnumerator ReturnToPool(GameObject FX, float duration, string explosionId)
    {
        yield return new WaitForSeconds(duration + 1);
        explosionPool[explosionId].Enqueue(FX);
        FX.SetActive(false);
    }
}

[System.Serializable]
public class Explosion
{
    public GameObject explosionFX;
    public string id;
    public int maxCount;
}
