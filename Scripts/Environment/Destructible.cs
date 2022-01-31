using UnityEngine;
using System;
using UnityEngine.ProBuilder;
using UnityEngine.AI;

public class Destructible : MonoBehaviour
{
    [SerializeField] private string destructFX;                     // FX to play when destroyed
    [SerializeField] private int health;                            // Amount of damage required to destroy
    [SerializeField] private bool explodeOnDestroy = false;         // Is this an explosive barrel etc
    [SerializeField] private int explosionDamage = 0;               // Explosion damage (if explode on destroy)
    [SerializeField] private GameObject[] destroyedObject = null;   // Destroyed model
    [SerializeField] private GameObject spawnObject = null;         // Allows destructible to have an object spawn when destroyed
    [SerializeField] private Vector3 spawnObjectOffset = Vector3.zero;
    [SerializeField] EnumContainer.DAMAGETYPE[] damageType;   // Has to be this kind of damage to destroy

    private string objectID;            // Used for saving / loading
    private bool bDestoryed = false;    // Used to check if object can be damaged

    private void Start()
    {
        objectID = gameObject.name + transform.position.ToString();
        SaveManager.Instance.OnGameSaved += SaveDestructible;
        SaveManager.Instance.OnGameLoaded += LoadDestructible;

        if(destroyedObject != null)
        {
            for (int i = 0; i < destroyedObject.Length; i++)
            {
                destroyedObject[i].SetActive(false);
            }
        }
    }

    private void OnDestroy()
    {
        SaveManager.Instance.OnGameSaved -= SaveDestructible;
        SaveManager.Instance.OnGameLoaded -= LoadDestructible;
    }

    public void ObjectTakeDamage(Transform attacker, int amount, EnumContainer.DAMAGETYPE dmgType)
    {
        if (bDestoryed)
            return;

        bool validDamage = false;
        for(int i = 0; i < damageType.Length; i++)
        {
            if (damageType[i] == dmgType)
            {
                validDamage = true;
                break;
            }
        }

        if (!validDamage)
            return;

        health -= amount;
        if(health <= 0)
        {
            DestructibleDestroy(attacker);
        }
    }

    void DestructibleDestroy(Transform attacker)
    {
        // Handling explosion first so that the effect is visible as soon as possible
        if (explodeOnDestroy)
        {
            bDestoryed = true;
            ExplosionManager.Instance.CreateExplosion(transform.position, "explosion_medium", 6, 500, 20, explosionDamage, null);
        }

        FxManager.Instance.PlayFX(destructFX, transform.position + spawnObjectOffset, transform.rotation);
        
        ShowChildren();
        HideMainObject();
        if (spawnObject != null)
            Instantiate(spawnObject, transform.position + spawnObjectOffset, transform.rotation);
    }

    void ShowChildren()
    {
        if (destroyedObject != null)
        {
            for (int i = 0; i < destroyedObject.Length; i++)
            {
                destroyedObject[i].SetActive(true);
            }
        }
    }

    void HideMainObject()
    {
        bDestoryed = true;
        if (gameObject.GetComponent<ProBuilderMesh>() != null)
            Destroy(gameObject.GetComponent<ProBuilderMesh>());

        SpriteRenderer sr = gameObject.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            Destroy(gameObject.GetComponentInChildren<EnvSprite>());
            Destroy(sr);
        }
            

        Destroy(gameObject.GetComponent<MeshRenderer>());
        Destroy(gameObject.GetComponent<BoxCollider>());    // TODO: Support other types of colliders
        Destroy(gameObject.GetComponent<MeshCollider>());
        Destroy(gameObject.GetComponent<NavMeshObstacle>());
    }

    public void ForceDestroy()
    {
        DestructibleDestroy(null);
    }

    void SaveDestructible()
    {
        var data = new SaveManager.SaveData.DoorData();
        data.objectID = objectID;
        data.doorOpen = bDestoryed;

        SaveManager.Instance.gameState.doorList.Add(data);
    }

    void LoadDestructible()
    {
        SaveManager.SaveData.DoorData destData = null;
        for (int i = 0; i < SaveManager.Instance.gameState.doorList.Count; i++)
        {
            if (SaveManager.Instance.gameState.doorList[i].objectID == objectID)
            {
                destData = SaveManager.Instance.gameState.doorList[i];
                break;
            }
        }

        if (destData == null)
            return;

        if(destData.doorOpen)
        {
            ShowChildren();
            HideMainObject();
        }
    }
}
