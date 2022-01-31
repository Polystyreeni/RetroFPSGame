/*
 * Object pooler for all In-game special effects
 * This way we don't have to create new objects constantly
 * but rather reuse already spawned object for better performance
 */


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FxManager : MonoBehaviour
{
    public static FxManager Instance = null;

    public List<FX> fxList = new List<FX>();
    private Dictionary<string, Queue<GameObject>> fxPool = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<string, Queue<GameObject>> activePool = new Dictionary<string, Queue<GameObject>>();
    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        else if (Instance != this)
            Destroy(this);
    }
    private void Start()
    {
        // Pool objects when map is loaded
        for (int i = 0; i < fxList.Count; i++)
        {
            for (int j = 0; j < fxList[i].maxCount; j++)
            {
                GameObject pooledObj = Instantiate(fxList[i].fxObject, transform.position, transform.rotation);
                pooledObj.SetActive(false);
                if (fxPool.ContainsKey(fxList[i].id))
                {
                    fxPool[fxList[i].id].Enqueue(pooledObj);
                }

                else
                {
                    fxPool[fxList[i].id] = new Queue<GameObject>();
                    fxPool[fxList[i].id].Enqueue(pooledObj);
                    activePool[fxList[i].id] = new Queue<GameObject>();
                }
            }
        }
    }

    /// <summary>
    /// Plays an effect with given id at given position
    /// </summary>
    /// <param name="fxId"></param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    public void PlayFX(string fxId, Vector3 position, Quaternion rotation)
    {
        bool bInfinite = false;
        for (int i = 0; i < fxList.Count; i++)
        {
            if (fxList[i].id == fxId)
            {
                if (fxList[i].bIsInfinite)
                    bInfinite = true;
            }
        }

        GameObject FXObject = GetPooledObject(fxId, bInfinite);
        if (FXObject == null)
            return;

        FXObject.gameObject.SetActive(true);
        FXObject.transform.position = position;
        FXObject.transform.rotation = rotation;
        ParticleSystem ps = FXObject.GetComponent<ParticleSystem>();
        if (ps != null)
            ps.Play();

        // Handle differently if fx is marked as infinite
        if (bInfinite)
        {
            FXObject.SetActive(false);
            FXObject.SetActive(true);
            activePool[fxId].Enqueue(FXObject);
        }
            
        else
        {
            float duration = ps.main.duration;
            StartCoroutine(ReturnToPool(FXObject, duration, fxId));
        }
    }

    public void PlayBloodFxAtPosition(Transform t)
    {
        PlayFX("fx_blood_gib", t.position, t.rotation);
    }

    GameObject GetPooledObject(string fxId, bool bInfinite = false)
    {
        if (!fxPool.ContainsKey(fxId))
        {
            Debug.LogWarning("No fx found with ID:" + fxId);
            return null;
        }

        if (fxPool[fxId].Count < 1)
        {
            if(bInfinite)
            {
                if (activePool[fxId].Count > 1)
                    return activePool[fxId].Dequeue();
            }

            else
                return null;
        }
       
        GameObject pooledObject = fxPool[fxId].Dequeue();
        return pooledObject;
    }

    IEnumerator ReturnToPool(GameObject FX, float duration, string explosionId)
    {
        yield return new WaitForSeconds(duration + 1);
        fxPool[explosionId].Enqueue(FX);
        FX.SetActive(false);
    }
}

[System.Serializable]
public class FX
{
    // Particle effect for this fx
    public GameObject fxObject;

    // Name of this fx
    public string id;

    // How many of these can be active at once
    public int maxCount;

    // Is this effect infinite (for example bulletholes)
    public bool bIsInfinite;
}