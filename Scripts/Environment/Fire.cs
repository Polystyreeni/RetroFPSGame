using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fire : MonoBehaviour
{
    [SerializeField] private int damagePerInterval = 1; // Interval is 0.1f seconds;
    [SerializeField] private float minDuration = 0f;
    [SerializeField] private float maxDuration = 1f;
    [SerializeField] private float dissolveDuration = 1f;
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private string prefabName = string.Empty;
    [SerializeField] private bool isSpawnable = false;
    [SerializeField] private bool isPooled = false;

    private List<GameObject> objectToDamage = new List<GameObject>();
    private bool canDamage = true;
    private Transform owner = null;

    private void Start()
    {
        if(isSpawnable)
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.OnGameSaved += SaveObject;
            } 
        }
    }

    private void OnEnable()
    {
        foreach (Transform t in transform.GetComponentsInChildren<Transform>())
        {
            t.localScale = Vector3.one;
        }

        canDamage = true;
        if(isSpawnable)
            Invoke(nameof(Dissolve), lifeTime);

        InvokeRepeating(nameof(DamageObjects), 0f, 0.1f);
    }

    void Dissolve()
    {
        canDamage = false;
        if (objectToDamage.Count > 0)
        {
            for(int i = 0; i < objectToDamage.Count; i++)
            {
                RemoveObjectFromFire(objectToDamage[i]);
            }
        }
           
        StartCoroutine(DissolveC());
    }

    IEnumerator DissolveC()
    {
        if (TryGetComponent<Rigidbody>(out Rigidbody rb))
            rb.isKinematic = true;

        float elapsedTime = 0;
        Transform[] children = transform.GetComponentsInChildren<Transform>();
        Vector3 defaultScale = Vector3.one;
        while (elapsedTime < dissolveDuration)
        {
            foreach (Transform t in children)
            {
                float scaleModifier = Mathf.Lerp(defaultScale.x, 0, elapsedTime / dissolveDuration);  //transform.localScale * Time.deltaTime;
                t.localScale = defaultScale * scaleModifier;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (isPooled)
            gameObject.SetActive(false);

        else
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (objectToDamage.Count > 0)
        {
            for (int i = 0; i < objectToDamage.Count; i++)
            {
                RemoveObjectFromFire(objectToDamage[i]);
            }
        }

        if (isSpawnable)
        {
            SaveManager.Instance.OnGameSaved -= SaveObject;
        }
    }

    private void OnDisable()
    {
        if (objectToDamage.Count > 0)
        {
            for (int i = 0; i < objectToDamage.Count; i++)
            {
                RemoveObjectFromFire(objectToDamage[i]);
            }
        }

        owner = null;

        CancelInvoke(nameof(Dissolve));
        CancelInvoke(nameof(DamageObjects));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!canDamage)
            return;

        GameObject go = other.gameObject;
        if (objectToDamage.Contains(go))
        {
            //Debug.Log("Object Already in fire: " + go.name);
            return;
        }        

        if (go.CompareTag("Player"))
        {
            //Debug.Log("Fire Damage Player");
            PlayerContainer pc = go.GetComponentInParent<PlayerContainer>();
            if (pc != null)
            {
                float randomDuration = Random.Range(minDuration, maxDuration);
                pc.SetPlayerBurning(true, damagePerInterval, randomDuration);
                objectToDamage.Add(go);
            }
        }

        else if (go.layer == LayerMask.NameToLayer("Actor"))
        {
            EnemyMovement mov = go.GetComponent<EnemyMovement>();
            if (mov != null)
            {
                float randomDuration = Random.Range(minDuration, maxDuration);
                mov.SetBurning(true, damagePerInterval, randomDuration, owner);
                objectToDamage.Add(go);
            }
        }

        else if(go.layer == LayerMask.NameToLayer("Destructible") || go.layer == LayerMask.NameToLayer("DestructibleTp"))
        {
            Destructible dest = go.GetComponent<Destructible>();
            if(dest != null)
            {
                dest.ObjectTakeDamage(transform, damagePerInterval, EnumContainer.DAMAGETYPE.Fire);
                objectToDamage.Add(go);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        RemoveObjectFromFire(other.gameObject);
    }

    void RemoveObjectFromFire(GameObject other)
    {
        if (other == null)
            return;

        if (!objectToDamage.Contains(other))
            return;

        if (other.CompareTag("Player"))
        {
            PlayerContainer pc = other.GetComponentInParent<PlayerContainer>();
            if (pc != null)
            {
                pc.SetPlayerBurning(false);
                objectToDamage.Remove(other);
            }
        }

        else if (other.layer == LayerMask.NameToLayer("Actor"))
        {
            EnemyMovement mov = other.GetComponent<EnemyMovement>();
            if (mov != null)
            {
                //mov.TakeDamage(transform.position, other, damagePerInterval, null, false, 1, EnumContainer.DAMAGETYPE.Fire);
                mov.SetBurning(false);
                objectToDamage.Remove(other);
            }
        }
    }

    void DamageObjects()
    {
        //Debug.Log("Damaging objects");
        if (!canDamage)
            return;

        foreach (GameObject go in objectToDamage)
        {
            if (go == null)
                continue;

            if (go.CompareTag("Player"))
            {
                //Debug.Log("Fire Damage Player");
                PlayerContainer pc = go.GetComponentInParent<PlayerContainer>();
                float randomDuration = Random.Range(minDuration, maxDuration);
                pc.SetPlayerBurning(true, damagePerInterval, randomDuration);
            }

            else if (go.layer == LayerMask.NameToLayer("Actor"))
            {
                EnemyMovement mov = go.GetComponent<EnemyMovement>();
                float randomDuration = Random.Range(minDuration, maxDuration);
                mov.SetBurning(true, damagePerInterval, randomDuration, owner);
            }

            else if(go.layer == LayerMask.NameToLayer("Destructible") || go.layer == LayerMask.NameToLayer("DestructibleTp"))
            {
                Destructible dest = go.GetComponent<Destructible>();
                dest.ObjectTakeDamage(transform, damagePerInterval, EnumContainer.DAMAGETYPE.Fire);
            }
        }
    }

    public void SetOwner(Transform t)
    {
        owner = t;
    }

    void SaveObject()
    {
        SaveManager.SaveData.ObjectData objectData = new SaveManager.SaveData.ObjectData();
        objectData.prefabName = prefabName;
        objectData.transformData.position = transform.position;
        objectData.transformData.rotation = transform.rotation.eulerAngles;
        objectData.transformData.scale = transform.localScale;
        objectData.velocity = Vector3.zero;

        SaveManager.Instance.gameState.objectList.Add(objectData);
    }
}
