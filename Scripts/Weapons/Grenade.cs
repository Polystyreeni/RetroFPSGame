using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grenade : MonoBehaviour
{
    [SerializeField] private bool bImpactExplosion = true;
    [SerializeField] private float range = 0f;
    [SerializeField] private int minDamage = 0;
    [SerializeField] private int maxDamage = 0;
    [SerializeField] private float force = 20f;
    [SerializeField] private float fuseTime = 2f;
    [SerializeField] private GameObject spawnableEffect = null;

    private float fuseTimer = 0f;
    public float FuseTime { get { return fuseTime; } }
    
    public float FuseTimer { get { return fuseTimer; } set { fuseTimer = value; } }

    private Transform owner = null;
    public Transform Owner { get { return owner; } set { owner = value; } }

    private AudioSource aSource = null;
    private bool bDetonated = false;

    Rigidbody rb;

    private void Start()
    {
        SaveManager.Instance.OnGameSaved += SaveObject;
        rb = GetComponent<Rigidbody>();
        aSource = GetComponent<AudioSource>();
    }

    private void OnDestroy()
    {
        SaveManager.Instance.OnGameSaved -= SaveObject;
    }

    private void Update()
    {
        FuseTimer += Time.deltaTime;
        if (fuseTimer >= fuseTime)
            Explode();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (bImpactExplosion)
        {
            if(!collision.collider.CompareTag("Player"))
                Explode();
        }

        if(aSource != null)
        {
            aSource.Play();
        }
    }

    public void Explode()
    {
        if (bDetonated)
            return;

        bDetonated = true;
        ExplosionManager.Instance.CreateExplosion(transform.position, "explosion_medium", range, force, minDamage, maxDamage, owner);
        if (spawnableEffect != null)
            Instantiate(spawnableEffect, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }

    void SaveObject()
    {
        SaveManager.SaveData.ObjectData objectData = new SaveManager.SaveData.ObjectData();
        objectData.transformData.position = transform.position;
        objectData.transformData.rotation = transform.rotation.eulerAngles;
        objectData.transformData.scale = transform.localScale;
        objectData.velocity = rb.velocity;

        if(bImpactExplosion)
            objectData.prefabName = "Weapon/Grenade";
        else
            objectData.prefabName = "Weapon/GrenadeNoImpact";

        SaveManager.Instance.gameState.objectList.Add(objectData);
    }
}
