using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnableFire : MonoBehaviour
{
    [SerializeField] private float duration = 5f;
    [SerializeField] private float dissolveDuration = 1f;
    [SerializeField] private int damagePerInterval = 1; // Interval is 0.1f seconds;
    [SerializeField] private string prefabName = string.Empty;

    private GameObject objectToDamage = null;
    private bool canDamage = true;

    private void Start()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnGameSaved += SaveObject;
        }

        Invoke(nameof(Dissolve), duration);
        //Destroy(gameObject, duration);
    }

    void Dissolve()
    {
        canDamage = false;
        StopBurningTarget();
        StartCoroutine(DissolveC());
    }

    IEnumerator DissolveC()
    {
        float elapsedTime = 0;
        Transform[] children = transform.GetComponentsInChildren<Transform>();
        Vector3 defaultScale = Vector3.one;
        while(elapsedTime < dissolveDuration)
        {
            foreach(Transform t in children)
            {
                float scaleModifier = Mathf.Lerp(defaultScale.x, 0, elapsedTime / dissolveDuration);  //transform.localScale * Time.deltaTime;
                t.localScale = defaultScale * scaleModifier;
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    void StopBurningTarget()
    {
        // Cancels burning, when fire gets disabled
        if (objectToDamage != null)
        {
            PlayerContainer pc = objectToDamage.GetComponentInParent<PlayerContainer>();
            if (pc != null)
            {
                pc.SetPlayerBurning(false);
            }
        }
    }

    private void OnDestroy()
    {
        StopBurningTarget();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!canDamage)
            return;

        if(other.CompareTag("Player"))
        {
            Debug.Log("Fire Damage Player");
            PlayerContainer pc = other.GetComponentInParent<PlayerContainer>();
            if(pc != null)
            {
                objectToDamage = other.gameObject;
                pc.SetPlayerBurning(true, damagePerInterval);
            }
        }

        // TODO: Enemy implementation
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerContainer pc = other.GetComponentInParent<PlayerContainer>();
            if (pc != null)
            {
                pc.SetPlayerBurning(false);
                objectToDamage = null;
            }
        }
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
